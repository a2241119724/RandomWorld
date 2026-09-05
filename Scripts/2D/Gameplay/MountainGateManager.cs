namespace LAB2D.Gameplay
{
    using LAB2D;
    using Character = LAB2D.Character.Character;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Gameplay;
    using LAB2D.Item.Build;
    using System;
    using UnityEngine;

    /// <summary>
    /// 山门核心管理器 — 小镇胜负锚点（M1.3）。
    /// 地图就绪后在中心附近放置核心瓦片（不可通行）；妖兽子弹命中核心格转调 DamageCore。
    /// 宽闸门失败曲线：前 CoreMaxDownfalls-1 次被击破只降级（恢复部分耐久 + 居民好感惩罚），
    /// 耗尽后终局失败（采集结算 + 冻结时间）；核心升到 CoreMaxLevel 即阶段胜利。
    /// 存档：核心位置/耐久/等级/被毁次数随 ASingletonSaveData 自动存取，读档后 InitPosition 重建视觉。
    /// </summary>
    public class MountainGateManager : ASingletonSaveData<MountainGateManager>
    {
        /// <summary>核心瓦片名（BuildMap PosMap 与 ResourceManager 资产名一致）。</summary>
        public const string CoreTileName = "MountainGateCore";

        /// <summary>核心占用边长（格）——3×3，主格为中心，副格由 RegisterCollisionTile 登记阻挡。</summary>
        public const int CoreSize = 3;

        /// <summary>寻找核心放置位的搜索半径（错开任务栏，取中心附近第一块 3×3 空地）。</summary>
        private const int PlaceSearchRadius = 25;

        /// <summary>核心被击破时全体居民对玩家的好感惩罚。</summary>
        private const float DownfallFavorabilityPenalty = -10f;

        /// <summary>核心所在地图格子坐标（default 表示尚未放置）。</summary>
        public Vector3Int CorePosition { get; private set; }

        /// <summary>核心是否已放置（含读档恢复）。</summary>
        public bool IsCorePlaced => this.CorePosition != default;

        /// <summary>核心当前耐久。</summary>
        public float CoreHp { get; private set; } = BuildingDamageRuleService.CoreMaxHp;

        /// <summary>核心等级（1 起，达到 CoreMaxLevel 即阶段胜利）。</summary>
        public int CoreLevel { get; private set; } = 1;

        /// <summary>核心已被击破次数。</summary>
        public int DownfallCount { get; private set; }

        /// <summary>终局失败已触发。</summary>
        public bool IsGameOver { get; private set; }

        /// <summary>阶段胜利已触发。</summary>
        public bool IsVictory { get; private set; }

        private readonly BuildingDamageRuleService ruleService = new();

        /// <summary>核心状态变化（位置/耐久/等级/被破次数/终局态任一变化）— 山门 HUD 订阅刷新。</summary>
        public event Action CoreChanged;

        /// <summary>触发核心状态变化通知（HUD 刷新）；只在事件点调用，不在逐帧路径。</summary>
        private void RaiseCoreChanged()
        {
            this.CoreChanged?.Invoke();
        }

        // ---- 初始化 ----

        /// <summary>
        /// 初始化核心位置（地图就绪后由 GlobalInit 调用）。
        /// 已有位置（读档恢复）时只重建视觉瓦片，不重新选址。
        /// </summary>
        /// <param name="mapCenter">地图中心格子坐标。</param>
        public void InitPosition(Vector3Int mapCenter)
        {
            if (this.IsCorePlaced)
            {
                this.PlaceCoreIcon(this.CorePosition);
                this.RaiseCoreChanged();
                return;
            }

            if (!this.TryFindCoreArea(mapCenter, PlaceSearchRadius, out Vector3Int found))
            {
                found = mapCenter; // 找不到整片空地时退回中心（极端地图）
                AWorkerTask.LogProvider("[GateDiag] 山门核心未找到 3×3 空地，退回地图中心", LogManager.LogLevelEnum.Warning);
            }

            this.CorePosition = found;
            this.PlaceCoreIcon(found);
            AWorkerTask.LogProvider(
                $"[GateDiag] 山门核心初始化 pos=({found.x},{found.y}) size={CoreSize}x{CoreSize} hp={this.CoreHp:F0} level={this.CoreLevel}",
                LogManager.LogLevelEnum.Debug);
            this.RaiseCoreChanged();
        }

        /// <summary>
        /// 判定某格是否属于山门核心占用范围（主格 + 副格，复用 ABuildItem.GetOccupiedPositions）。
        /// 供 BuildMap 核心伤害转调与拆除拦截使用。
        /// </summary>
        public bool IsCoreCell(Vector3Int pos)
        {
            if (!this.IsCorePlaced)
            {
                return false;
            }

            return ABuildItem.GetOccupiedPositions(this.CorePosition, CoreSize, CoreSize, AWorkerTask.RectType.BottomLeft).Contains(pos);
        }

        /// <summary>
        /// 从中心向外按 Chebyshev 环搜索第一块 CoreSize×CoreSize 全空的区域（照 IsAvailableMap.SpiralSearch 模式，
        /// 该方法只做单格判定，无多格版本）。found 为区域左下角（MountainGateCore 用 BottomLeft，参考点即主格）。
        /// </summary>
        private bool TryFindCoreArea(Vector3Int mapCenter, int radius, out Vector3Int found)
        {
            IsAvailableMap isAvailableMap = Core.ServiceLocator.Get<IsAvailableMap>();
            for (int layer = 0; layer <= radius; layer++)
            {
                for (int dx = -layer; dx <= layer; dx++)
                {
                    for (int dy = -layer; dy <= layer; dy++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != layer)
                        {
                            continue;
                        }

                        Vector3Int pos = new Vector3Int(mapCenter.x + dx, mapCenter.y + dy, 0);
                        if (this.IsAreaFree(isAvailableMap, pos, CoreSize))
                        {
                            found = pos;
                            return true;
                        }
                    }
                }
            }

            found = default;
            return false;
        }

        /// <summary>
        /// 检查以 pos 为左下角、边长 size 的方形区域是否全部为空地。
        /// </summary>
        private bool IsAreaFree(IsAvailableMap isAvailableMap, Vector3Int pos, int size)
        {
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (!isAvailableMap.IsTileFreeForDrop(new Vector3Int(pos.x + i, pos.y + j, 0)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 放置核心（3×3）：走 MountainGateCore 既有建造管线——
        /// SO 条目 IsNeedBuild=true（放置后创建建造任务，Worker 参与建核心），
        /// IsPass=false 主格物理阻挡可被打、副格 A* 阻挡。
        /// 读档重放幂等（AddBuild 覆盖已有 PosMap 条目，RegisterCollisionTile 有占用保护）。
        /// </summary>
        private void PlaceCoreIcon(Vector3Int pos)
        {
            new MountainGateCore().PlaceBySystem(pos);
        }

        // ---- 伤害与胜负 ----

        /// <summary>
        /// 核心受击（BuildMap.DamageBuilding 识别核心格后转调）。
        /// </summary>
        /// <param name="damage">本次伤害。</param>
        /// <param name="attacker">攻击者（可为 null）。</param>
        public void DamageCore(float damage, Character attacker)
        {
            if (!this.IsCorePlaced || this.IsGameOver || this.IsVictory || damage <= 0f)
            {
                return;
            }

            BuildingDamageRuleService.BuildingDamageResult result = this.ruleService.ApplyDamage(this.CoreHp, damage);
            this.CoreHp = result.RemainingHp;
            AWorkerTask.LogProvider(
                $"[GateDiag] 山门核心受击 damage={damage:F1} hp={this.CoreHp:F0}/{BuildingDamageRuleService.CoreMaxHp} attacker={attacker?.name ?? "null"}",
                LogManager.LogLevelEnum.Debug);

            if (result.IsDestroyed)
            {
                this.OnCoreDestroyed();
            }

            this.RaiseCoreChanged();
        }

        /// <summary>
        /// 尝试升级核心（阶段推进入口，山门 HUD 升级按钮/Editor 菜单调用）。
        /// 消耗玩家金币（1→2 扣 200、2→3 扣 500，规则见 BuildingDamageRuleService.GetCoreUpgradeCost，
        /// HUD 按钮同规则置灰）；升到 CoreMaxLevel 触发阶段胜利。
        /// </summary>
        /// <returns>是否升级成功。</returns>
        public bool TryUpgradeCore()
        {
            if (!this.IsCorePlaced || this.IsGameOver || this.IsVictory || this.CoreLevel >= BuildingDamageRuleService.CoreMaxLevel)
            {
                return false;
            }

            int cost = this.ruleService.GetCoreUpgradeCost(this.CoreLevel);
            if (cost > 0)
            {
                CurrencyManager currency = Core.ServiceLocator.Get<CurrencyManager>();
                if (currency == null || !currency.TrySpendPlayerGold(cost))
                {
                    ShowTip($"金币不足：升级核心需 {cost} 金币");
                    AWorkerTask.LogProvider(
                        $"[GateDiag] 升级失败（金币不足）level={this.CoreLevel} cost={cost}",
                        LogManager.LogLevelEnum.Warning);
                    return false;
                }
            }

            this.CoreLevel++;
            AWorkerTask.LogProvider($"[GateDiag] 山门核心升级 → level={this.CoreLevel} cost={cost}", LogManager.LogLevelEnum.Debug);
            this.RaiseCoreChanged();
            if (this.CoreLevel >= BuildingDamageRuleService.CoreMaxLevel)
            {
                this.TriggerVictory();
            }
            else
            {
                ShowTip($"山门核心升至 {this.CoreLevel} 级，小镇灵气渐盛");
            }

            return true;
        }

        /// <summary>
        /// 核心被击破 — 宽闸门：未达上限先降级（恢复部分耐久 + 居民好感惩罚），达上限终局失败。
        /// </summary>
        private void OnCoreDestroyed()
        {
            this.DownfallCount++;
            if (this.DownfallCount >= BuildingDamageRuleService.CoreMaxDownfalls)
            {
                this.TriggerGameOver();
                return;
            }

            this.CoreHp = this.ruleService.ComputeCoreReviveHp();
            AWorkerTask.LogProvider(
                $"[GateDiag] 山门核心被击破（第 {this.DownfallCount}/{BuildingDamageRuleService.CoreMaxDownfalls} 次）→ 降级恢复 hp={this.CoreHp:F0}",
                LogManager.LogLevelEnum.Debug);

            // 居民士气受挫：全体对玩家好感下降
            try
            {
                FavorabilityManager fm = Core.ServiceLocator.Get<FavorabilityManager>();
                int workerCount = Core.GameServices.WorkerCountProvider();
                for (int i = 0; i < workerCount; i++)
                {
                    if (Core.GameServices.WorkerGetProvider(i) is AWorker worker)
                    {
                        fm?.ModifyWithPlayer(worker, DownfallFavorabilityPenalty, "山门核心被击破");
                    }
                }
            }
            catch (Exception)
            {
                // Provider 未就绪（测试环境）时静默降级
            }

            ShowTip($"山门核心被击破！（第 {this.DownfallCount}/{BuildingDamageRuleService.CoreMaxDownfalls} 次）居民士气受挫，速速重建防线！");
        }

        /// <summary>
        /// 终局失败：采集会话结算（Defeat）并冻结时间（ITickable 与 WaveManager 协程一并停止），
        /// 弹出终局结算面板（面板按钮负责显式恢复 timeScale）。
        /// </summary>
        private void TriggerGameOver()
        {
            if (this.IsGameOver)
            {
                return;
            }

            this.IsGameOver = true;
            AWorkerTask.LogProvider("[GateDiag] 终局失败：山门核心连续被毁，小镇陷落", LogManager.LogLevelEnum.Debug);
            SessionResultData result = SessionResultManager.Instance.CaptureResult(SessionEndingType.Defeat);
            this.RaiseCoreChanged();
            Time.timeScale = 0f;
            ShowTip("山门陷落，小镇化为废墟……（终局失败，结算已采集）");
            this.OpenSessionEndPanel(result, SessionEndingType.Defeat);
        }

        /// <summary>
        /// 阶段胜利：核心满级，采集会话结算（Victory，不冻结时间），弹出终局结算面板。
        /// </summary>
        private void TriggerVictory()
        {
            if (this.IsVictory)
            {
                return;
            }

            this.IsVictory = true;
            AWorkerTask.LogProvider("[GateDiag] 阶段胜利：山门核心升至满级", LogManager.LogLevelEnum.Debug);
            SessionResultData result = SessionResultManager.Instance.CaptureResult(SessionEndingType.Victory);
            this.RaiseCoreChanged();
            ShowTip("山门核心大功告成！小镇在妖兽潮中屹立不倒（阶段胜利，结算已采集）");
            this.OpenSessionEndPanel(result, SessionEndingType.Victory);
        }

        /// <summary>
        /// 打开终局结算面板（try-catch：面板依赖场景 UI 根节点，Editor 测试环境可能缺失时静默降级）。
        /// </summary>
        private void OpenSessionEndPanel(SessionResultData result, SessionEndingType ending)
        {
            try
            {
                LAB2D.UI.Panel.SessionEndPanel.Instance.Open(result, ending);
            }
            catch (Exception)
            {
                // UI 根不存在（测试环境）时静默降级
            }
        }

        private static void ShowTip(string text)
        {
            try
            {
                Core.GameServices.ShowTipProvider(text);
            }
            catch (Exception)
            {
                // Tip 不可用时静默降级（测试环境）
            }
        }

        // ---- 存档 ----

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            MountainGateData data = new MountainGateData
            {
                CorePosX = this.CorePosition.x,
                CorePosY = this.CorePosition.y,
                CorePosZ = this.CorePosition.z,
                CoreHp = this.CoreHp,
                CoreLevel = this.CoreLevel,
                DownfallCount = this.DownfallCount,
                IsGameOver = this.IsGameOver,
                IsVictory = this.IsVictory,
            };
            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), data);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            MountainGateData data = DataTool.LoadDataByBinary<MountainGateData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            if (data == null)
            {
                return;
            }

            if (data.CorePosX != 0 || data.CorePosY != 0)
            {
                this.CorePosition = new Vector3Int(data.CorePosX, data.CorePosY, data.CorePosZ);
            }

            this.CoreHp = data.CoreHp;
            this.CoreLevel = data.CoreLevel;
            this.DownfallCount = data.DownfallCount;
            this.IsGameOver = data.IsGameOver;
            this.IsVictory = data.IsVictory;
            this.RaiseCoreChanged();
        }

        /// <summary>
        /// 山门核心存档数据。
        /// </summary>
        [Serializable]
        public class MountainGateData
        {
            public int CorePosX;
            public int CorePosY;
            public int CorePosZ;
            public float CoreHp;
            public int CoreLevel;
            public int DownfallCount;
            public bool IsGameOver;
            public bool IsVictory;
        }
    }
}
