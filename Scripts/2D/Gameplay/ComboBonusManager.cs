namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay;
    using System;

    /// <summary>
    /// 连击增益奖励管理器。
    /// 基于 GameplaySessionStats 的实时连击计数，为玩家提供伤害加成和经验值加成。
    /// 连击越高，增益倍率越大。连击中断时给予即时反馈。
    /// 运行时非 MonoBehaviour 单例，不保存数据，不涉及网络同步。
    ///
    /// 接入方式：
    /// - Character.ReduceHp 中自动对 Player 攻击者应用 DamageMultiplier
    /// - Player.AddExperienceValue 中自动应用 ExperienceMultiplier
    /// - 外部系统可订阅 OnComboChanged / OnComboMilestoneReached / OnComboBroken 事件
    /// </summary>
    public class ComboBonusManager : Singleton<ComboBonusManager>, IInitializable
    {
        /// <summary>
        /// 连击等级配置表：按连击数阈值定义伤害倍率和经验倍率。
        /// 从高到低遍历，取第一个匹配的等级。
        /// </summary>
        private static readonly ComboTier[] Tiers = new ComboTier[]
        {
            new ComboTier { MinCombo = 1,   DamageMultiplier = 1.00f, ExperienceMultiplier = 1.0f, Label = "" },
            new ComboTier { MinCombo = 3,   DamageMultiplier = 1.15f, ExperienceMultiplier = 1.3f, Label = "连击 x3!" },
            new ComboTier { MinCombo = 5,   DamageMultiplier = 1.30f, ExperienceMultiplier = 1.6f, Label = "连击 x5! 伤害提升!" },
            new ComboTier { MinCombo = 10,  DamageMultiplier = 1.50f, ExperienceMultiplier = 2.0f, Label = "连击 x10! 激增!" },
            new ComboTier { MinCombo = 20,  DamageMultiplier = 1.80f, ExperienceMultiplier = 3.0f, Label = "连击 x20! 无双!" },
            new ComboTier { MinCombo = 50,  DamageMultiplier = 2.50f, ExperienceMultiplier = 5.0f, Label = "连击 x50! 传说!" },
        };

        private static readonly int[] tierThresholds;
        private static readonly float[] tierDamageMultipliers;
        private static readonly float[] tierExpMultipliers;
        private static readonly string[] tierLabels;
        private static readonly ComboBonusRuleService ruleService = new ComboBonusRuleService();

        static ComboBonusManager()
        {
            tierThresholds = new int[Tiers.Length];
            tierDamageMultipliers = new float[Tiers.Length];
            tierExpMultipliers = new float[Tiers.Length];
            tierLabels = new string[Tiers.Length];
            for (int i = 0; i < Tiers.Length; i++)
            {
                tierThresholds[i] = Tiers[i].MinCombo;
                tierDamageMultipliers[i] = Tiers[i].DamageMultiplier;
                tierExpMultipliers[i] = Tiers[i].ExperienceMultiplier;
                tierLabels[i] = Tiers[i].Label;
            }
        }

        private int currentCombo;
        private int currentTierIndex;
        private float damageMultiplier = 1.0f;
        private float experienceMultiplier = 1.0f;
        private bool initialized;

        /// <summary>
        /// 是否已完成初始化。由 GlobalInit.Start() 通过 IInitializable 接口统一调用 Initialize() 后置为 true。
        /// </summary>
        public bool IsInitialized => this.initialized;

        /// <summary>
        /// 初始化连击系统：订阅 GameplaySessionStats 的 StatsChanged 事件，
        /// 并立即同步当前连击状态，避免错过已累积的连击数据。
        /// 由 GlobalInit.Start() 通过 IInitializable 接口统一调用，确保在 GameplaySessionStats 之后初始化。
        /// 重复调用安全（幂等）。
        /// </summary>
        public void Initialize()
        {
            if (this.initialized)
            {
                return;
            }

            try
            {
                GameplaySessionStats stats = Core.ServiceLocator.Get<GameplaySessionStats>();

                // 订阅后续连击变更事件
                stats.StatsChanged += this.OnStatsChanged;

                // 立即同步当前连击状态，防止因延迟初始化错过已累积的连击数据
                // 例如：玩家已击杀3只敌人（combo=3）后 ComboBonusManager 才首次被访问，
                // 此时必须主动拉取当前 combo，否则内部 currentCombo 保持 0，倍率恒为 1.0
                GameplaySessionStatsSnapshot snapshot = stats.CreateSnapshot();
                this.currentCombo = snapshot.CurrentCombo;
                this.RecalculateMultipliers();
                this.initialized = true;
            }
            catch (Exception e)
            {
                AWorkerTask.LogProvider(
                    $"ComboBonusManager.EnsureInitialized failed.\n{e}",
                    LogManager.LogLevelEnum.Error);
            }
        }

        #region 公开属性

        /// <summary>
        /// 当前伤害倍率。基于玩家当前连击数计算，默认 1.0（无加成）。
        /// 在 Character.ReduceHp 中自动应用于 Player 攻击者的伤害输出。
        /// </summary>
        public float DamageMultiplier
        {
            get
            {
                return this.damageMultiplier;
            }
        }

        /// <summary>
        /// 当前经验值倍率。基于玩家当前连击数计算，默认 1.0（无加成）。
        /// 在 Player.AddExperienceValue 中自动应用。
        /// </summary>
        public float ExperienceMultiplier
        {
            get
            {
                return this.experienceMultiplier;
            }
        }

        /// <summary>
        /// 当前连击数（只读，来自 GameplaySessionStats 的实时同步）。
        /// </summary>
        public int CurrentCombo
        {
            get
            {
                return this.currentCombo;
            }
        }

        /// <summary>
        /// 当前连击等级索引（0 为无加成等级）。
        /// </summary>
        public int CurrentTierIndex
        {
            get
            {
                return this.currentTierIndex;
            }
        }

        #endregion

        #region 事件

        /// <summary>
        /// 连击数变更事件。每次连击数变化时触发（包括增加和减少）。
        /// 参数：当前连击数。
        /// </summary>
        public event Action<int> OnComboChanged;

        /// <summary>
        /// 连击里程碑达成事件。连击数跨越等级阈值进入更高等级时触发。
        /// 参数：连击数、伤害倍率、经验倍率。
        /// 可用于 UI 特效、音效、成就判定等。
        /// </summary>
        public event Action<int, float, float> OnComboMilestoneReached;

        /// <summary>
        /// 连击中断事件。连击从大于 1 回落到 0 或 1 时触发。
        /// 参数：中断前的最高连击数。
        /// </summary>
        public event Action<int> OnComboBroken;

        #endregion

        #region 核心逻辑

        /// <summary>
        /// 会话统计数据变更回调。
        /// 检测连击数变化、连击中断、等级跨越，并更新增益倍率。
        /// </summary>
        /// <param name="snapshot">GameplaySessionStats 的当前快照。</param>
        private void OnStatsChanged(GameplaySessionStatsSnapshot snapshot)
        {
            int newCombo = snapshot.CurrentCombo;
            int oldCombo = this.currentCombo;

            // 连击中断检测：之前有连击（>1），现在 <=1 表示连击链断裂
            if (oldCombo > 1 && newCombo <= 1)
            {
                this.OnComboBroken?.Invoke(oldCombo);
                this.ShowComboBreakTip(oldCombo);
            }

            this.currentCombo = newCombo;
            this.RecalculateMultipliers();

            if (newCombo != oldCombo)
            {
                this.OnComboChanged?.Invoke(newCombo);
            }
        }

        /// <summary>
        /// 根据当前连击数重新计算伤害倍率和经验倍率。
        /// 从高等级向低等级遍历 Tiers，取第一个匹配的等级。
        /// 如果等级发生变化，触发里程碑事件和 UI 提示。
        /// </summary>
        private void RecalculateMultipliers()
        {
            int newTierIndex = ruleService.FindTierIndex(this.currentCombo, tierThresholds);

            float newDmgMult = ruleService.GetDamageMultiplier(newTierIndex, tierDamageMultipliers);
            float newExpMult = ruleService.GetExperienceMultiplier(newTierIndex, tierExpMultipliers);

            bool tierChanged = newTierIndex != this.currentTierIndex;
            this.currentTierIndex = newTierIndex;
            this.damageMultiplier = newDmgMult;
            this.experienceMultiplier = newExpMult;

            // 连击等级提升时触发里程碑事件和即时提示
            if (tierChanged && this.currentTierIndex > 0)
            {
                string label = ruleService.GetTierLabel(this.currentTierIndex, tierLabels);
                if (!string.IsNullOrEmpty(label))
                {
                    this.ShowComboMilestoneTip(label);
                }

                this.OnComboMilestoneReached?.Invoke(
                    this.currentCombo,
                    this.damageMultiplier,
                    this.experienceMultiplier);
            }
        }

        private void ShowComboMilestoneTip(string message)
        {
            TipHelper.Show(message, "[ComboBonus]");
        }

        private void ShowComboBreakTip(int maxCombo)
        {
            TipHelper.Show($"连击中断! 最高连击: {maxCombo}", "[ComboBonus]");
        }

        #endregion

        #region 查询方法

        /// <summary>
        /// 获取当前连击等级标签文本，供 UI 或 HUD 使用。
        /// </summary>
        /// <returns>等级标签，无等级时返回空字符串。</returns>
        public string GetCurrentTierLabel()
        {
            if (this.currentTierIndex >= 0 && this.currentTierIndex < Tiers.Length)
            {
                return Tiers[this.currentTierIndex].Label;
            }

            return string.Empty;
        }

        /// <summary>
        /// 静态查询：获取指定连击数对应的伤害倍率。
        /// 只读方法，不修改任何状态，可用于 UI 预览或配置检查。
        /// </summary>
        /// <param name="combo">连击数。</param>
        /// <returns>对应的伤害倍率。</returns>
        public static float GetDamageMultiplierForCombo(int combo)
        {
            int tierIndex = ruleService.FindTierIndex(combo, tierThresholds);
            return ruleService.GetDamageMultiplier(tierIndex, tierDamageMultipliers);
        }

        /// <summary>
        /// 静态查询：获取指定连击数对应的经验倍率。
        /// 只读方法，不修改任何状态，可用于 UI 预览或配置检查。
        /// </summary>
        /// <param name="combo">连击数。</param>
        /// <returns>对应的经验倍率。</returns>
        public static float GetExperienceMultiplierForCombo(int combo)
        {
            int tierIndex = ruleService.FindTierIndex(combo, tierThresholds);
            return ruleService.GetExperienceMultiplier(tierIndex, tierExpMultipliers);
        }

        #endregion

        /// <summary>
        /// 连击等级配置结构体。
        /// </summary>
        [Serializable]
        public struct ComboTier
        {
            /// <summary>进入该等级所需的最低连击数</summary>
            public int MinCombo;

            /// <summary>伤害倍率（如 1.5 表示 150% 伤害）</summary>
            public float DamageMultiplier;

            /// <summary>经验值倍率（如 2.0 表示 200% 经验）</summary>
            public float ExperienceMultiplier;

            /// <summary>UI 提示标签（如 "连击 x50! 无双!"），空字符串表示不提示</summary>
            public string Label;
        }
    }
}
