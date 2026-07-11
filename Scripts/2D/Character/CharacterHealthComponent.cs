namespace LAB2D.Character
{
    using LAB2D;
    using LAB2D.Domain.Character;

    /// <summary>
    /// 角色生命值组件 — 封装伤害计算流程，分离领域逻辑与 Unity UI 表现。
    ///
    /// 职责:
    ///   1. 计算最终伤害（连击加成、波间奖励、防御减伤）
    ///   2. 应用伤害到生命值并判断死亡
    ///   3. 通过回调委托通知外部进行 UI 表现（伤害数字、闪烁效果、浮动文字）
    ///
    /// 依赖注入: 通过构造函数注入领域服务，支持 ServiceLocator 回退。
    /// </summary>
    public sealed class CharacterHealthComponent
    {
        private readonly DamageCalculator damageCalculator;
        private readonly LevelProgressionService levelProgressionService;

        public CharacterHealthComponent(
            DamageCalculator damageCalculator = null,
            LevelProgressionService levelProgressionService = null)
        {
            this.damageCalculator = damageCalculator ?? new DamageCalculator();
            this.levelProgressionService = levelProgressionService ?? new LevelProgressionService();
        }

        /// <summary>
        /// 对角色应用伤害。
        /// </summary>
        /// <param name="target">受伤角色。</param>
        /// <param name="incomingHp">原始伤害值。</param>
        /// <param name="attacker">攻击者（用于判断连击加成）。</param>
        /// <param name="isCRT">是否暴击。</param>
        /// <param name="damageCallback">伤害显示回调（target, finalHp, isCRT, isCombo）。</param>
        /// <returns>伤害处理结果。</returns>
        public CharacterHealthResult ApplyDamage(
            Character target,
            float incomingHp,
            Character attacker,
            bool isCRT,
            System.Action<Character, float, bool, bool> damageCallback = null)
        {
            if (incomingHp <= 0)
            {
                return new CharacterHealthResult { RemainingHp = target.CharacterDataLAB.Hp, IsDead = false };
            }

            target.LastAttacker = attacker;

            // 连击增益
            bool isComboDamage = false;
            if (attacker is Player)
            {
                float comboMult = ComboBonusManager.Instance.DamageMultiplier;
                incomingHp *= comboMult;
                isComboDamage = comboMult > 1.0f;

                // 波间奖励 — 玩家输出强化
                incomingHp = WaveBossRewardManager.Instance.GetAdjustedPlayerOutgoingDamage(attacker, incomingHp);
            }

            // 波间奖励 — 受击减伤
            incomingHp = WaveBossRewardManager.Instance.GetAdjustedIncomingDamage(target, incomingHp);

            // 防御减伤
            incomingHp = this.damageCalculator.ApplyDefense(incomingHp, target.CharacterDataLAB.DEF);

            // 记录战斗统计
            GameplaySessionStats.Instance.RecordDamageDealt(incomingHp, isCRT);
            GameplaySessionStats.Instance.RecordDamageTaken(incomingHp);

            // 通知外部进行 UI 表现
            damageCallback?.Invoke(target, incomingHp, isCRT, isComboDamage);

            // 应用伤害到生命值
            CharacterHealthResult healthResult = this.damageCalculator.ApplyDamageToHealth(
                target.CharacterDataLAB.Hp, incomingHp);
            target.CharacterDataLAB.Hp = healthResult.RemainingHp;

            return healthResult;
        }

        /// <summary>
        /// 增加经验值。
        /// </summary>
        public LevelProgressionResult AddExperience(CharacterData data, int experience)
        {
            GameplaySessionStats.Instance.RecordExperienceGained(experience);
            LevelProgressionResult result = this.levelProgressionService.AddExperience(
                data.CurExperience,
                data.MaxExperience,
                data.Level,
                experience);
            data.CurExperience = result.CurrentExperience;
            data.MaxExperience = result.MaxExperience;
            data.Level = result.Level;
            return result;
        }

        /// <summary>
        /// CharacterData 类型别名，避免命名冲突。
        /// </summary>
        public class CharacterData
        {
        }
    }
}
