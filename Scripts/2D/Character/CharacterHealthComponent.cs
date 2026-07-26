namespace LAB2D.Character
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Domain.Character;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Player;
    using LAB2D.Gameplay;
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
            this.comboBonusManager = ResolveOrDefault(comboBonusManager, ServiceLocator.Get<ComboBonusManager>());
            this.waveBossRewardManager = ResolveOrDefault(waveBossRewardManager, ServiceLocator.Get<WaveBossRewardManager>());
            this.gameplaySessionStats = ResolveOrDefault(gameplaySessionStats, ServiceLocator.Get<GameplaySessionStats>());
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
            if (attacker != null && attacker.IsPlayerCharacter)
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

        public CharacterHealthDamageResult ApplyDamageToState(
            CharacterRuntimeState state,
            float def,
            float incomingHp,
            float damageTime,
            float invincibilityDuration,
            bool isPlayer,
            bool attackerIsPlayer,
            bool isCRT,
            PlayerDamagePolicy playerDamagePolicy = null,
            Character attackerCharacter = null,
            Character targetCharacter = null)
        {
            if (incomingHp <= 0f)
            {
                return new CharacterHealthDamageResult(state, 0f, false, false);
            }

            if (state.IsDead)
            {
                return new CharacterHealthDamageResult(state, 0f, false, false);
            }

            if (isPlayer && playerDamagePolicy != null)
            {
                if (playerDamagePolicy.ShouldIgnoreDamage(
                    incomingHp,
                    state.IsRespawning,
                    damageTime,
                    state.LastDamageTime,
                    invincibilityDuration))
                {
                    return new CharacterHealthDamageResult(state, 0f, false, true);
                }
            }

            float finalDamage = incomingHp;

            if (attackerIsPlayer)
            {
                float comboMult = this.comboBonusManager.DamageMultiplier;
                finalDamage *= comboMult;
                finalDamage = this.waveBossRewardManager.GetAdjustedPlayerOutgoingDamage(attackerCharacter, finalDamage);
            }

            finalDamage = this.waveBossRewardManager.GetAdjustedIncomingDamage(targetCharacter, finalDamage);
            finalDamage = this.damageCalculator.ApplyDefense(finalDamage, def);

            CharacterHealthResult healthResult = this.damageCalculator.ApplyDamageToHealth(
                state.Hp,
                finalDamage);

            CharacterRuntimeState newState = state.WithHpAndDamageTime(
                healthResult.RemainingHp,
                isPlayer ? damageTime : state.LastDamageTime);

            this.gameplaySessionStats.RecordDamageDealt(finalDamage, isCRT);
            this.gameplaySessionStats.RecordDamageTaken(finalDamage);

            return new CharacterHealthDamageResult(
                newState,
                finalDamage,
                isCRT,
                false);
        }

        public CharacterRuntimeState ApplyHealingToState(
            CharacterRuntimeState state,
            float healAmount)
        {
            float newHp = this.damageCalculator.ApplyHealingToHealth(
                state.Hp,
                state.MaxHp,
                healAmount);

            return state.WithHp(newHp);
        }

        public CharacterExperienceResult ApplyExperienceToState(
            CharacterRuntimeState state,
            int experience)
        {
            this.gameplaySessionStats.RecordExperienceGained(experience);

            LevelProgressionResult result = this.levelProgressionService.AddExperience(
                state.CurExperience,
                state.MaxExperience,
                state.Level,
                experience);

            CharacterRuntimeState newState = state.WithExperience(
                result.CurrentExperience,
                result.MaxExperience,
                result.Level);

            return new CharacterExperienceResult(newState, result.LeveledUp);
        }
    }

    public sealed class CharacterHealthDamageResult
    {
        public readonly CharacterRuntimeState NewState;
        public readonly float FinalDamage;
        public readonly bool IsCritical;
        public readonly bool WasBlocked;

        public CharacterHealthDamageResult(
            CharacterRuntimeState newState,
            float finalDamage,
            bool isCritical,
            bool wasBlocked)
        {
            this.NewState = newState;
            this.FinalDamage = finalDamage;
            this.IsCritical = isCritical;
            this.WasBlocked = wasBlocked;
        }
    }

    public sealed class CharacterExperienceResult
    {
        public readonly CharacterRuntimeState NewState;
        public readonly bool LeveledUp;

        public CharacterExperienceResult(CharacterRuntimeState newState, bool leveledUp)
        {
            this.NewState = newState;
            this.LeveledUp = leveledUp;
        }
    }
}
