namespace LAB2D.Character
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Domain.Character;
    using LAB2D.Domain.Character.Growth;
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

            this.ApplyGrowthCombatEffects(attacker, target, incomingHp);

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

            this.ApplyGrowthCombatEffects(attackerCharacter, targetCharacter, finalDamage);

            return new CharacterHealthDamageResult(
                newState,
                finalDamage,
                isCRT,
                false);
        }

        /// <summary>
        /// 词条成长系统的战斗事件效果：攻击方吸血、受击方反伤。
        /// 比例读取自双方 GrowthData.Special（ComputeAttribute 时由 GrowthCollectProvider 快照）。
        /// 反伤经 attacker.ReduceHp 再次进入本组件时对方通常无反伤词条即终止，无递归风险。
        /// </summary>
        /// <param name="attacker">攻击方（可为 null，如环境伤害）。</param>
        /// <param name="target">受击方。</param>
        /// <param name="finalDamage">经过防御/加成后的最终伤害。</param>
        private void ApplyGrowthCombatEffects(Character attacker, Character target, float finalDamage)
        {
            if (finalDamage <= 0f || attacker == null || target == null || attacker == target)
            {
                return;
            }

            GrowthData attackerGrowth = attacker.CharacterDataLAB?.Growth;
            float lifestealRatio = attackerGrowth?.Special.LifestealRatio ?? 0f;
            if (lifestealRatio > 0f)
            {
                float heal = finalDamage * lifestealRatio;
                attacker.Heal(heal);
                AWorkerTask.LogProvider(
                    $"[AffixDiag] {attacker.name} 吸血 {heal:F1}（率 {lifestealRatio:P1}，伤害 {finalDamage:F1}）",
                    LogManager.LogLevelEnum.Trace);
            }

            GrowthData targetGrowth = target.CharacterDataLAB?.Growth;
            float reflectRatio = targetGrowth?.Special.ReflectRatio ?? 0f;
            if (reflectRatio > 0f)
            {
                float reflectDamage = finalDamage * reflectRatio;
                attacker.ReduceHp(reflectDamage, target);
                AWorkerTask.LogProvider(
                    $"[AffixDiag] {target.name} 反伤 {attacker.name} {reflectDamage:F1}（率 {reflectRatio:P1}）",
                    LogManager.LogLevelEnum.Trace);
            }
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
