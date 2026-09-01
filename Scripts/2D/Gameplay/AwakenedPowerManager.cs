namespace LAB2D.Gameplay
{
    using System;
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay.AwakenedPower;
    using GameCharacter = LAB2D.Character.Character;

    /// <summary>
    /// 异能觉醒管理器 — 玩家受击时按概率觉醒（血量越低概率越高，上限 1 个），
    /// 觉醒即写入 GrowthData.AwakenedPowerIds 并注册对应主动技能进扩展槽位；
    /// 读档后按存档懒重建技能注册（技能本身不存档）。
    /// 单例，由 GlobalInit 注册并驱动（IInitializable + ITickable，排在 GongFaManager 之后）。
    /// </summary>
    public class AwakenedPowerManager : Singleton<AwakenedPowerManager>, IInitializable, ITickable
    {
        internal static Func<GameCharacter.CharacterData> PlayerDataProvider { get; set; }
            = () => CultivationManager.GetPlayerData();

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

        /// <summary>本会话是否已按 GrowthData 重建过异能技能注册（玩家就绪后执行一次）。</summary>
        private bool skillRebuilt;

        private bool isInitialized;

        /// <inheritdoc/>
        public void Initialize()
        {
            if (this.isInitialized)
            {
                return;
            }

            this.isInitialized = true;
            this.skillRebuilt = false;
            EventBus.Instance.Subscribe<CharacterDamagedEvent>(this.OnCharacterDamaged);
            AWorkerTask.LogProvider("[PowerDiag] AwakenedPowerManager 初始化完成", LogManager.LogLevelEnum.Debug);
        }

        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            // 玩家就绪后按存档重建一次异能技能注册（SkillManager 技能不存档）
            if (this.skillRebuilt)
            {
                return;
            }

            GameCharacter.CharacterData data = PlayerDataProvider();
            if (data == null)
            {
                return;
            }

            GrowthData.Ensure(ref data.Growth);
            foreach (string powerId in data.Growth.AwakenedPowerIds)
            {
                this.RegisterPowerSkill(powerId);
            }

            this.skillRebuilt = true;
        }

        /// <summary>受击觉醒判定：玩家受击 roll 觉醒（注册主动技能）；Worker 受击同样可 roll（转被动加成）。</summary>
        private void OnCharacterDamaged(CharacterDamagedEvent e)
        {
            GameCharacter.CharacterData data = PlayerDataProvider();
            if (data != null && e.TargetId == data.Id)
            {
                this.RollAwaken(data, registerSkill: true);
                return;
            }

            // Worker 觉醒：无技能栏（SkillManager 槽位玩家全局共享），被动加成入账
            System.Collections.Generic.List<AWorker> workers = CultivationManager.WorkerCharactersProvider();
            if (workers == null)
            {
                return;
            }

            foreach (AWorker worker in workers)
            {
                if (worker == null || worker.CharacterDataLAB == null || worker.CharacterDataLAB.Id != e.TargetId)
                {
                    continue;
                }

                this.RollAwaken(worker.CharacterDataLAB, registerSkill: false);
                return;
            }
        }

        /// <summary>
        /// 觉醒 roll 与结算（玩家/Worker 通用）：血量越低概率越高，上限按各自 GrowthData 计。
        /// 玩家觉醒注册主动技能；Worker 觉醒把 <see cref="AwakenedPowerDef.WorkerPassiveBonus"/>
        /// 入账 PermanentRealmBonus（管线零改动，属性立即重算）。
        /// </summary>
        private void RollAwaken(GameCharacter.CharacterData data, bool registerSkill)
        {
            GrowthData.Ensure(ref data.Growth);
            GrowthData growth = data.Growth;
            if (!AwakenedPowerRuleService.CanAwaken(growth))
            {
                return;
            }

            float chance = AwakenedPowerRuleService.GetAwakenChance(data.Hp, data.MaxHp);
            float roll = AwakenedPowerRuleService.RandomFloatProvider?.Invoke(0f, 1f) ?? 1f;
            if (roll >= chance)
            {
                AWorkerTask.LogProvider(
                    $"[PowerDiag] {data.Name} 觉醒未触发（roll {roll:F2} >= 概率 {chance:P1}，Hp {data.Hp:F0}/{data.MaxHp:F0}）",
                    LogManager.LogLevelEnum.Trace);
                return;
            }

            string powerId = AwakenedPowerRuleService.RollPowerId(growth);
            AwakenedPowerDef def = AwakenedPowerLibrary.Get(powerId);
            if (def == null)
            {
                return;
            }

            growth.AwakenedPowerIds.Add(def.Id);
            if (registerSkill)
            {
                this.RegisterPowerSkill(def.Id);
            }
            else
            {
                // Worker：觉醒转被动加成入账（与境界加成同通道，重算立即生效）
                growth.PermanentRealmBonus += def.WorkerPassiveBonus;
                data.Character?.RecomputeGrowthAttributes();
                (data.Character as AWorker)?.ShowMindBubble($"体内涌起陌生的力量…觉醒了「{def.Name}」");
            }

            TipProvider($"{data.Name} 血脉觉醒！获得异能「{def.Name}」");
            AWorkerTask.LogProvider(
                $"[PowerDiag] {data.Name} 觉醒异能 {def.Name}（{(registerSkill ? "主动技" : "Worker被动")}，roll {roll:F2} < 概率 {chance:P1}，Hp {data.Hp:F0}/{data.MaxHp:F0}）",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>按异能 Id 注册对应主动技能（幂等）。</summary>
        private bool RegisterPowerSkill(string powerId)
        {
            AwakenedPowerDef def = AwakenedPowerLibrary.Get(powerId);
            if (def == null)
            {
                return false;
            }

            SkillData skill = SkillManager.CreateExtraSkill(def.SkillId);
            if (skill == null)
            {
                return false;
            }

            return SkillManager.Instance.RegisterExtraSkill(skill, def.Name);
        }
    }
}
