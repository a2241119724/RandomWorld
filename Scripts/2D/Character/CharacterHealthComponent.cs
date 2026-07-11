namespace LAB2D.Character
{
    using LAB2D;
    using LAB2D.Domain.Character;
    using PlayerCharacter = LAB2D.Character.Player.Player;

    /// <summary>
    /// Applies character health, damage, and experience rules outside Unity UI presentation.
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

        public CharacterHealthResult ApplyDamage(
            Character target,
            float incomingHp,
            Character attacker,
            bool isCRT,
            System.Action<Character, float, bool, bool> damageCallback = null)
        {
            if (incomingHp <= 0)
            {
                return new CharacterHealthResult(target.CharacterDataLAB.Hp, false);
            }

            target.LastAttacker = attacker;

            bool isComboDamage = false;
            if (attacker is PlayerCharacter)
            {
                float comboMult = ComboBonusManager.Instance.DamageMultiplier;
                incomingHp *= comboMult;
                isComboDamage = comboMult > 1.0f;

                incomingHp = WaveBossRewardManager.Instance.GetAdjustedPlayerOutgoingDamage(attacker, incomingHp);
            }

            incomingHp = WaveBossRewardManager.Instance.GetAdjustedIncomingDamage(target, incomingHp);
            incomingHp = this.damageCalculator.ApplyDefense(incomingHp, target.CharacterDataLAB.DEF);

            GameplaySessionStats.Instance.RecordDamageDealt(incomingHp, isCRT);
            GameplaySessionStats.Instance.RecordDamageTaken(incomingHp);

            damageCallback?.Invoke(target, incomingHp, isCRT, isComboDamage);

            CharacterHealthResult healthResult = this.damageCalculator.ApplyDamageToHealth(
                target.CharacterDataLAB.Hp,
                incomingHp);
            target.CharacterDataLAB.Hp = healthResult.RemainingHp;

            return healthResult;
        }

        public LevelProgressionResult AddExperience(Character.CharacterData data, int experience)
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
    }
}
