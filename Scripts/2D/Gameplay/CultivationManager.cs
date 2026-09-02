namespace LAB2D.Gameplay
{
    using System;
    using LAB2D.Constant;
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay.Cultivation;
    using LAB2D.Domain.Worker;
    using UnityEngine;
    using GameCharacter = LAB2D.Character.Character;

    /// <summary>
    /// 修仙管理器 — 打坐灵气积累（×修炼速度加成）、打坐回蓝、突破结算、
    /// 受击/移动打断打坐。境界永久加成经 GrowthData.PermanentRealmBonus 走统一属性管线。
    /// 单例，由 GlobalInit 注册并驱动（IInitializable + ITickable）。
    /// </summary>
    public class CultivationManager : Singleton<CultivationManager>, IInitializable, ITickable
    {
        /// <summary>打坐打断的累计移动距离阈值（世界单位）。</summary>
        internal const float MeditateBreakMoveDistance = 1.0f;

        /// <summary>Worker 地面睡眠（无床）的吐纳效率系数（床睡为 1.0，鼓励建床）。</summary>
        internal const float GroundSleepQiScale = 0.5f;

        /// <summary>Worker 修仙扫描间隔（秒）：自动突破 + 自动修习内功。</summary>
        private const float WorkerScanInterval = 2f;

        internal static Func<Player> PlayerMineProvider { get; set; }
            = () => ServiceLocator.TryGet(out PlayerManager pm) ? pm.Mine : null;

        internal static Func<System.Collections.Generic.List<AWorker>> WorkerCharactersProvider { get; set; }
            = () => ServiceLocator.TryGet(out WorkerManager wm) ? wm.Characters : null;

        internal static Action<string> TipProvider { get; set; }
            = (msg) =>
            {
                try
                {
                    Core.GameServices.ShowTipProvider(msg);
                }
                catch (Exception)
                {
                    // Tip 不可用时静默降级（初始化早期/测试环境）
                }
            };

        /// <summary>玩家当前是否打坐中。</summary>
        public bool IsMeditating { get; private set; }

        private Vector3 meditateStartPos;
        private float mpRegenCarry;
        private float workerScanTimer;
        private bool isInitialized;

        /// <inheritdoc/>
        public void Initialize()
        {
            if (this.isInitialized)
            {
                return;
            }

            this.isInitialized = true;
            EventBus.Instance.Subscribe<CharacterDamagedEvent>(this.OnCharacterDamaged);
            AWorkerTask.LogProvider("[CultivationDiag] CultivationManager 初始化完成", LogManager.LogLevelEnum.Debug);
        }

        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            this.TickWorkers(deltaTime);

            if (!this.IsMeditating)
            {
                return;
            }

            Player player = PlayerMineProvider();
            GameCharacter.CharacterData data = GetPlayerData();
            if (player == null || data == null)
            {
                this.StopMeditate("玩家不存在");
                return;
            }

            // 移动打断：打坐期间累计位移超阈值视为主动移动
            if ((player.transform.position - this.meditateStartPos).sqrMagnitude
                > MeditateBreakMoveDistance * MeditateBreakMoveDistance)
            {
                this.StopMeditate("移动打断");
                return;
            }

            GrowthData.Ensure(ref data.Growth);

            // 聚灵阵科技加成：任一已建成聚灵阵 → 打坐灵气积累 +50%（加数不叠乘，见 TechManager）
            float spiritArrayBonus = TechManager.Instance.GetMeditateSpeedBonus();
            data.Growth.Qi += RealmRuleService.ComputeQiGain(data.Growth, deltaTime, spiritArrayBonus);

            // 回蓝：int 型 Mp 按秒折算，零头进累计器
            if (data.Mp < data.MaxMp)
            {
                this.mpRegenCarry += RealmRuleService.MeditateMpPerSec * deltaTime;
                if (this.mpRegenCarry >= 1f)
                {
                    int add = (int)this.mpRegenCarry;
                    this.mpRegenCarry -= add;
                    data.Mp = Math.Min(data.MaxMp, data.Mp + add);
                }
            }
        }

        /// <summary>切换打坐状态（面板按钮入口）。</summary>
        public void ToggleMeditate()
        {
            if (this.IsMeditating)
            {
                this.StopMeditate("手动停止");
            }
            else
            {
                this.StartMeditate();
            }
        }

        /// <summary>开始打坐。打坐中无法移动，受击会打断。</summary>
        public void StartMeditate()
        {
            if (this.IsMeditating)
            {
                return;
            }

            Player player = PlayerMineProvider();
            if (player == null)
            {
                return;
            }

            this.IsMeditating = true;
            this.meditateStartPos = player.transform.position;
            this.mpRegenCarry = 0f;
            TipProvider("开始打坐修炼");
            AWorkerTask.LogProvider("[CultivationDiag] 玩家开始打坐", LogManager.LogLevelEnum.Debug);
        }

        /// <summary>停止打坐。</summary>
        /// <param name="reason">停止原因（用于提示与日志）。</param>
        public void StopMeditate(string reason = "手动停止")
        {
            if (!this.IsMeditating)
            {
                return;
            }

            this.IsMeditating = false;
            TipProvider("打坐结束：" + reason);
            AWorkerTask.LogProvider($"[CultivationDiag] 打坐结束：{reason}", LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 尝试突破境界（玩家入口）：条件满足时扣除灵气、境界 +1、永久加成生效并触发属性重算。
        /// </summary>
        /// <returns>是否突破成功。</returns>
        public bool TryBreakthrough()
        {
            GameCharacter.CharacterData data = GetPlayerData();
            if (data == null)
            {
                return false;
            }

            GrowthData.Ensure(ref data.Growth);

            if (RealmLibrary.IsMax(data.Growth.RealmIndex))
            {
                TipProvider("已至金丹巅峰，暂无更高境界");
                return false;
            }

            if (!RealmRuleService.CanBreakthrough(data.Growth))
            {
                RealmDef current = RealmRuleService.GetRealm(data.Growth);
                RealmDef next = RealmLibrary.Get(current.Index + 1);
                TipProvider($"灵气不足：突破{next.Name}需 {current.QiToNext:F0} 灵气");
                return false;
            }

            int before = data.Growth.RealmIndex;
            if (!this.BreakthroughData(data))
            {
                return false;
            }

            RealmDef newRealm = RealmLibrary.Get(data.Growth.RealmIndex);
            TipProvider($"突破成功！进入 {newRealm.Name} 境界");
            AWorkerTask.LogProvider(
                $"[CultivationDiag] 玩家突破 {RealmLibrary.Get(before).Name} -> {newRealm.Name}，累计永久加成已生效",
                LogManager.LogLevelEnum.Debug);
            return true;
        }

        /// <summary>
        /// 突破结算（通用，玩家与 Worker 共用）：扣除灵气、境界 +1、
        /// 永久加成累进并触发属性重算。前提检查由调用方负责（或失败返回 false）。
        /// </summary>
        /// <param name="data">修炼者角色数据。</param>
        /// <returns>是否突破成功。</returns>
        internal bool BreakthroughData(GameCharacter.CharacterData data)
        {
            if (data == null)
            {
                return false;
            }

            GrowthData.Ensure(ref data.Growth);
            int before = data.Growth.RealmIndex;
            if (!RealmRuleService.Breakthrough(data.Growth))
            {
                return false;
            }

            RealmDef newRealm = RealmLibrary.Get(data.Growth.RealmIndex);

            // 永久加成进属性管线：立即重算
            data.Character?.RecomputeGrowthAttributes();
            AWorkerTask.LogProvider(
                $"[CultivationDiag] {data.Name} 突破 {RealmLibrary.Get(before).Name} -> {newRealm.Name}",
                LogManager.LogLevelEnum.Debug);
            return true;
        }

        /// <summary>
        /// Worker 睡眠吐纳：按睡眠时长结算灵气（睡觉即打坐，床睡全额/地面睡半额）。
        /// 由 WorkerSleepTask.Finish 调用；灵气只积累不突破，突破由 TickWorkers 扫描结算。
        /// </summary>
        /// <param name="data">Worker 角色数据。</param>
        /// <param name="seconds">睡眠时长（秒）。</param>
        /// <param name="scale">场景系数（床睡 1.0 / 地面睡 <see cref="GroundSleepQiScale"/>）。</param>
        internal void MeditateFor(GameCharacter.CharacterData data, float seconds, float scale = 1f)
        {
            if (data == null || seconds <= 0f)
            {
                return;
            }

            GrowthData.Ensure(ref data.Growth);
            float spiritArrayBonus = TechManager.Instance.GetMeditateSpeedBonus();
            float gain = RealmRuleService.ComputeQiGain(data.Growth, seconds, spiritArrayBonus, scale);
            if (gain <= 0f)
            {
                return;
            }

            data.Growth.Qi += gain;
            AWorkerTask.LogProvider(
                $"[CultivationDiag] {data.Name} 睡眠吐纳 +{gain:F0} 灵气（累计 {data.Growth.Qi:F0}/{RealmRuleService.QiToNext(data.Growth):F0}）",
                LogManager.LogLevelEnum.Trace);
        }

        /// <summary>
        /// Worker 修仙扫描（节流）：自动突破 + 自动修习内功。
        /// Worker 无修仙 UI，境界成长全自动；气泡与日志是唯一反馈面。
        /// </summary>
        private void TickWorkers(float deltaTime)
        {
            this.workerScanTimer += deltaTime;
            if (this.workerScanTimer < WorkerScanInterval)
            {
                return;
            }

            this.workerScanTimer = 0f;
            System.Collections.Generic.List<AWorker> workers = WorkerCharactersProvider();
            if (workers == null)
            {
                return;
            }

            foreach (AWorker worker in workers)
            {
                if (worker == null || worker.CharacterDataLAB == null)
                {
                    continue;
                }

                GameCharacter.CharacterData data = worker.CharacterDataLAB;
                GrowthData.Ensure(ref data.Growth);

                if (RealmRuleService.CanBreakthrough(data.Growth) && this.BreakthroughData(data))
                {
                    RealmDef newRealm = RealmLibrary.Get(data.Growth.RealmIndex);
                    worker.ShowMindBubble($"突破！进入{newRealm.Name}期…");
                    this.RecordBreakthroughMind(worker, data, newRealm);
                    AWorkerTask.LogProvider(
                        $"[CultivationDiag] {data.Name} 自动突破 -> {newRealm.Name}",
                        LogManager.LogLevelEnum.Debug);
                }

                GongFaManager.Instance.AutoLearnNeiGongFor(data);
            }
        }

        /// <summary>
        /// 突破接心智层（M2A 包 2.2）：突破者自身成就记忆 + 工友旁观反应 —
        /// 贪婪高者嫉妒（记仇向记忆，Greed 越强越酸），境界低于突破者者心生敬仰
        /// （爱慕向记忆，可能升 Admiration 关系）。两者均走 RecordEvent 既有管线
        /// （记忆/信念/人格漂移/关系喂食/最近想法），关系等级变化自动弹关系气泡。
        /// </summary>
        private void RecordBreakthroughMind(AWorker breaker, GameCharacter.CharacterData data, RealmDef newRealm)
        {
            if (!Core.ServiceLocator.TryGet<WorkerMindService>(out WorkerMindService mindService))
            {
                return;
            }

            mindService.RecordEvent(breaker, WorkerMindConstant.EVT_CULTIVATION_BREAKTHROUGH,
                MemoryValence.Positive, null, 70f, $"突破至{newRealm.Name}期");

            System.Collections.Generic.List<AWorker> workers = WorkerCharactersProvider();
            if (workers == null)
            {
                return;
            }

            string breakerName = breaker.name;
            int newRealmIndex = data.Growth.RealmIndex;
            foreach (AWorker other in workers)
            {
                if (other == null || other == breaker || other.CharacterDataLAB == null)
                {
                    continue;
                }

                GameCharacter.CharacterData otherData = other.CharacterDataLAB;
                GrowthData.Ensure(ref otherData.Growth);
                AWorker.WorkerData wd = otherData as AWorker.WorkerData;
                if (wd == null)
                {
                    continue;
                }

                WorkerMindData.Ensure(wd);
                if (wd.Greed >= WorkerMindConstant.RelationJealousyGreedThreshold)
                {
                    mindService.RecordEvent(other, WorkerMindConstant.EVT_FELLOW_BREAKTHROUGH_ENVY,
                        MemoryValence.Negative, breakerName,
                        Mathf.Clamp(wd.Greed, 20f, 100f), "工友突破了，心里不是滋味");
                }
                else if (newRealmIndex > otherData.Growth.RealmIndex)
                {
                    mindService.RecordEvent(other, WorkerMindConstant.EVT_FELLOW_BREAKTHROUGH,
                        MemoryValence.Positive, breakerName, 40f, "工友突破了，心生敬仰");
                }
            }
        }

        /// <summary>获取玩家角色数据（无玩家时为 null）。</summary>
        internal static GameCharacter.CharacterData GetPlayerData()
        {
            Player player = PlayerMineProvider();
            return player != null ? player.CharacterDataLAB : null;
        }

        /// <summary>受击打断：仅玩家自身受击（含反伤）时打断打坐。</summary>
        private void OnCharacterDamaged(CharacterDamagedEvent e)
        {
            if (!this.IsMeditating)
            {
                return;
            }

            GameCharacter.CharacterData data = GetPlayerData();
            if (data != null && e.TargetId == data.Id)
            {
                this.StopMeditate("受击打断");
            }
        }
    }
}
