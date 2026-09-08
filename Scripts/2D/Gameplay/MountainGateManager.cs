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
    /// 山门核心管理器 — 无阵营 Worker 与玩家的默认核心（M1.3，P5 胜负软化后去终局）。
    /// 地图就绪后在中心附近放置核心瓦片（不可通行）；妖兽子弹命中核心格转调 DamageCore。
    /// 被击破不再终局：惩罚（居民好感 -10 + 玩家钱包罚 10%）+ 按比例复活（山门永续）；
    /// 升到满级也不再胜利：改里程碑播报（ShowTip + 全员士气 +20）。
    /// 终局结算面板（SessionEndPanel）保留，触发线交由后续玩法接管。
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

        /// <summary>核心被击破时玩家钱包罚没比例（P5 胜负软化：惩罚替代终局）。</summary>
        private const float DownfallWalletPenaltyRatio = 0.1f;

        /// <summary>核心满级里程碑的全员士气增益（P5：替代原阶段胜利）。</summary>
        private const float MaxLevelMoraleBonus = 20f;

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
        /// 2026-09-08 用户决策"中心点不需要宗门核心的建造"：本函数不再自动放置/重建
        /// 山门核心（新档不放、读档不重放），IsCorePlaced 保持 false——HUD/胜负/伤害
        /// 各依赖方均有"无核心"分支自然跳过。MountainGateCore 类、SO 条目与
        /// PlaceBySystem 管线保留（Editor 菜单 MountainGateMenu 仍可手动放置）。
        /// </summary>
        /// <param name="mapCenter">地图中心格子坐标。</param>
        public void InitPosition(Vector3Int mapCenter)
        {
            // 山门核心自动放置已停用（见上注释）：新档不放、读档不重放
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
                this.OnCoreMaxLevelMilestone();
            }
            else
            {
                ShowTip($"山门核心升至 {this.CoreLevel} 级，小镇灵气渐盛");
            }

            return true;
        }

        /// <summary>
        /// 满级里程碑（P5 胜负软化）：不再触发阶段胜利终局，
        /// 改为播报 + 全员士气 +20（小镇纪元继续）。
        /// </summary>
        private void OnCoreMaxLevelMilestone()
        {
            AWorkerTask.LogProvider("[GateDiag] 山门核心升至满级（里程碑，胜负软化后不再终局）", LogManager.LogLevelEnum.Debug);
            try
            {
                int workerCount = Core.GameServices.WorkerCountProvider();
                for (int i = 0; i < workerCount; i++)
                {
                    if (Core.GameServices.WorkerGetProvider(i) is Character { IsWorkerCharacter: true } worker
                        && worker.CharacterDataLAB is LAB2D.Character.Worker.AWorker.WorkerData wd)
                    {
                        wd.CurMorale = Math.Min(100f, wd.CurMorale + MaxLevelMoraleBonus);
                    }
                }
            }
            catch (Exception)
            {
                // Provider 未就绪（测试环境）时静默降级
            }

            ShowTip("山门核心大功告成！小镇声威大振，居民士气高涨（里程碑达成，纪元继续）");
        }

        /// <summary>
        /// 核心被击破 — 胜负软化（P5）：不再终局，山门永续。
        /// 惩罚 = 居民好感 -10 + 玩家钱包罚 10%；核心按比例复活（ComputeCoreReviveHp）。
        /// </summary>
        private void OnCoreDestroyed()
        {
            this.DownfallCount++;
            this.CoreHp = this.ruleService.ComputeCoreReviveHp();
            AWorkerTask.LogProvider(
                $"[GateDiag] 山门核心被击破（第 {this.DownfallCount} 次，胜负软化后不再终局）→ 恢复 hp={this.CoreHp:F0}",
                LogManager.LogLevelEnum.Debug);

            int walletPenalty = 0;
            try
            {
                // 居民士气受挫：全体对玩家好感下降
                FavorabilityManager fm = Core.ServiceLocator.Get<FavorabilityManager>();
                int workerCount = Core.GameServices.WorkerCountProvider();
                for (int i = 0; i < workerCount; i++)
                {
                    if (Core.GameServices.WorkerGetProvider(i) is LAB2D.Character.Worker.AWorker worker)
                    {
                        fm?.ModifyWithPlayer(worker, DownfallFavorabilityPenalty, "山门核心被击破");
                    }
                }

                // 玩家惩罚：钱包罚 10%（与死亡惩罚管线同思路——惩罚而非终局）
                CurrencyManager currency = Core.ServiceLocator.Get<CurrencyManager>();
                if (currency != null)
                {
                    walletPenalty = (int)(currency.GetWalletBalance(0).Gold * DownfallWalletPenaltyRatio);
                    currency.TrySpendPlayerGold(walletPenalty);
                }
            }
            catch (Exception)
            {
                // Provider 未就绪（测试环境）时静默降级
            }

            ShowTip($"山门核心被击破！（第 {this.DownfallCount} 次）居民士气受挫、损失 {walletPenalty} 金——重整旗鼓，山门可再守！");
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
