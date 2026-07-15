namespace LAB2D.Character
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Domain.Character;
    using LAB2D.Gameplay;
    using PlayerCharacter = LAB2D.Character.Player.Player;

    /// <summary>
    /// Applies character health, damage, and experience rules outside Unity UI presentation.
    /// Manager dependencies are resolved via ServiceLocator with .Instance fallback
    /// for backward compatibility during the singleton migration.
    /// </summary>
    public sealed class CharacterHealthComponent
    {
        private readonly DamageCalculator damageCalculator;
        private readonly LevelProgressionService levelProgressionService;
        private readonly ComboBonusManager comboBonusManager;
        private readonly WaveBossRewardManager waveBossRewardManager;
        private readonly GameplaySessionStats gameplaySessionStats;

        public CharacterHealthComponent(
            DamageCalculator damageCalculator = null,
            LevelProgressionService levelProgressionService = null,
            ComboBonusManager comboBonusManager = null,
            WaveBossRewardManager waveBossRewardManager = null,
            GameplaySessionStats gameplaySessionStats = null)
        {
            this.damageCalculator = damageCalculator ?? new DamageCalculator();
            this.levelProgressionService = levelProgressionService ?? new LevelProgressionService();
            this.comboBonusManager = ResolveOrDefault(comboBonusManager, ComboBonusManager.Instance);
            this.waveBossRewardManager = ResolveOrDefault(waveBossRewardManager, WaveBossRewardManager.Instance);
            this.gameplaySessionStats = ResolveOrDefault(gameplaySessionStats, GameplaySessionStats.Instance);
        }

        private static T ResolveOrDefault<T>(T existing, T fallback) where T : class
        {
            if (existing != null)
            {
                return existing;
            }

            return ServiceLocator.TryGet(out T resolved) ? resolved : fallback;
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
                float comboMult = this.comboBonusManager.DamageMultiplier;
                incomingHp *= comboMult;
                isComboDamage = comboMult > 1.0f;

                incomingHp = this.waveBossRewardManager.GetAdjustedPlayerOutgoingDamage(attacker, incomingHp);
            }

            incomingHp = this.waveBossRewardManager.GetAdjustedIncomingDamage(target, incomingHp);
            incomingHp = this.damageCalculator.ApplyDefense(incomingHp, target.CharacterDataLAB.DEF);

            this.gameplaySessionStats.RecordDamageDealt(incomingHp, isCRT);
            this.gameplaySessionStats.RecordDamageTaken(incomingHp);

            damageCallback?.Invoke(target, incomingHp, isCRT, isComboDamage);

            CharacterHealthResult healthResult = this.damageCalculator.ApplyDamageToHealth(
                target.CharacterDataLAB.Hp,
                incomingHp);
            target.CharacterDataLAB.Hp = healthResult.RemainingHp;

            return healthResult;
        }

        public LevelProgressionResult AddExperience(Character.CharacterData data, int experience)
        {
            this.gameplaySessionStats.RecordExperienceGained(experience);
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
