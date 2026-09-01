namespace LAB2D.Gameplay
{
    using System;
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay.GongFa;
    using GameCharacter = LAB2D.Character.Character;

    /// <summary>
    /// 武学功法管理器 — 功法学习/内功激活的入口与规则结算。
    /// 内功被动加成经 GrowthBonusService 收集走统一属性管线（Learn/Activate 后触发重算）；
    /// 内功回蓝由 Tick 按秒折算（激活即生效，与打坐回蓝独立叠加）；
    /// 外功招式注册进 SkillManager 扩展槽位（读档后按学习顺序懒重建，槽位稳定）。
    /// 单例，由 GlobalInit 注册并驱动（IInitializable + ITickable，排在 CultivationManager 之后）。
    /// </summary>
    public class GongFaManager : Singleton<GongFaManager>, IInitializable, ITickable
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

        /// <summary>本会话是否已按 GrowthData 重建过外功注册（玩家就绪后执行一次）。</summary>
        private bool skillRebuilt;

        /// <summary>回蓝零头累计器（int 型 Mp 按秒折算）。</summary>
        private float mpRegenCarry;

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
            AWorkerTask.LogProvider("[GongFaDiag] GongFaManager 初始化完成", LogManager.LogLevelEnum.Debug);
        }

        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            // 玩家就绪后按存档重建一次外功技能注册（SkillManager 技能不存档）
            if (!this.skillRebuilt)
            {
                GameCharacter.CharacterData data = PlayerDataProvider();
                if (data != null)
                {
                    this.RebuildLearnedSkills(data);
                    this.skillRebuilt = true;
                }
            }

            GameCharacter.CharacterData current = PlayerDataProvider();
            if (current != null)
            {
                this.TickMpRegen(current, deltaTime);
            }
        }

        /// <summary>
        /// 学习功法：内功写入已学列表并触发属性重算；外功额外注册主动技能。
        /// </summary>
        /// <returns>是否学习成功。</returns>
        public bool Learn(string gongFaId)
        {
            GameCharacter.CharacterData data = PlayerDataProvider();
            if (data == null)
            {
                return false;
            }

            GrowthData.Ensure(ref data.Growth);
            GongFaDef def = GongFaLibrary.Get(gongFaId);
            if (!GongFaRuleService.CanLearn(data.Growth, def))
            {
                if (def != null && data.Growth.LearnedGongFaIds.Contains(def.Id))
                {
                    TipProvider("已学过 " + def.Name);
                }
                else if (def != null)
                {
                    TipProvider($"{def.Name} 需 {RealmName(def.RequiredRealmIndex)} 境界");
                }

                return false;
            }

            // 外功先注册技能（槽位满/重复时整体失败，不产生脏数据）
            if (!def.IsNeiGong)
            {
                if (!SkillManager.Instance.RegisterGongFaSkill(def))
                {
                    TipProvider("技能栏已满，无法学习 " + def.Name);
                    return false;
                }
            }

            data.Growth.LearnedGongFaIds.Add(def.Id);
            data.Character?.RecomputeGrowthAttributes();
            TipProvider($"习得 {def.Name}！");
            AWorkerTask.LogProvider(
                $"[GongFaDiag] 学习功法 {def.Name}（{(def.IsNeiGong ? "内功" : "外功")}）",
                LogManager.LogLevelEnum.Debug);
            return true;
        }

        /// <summary>
        /// 激活已学的内功（同时仅一本；换功法直接切换）。
        /// </summary>
        /// <returns>是否激活成功。</returns>
        public bool ActivateNeiGong(string gongFaId)
        {
            GameCharacter.CharacterData data = PlayerDataProvider();
            if (data == null)
            {
                return false;
            }

            GrowthData.Ensure(ref data.Growth);
            GongFaDef def = GongFaLibrary.Get(gongFaId);
            if (!GongFaRuleService.CanActivate(data.Growth, def))
            {
                return false;
            }

            if (data.Growth.ActiveNeiGongId == def.Id)
            {
                TipProvider(def.Name + " 已在运转");
                return true;
            }

            data.Growth.ActiveNeiGongId = def.Id;
            data.Character?.RecomputeGrowthAttributes();
            TipProvider($"运转 {def.Name}");
            AWorkerTask.LogProvider(
                $"[GongFaDiag] 激活内功 {def.Name}（回蓝 {def.MpRegenPerSec:F0}/秒）",
                LogManager.LogLevelEnum.Debug);
            return true;
        }

        /// <summary>功法是否已学习（面板展示用）。</summary>
        public bool IsLearned(GameCharacter.CharacterData data, string gongFaId)
        {
            return data?.Growth?.LearnedGongFaIds != null && data.Growth.LearnedGongFaIds.Contains(gongFaId);
        }

        /// <summary>
        /// Worker 自动修习内功：境界达标的未学内功全部学会，并自动运转最新学的一本
        /// （Worker 无修仙 UI，全自动；被动加成经统一属性管线生效）。
        /// 绝不学习外功——外工会注册进全局 SkillManager 挤占玩家技能槽位。
        /// </summary>
        /// <param name="data">Worker 角色数据。</param>
        internal void AutoLearnNeiGongFor(GameCharacter.CharacterData data)
        {
            if (data == null)
            {
                return;
            }

            GrowthData.Ensure(ref data.Growth);
            foreach (GongFaDef def in GongFaLibrary.All)
            {
                if (!def.IsNeiGong
                    || data.Growth.LearnedGongFaIds.Contains(def.Id)
                    || !GongFaRuleService.CanLearn(data.Growth, def))
                {
                    continue;
                }

                data.Growth.LearnedGongFaIds.Add(def.Id);
                data.Growth.ActiveNeiGongId = def.Id;
                data.Character?.RecomputeGrowthAttributes();
                AWorkerTask.LogProvider(
                    $"[GongFaDiag] {data.Name} 自动修习内功 {def.Name} 并开始运转",
                    LogManager.LogLevelEnum.Debug);
            }
        }

        /// <summary>按存档重建外功技能注册（保持学习顺序，槽位与学习时一致）。</summary>
        private void RebuildLearnedSkills(GameCharacter.CharacterData data)
        {
            GrowthData.Ensure(ref data.Growth);
            foreach (GongFaDef def in GongFaLibrary.GetLearnedExternalSkills(data.Growth.LearnedGongFaIds))
            {
                SkillManager.Instance.RegisterGongFaSkill(def);
            }
        }

        /// <summary>内功回蓝：激活内功的 MpRegenPerSec 按秒折算（零头进累计器，与打坐回蓝独立）。</summary>
        private void TickMpRegen(GameCharacter.CharacterData data, float deltaTime)
        {
            GrowthData.Ensure(ref data.Growth);
            GongFaDef neiGong = GongFaLibrary.Get(data.Growth.ActiveNeiGongId);
            float regen = neiGong != null && neiGong.IsNeiGong ? neiGong.MpRegenPerSec : 0f;
            if (regen <= 0f)
            {
                this.mpRegenCarry = 0f;
                return;
            }

            if (data.Mp >= data.MaxMp)
            {
                this.mpRegenCarry = 0f;
                return;
            }

            this.mpRegenCarry += regen * deltaTime;
            if (this.mpRegenCarry >= 1f)
            {
                int add = (int)this.mpRegenCarry;
                this.mpRegenCarry -= add;
                int before = data.Mp;
                data.Mp = Math.Min(data.MaxMp, data.Mp + add);
                if (data.Mp != before)
                {
                    AWorkerTask.LogProvider(
                        $"[GongFaDiag] 内功回蓝 +{data.Mp - before}（{neiGong.Name}）",
                        LogManager.LogLevelEnum.Trace);
                }
            }
        }

        /// <summary>境界索引 → 中文名（提示文案用）。</summary>
        private static string RealmName(int realmIndex)
        {
            return LAB2D.Domain.Gameplay.Cultivation.RealmLibrary.Get(realmIndex).Name;
        }
    }
}
