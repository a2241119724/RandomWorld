using System;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D.AgentGenerated
{
    /// <summary>
    /// Interface that character scripts must implement to receive status effects from StatusEffectController.
    /// </summary>
    public interface IStatusEffectTarget
    {
        void SetSpeedMultiplier(float multiplier);
        void SetDamageMultiplier(float multiplier);
        void ApplyHeal(float amount);
        void ApplyDamage(float amount);
    }

    public enum EffectType
    {
        SpeedModifier,
        DamageModifier,
        HealOverTime,
        DamageOverTime
    }

    public class StatusEffectController : MonoBehaviour
    {
        [Tooltip("Target character to apply effects to. If null, will be found on the same GameObject.")]
        [SerializeField] private IStatusEffectTarget target;

        private readonly Dictionary<int, StatusEffectInstance> activeEffects = new Dictionary<int, StatusEffectInstance>();
        private readonly List<int> speedEffectIDs = new List<int>();
        private readonly List<int> damageEffectIDs = new List<int>();
        private int nextEffectId;

        private void Awake()
        {
            if (target == null)
            {
                target = GetComponent<IStatusEffectTarget>();
                if (target == null)
                {
                    Debug.LogWarning($"StatusEffectController on {gameObject.name}: No IStatusEffectTarget found. Controller disabled.", this);
                    enabled = false;
                }
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            List<int> expiredIds = null;

            foreach (var kvp in activeEffects)
            {
                StatusEffectInstance effect = kvp.Value;
                effect.durationRemaining -= deltaTime;
                bool expired = effect.durationRemaining <= 0f;

                if (effect.tickInterval > 0)
                {
                    effect.tickTimer += deltaTime;
                    while (effect.tickTimer >= effect.tickInterval)
                    {
                        effect.tickTimer -= effect.tickInterval;
                        ApplyTick(effect);
                    }
                }

                if (expired)
                {
                    if (expiredIds == null) expiredIds = new List<int>();
                    expiredIds.Add(kvp.Key);
                }
            }

            if (expiredIds != null)
            {
                foreach (int id in expiredIds)
                {
                    RemoveEffectInternal(id);
                }
            }
        }

        /// <summary>
        /// Adds a new status effect.
        /// </summary>
        /// <param name="type">Type of effect.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="magnitude">
        /// For Speed/Damage modifiers: multiplier (1 = normal). For Heal/Damage over time: amount per tick.
        /// </param>
        /// <param name="tickInterval">How often the effect applies (0 means no ticking). For over‑time effects a typical value is 0.1.</param>
        /// <returns>Unique ID of the effect, can be used for manual removal.</returns>
        public int AddEffect(EffectType type, float duration, float magnitude, float tickInterval = 0f)
        {
            if (target == null)
            {
                Debug.LogError("Cannot add effect: no valid IStatusEffectTarget.");
                return -1;
            }

            int id = nextEffectId++;
            StatusEffectInstance instance = new StatusEffectInstance
            {
                type = type,
                durationRemaining = duration,
                magnitude = magnitude,
                tickInterval = tickInterval,
                tickTimer = 0f
            };

            activeEffects[id] = instance;

            switch (type)
            {
                case EffectType.SpeedModifier:
                    speedEffectIDs.Add(id);
                    RecalculateSpeedMultiplier();
                    break;
                case EffectType.DamageModifier:
                    damageEffectIDs.Add(id);
                    RecalculateDamageMultiplier();
                    break;
            }

            return id;
        }

        /// <summary>
        /// Removes an active effect by its ID.
        /// </summary>
        public void RemoveEffect(int effectId)
        {
            RemoveEffectInternal(effectId);
        }

        /// <summary>
        /// Removes all active effects of the given type.
        /// </summary>
        public void RemoveAllEffectsOfType(EffectType type)
        {
            List<int> toRemove = new List<int>();
            foreach (var kvp in activeEffects)
            {
                if (kvp.Value.type == type)
                    toRemove.Add(kvp.Key);
            }

            foreach (int id in toRemove)
            {
                RemoveEffectInternal(id);
            }
        }

        private void RemoveEffectInternal(int effectId)
        {
            if (!activeEffects.TryGetValue(effectId, out StatusEffectInstance effect))
                return;

            switch (effect.type)
            {
                case EffectType.SpeedModifier:
                    speedEffectIDs.Remove(effectId);
                    RecalculateSpeedMultiplier();
                    break;
                case EffectType.DamageModifier:
                    damageEffectIDs.Remove(effectId);
                    RecalculateDamageMultiplier();
                    break;
            }

            activeEffects.Remove(effectId);
        }

        private void ApplyTick(StatusEffectInstance effect)
        {
            if (target == null) return;

            switch (effect.type)
            {
                case EffectType.HealOverTime:
                    target.ApplyHeal(effect.magnitude);
                    break;
                case EffectType.DamageOverTime:
                    target.ApplyDamage(effect.magnitude);
                    break;
            }
        }

        private void RecalculateSpeedMultiplier()
        {
            float total = 1f;
            foreach (int id in speedEffectIDs)
            {
                if (activeEffects.TryGetValue(id, out var eff))
                    total *= eff.magnitude;
            }
            target.SetSpeedMultiplier(total);
        }

        private void RecalculateDamageMultiplier()
        {
            float total = 1f;
            foreach (int id in damageEffectIDs)
            {
                if (activeEffects.TryGetValue(id, out var eff))
                    total *= eff.magnitude;
            }
            target.SetDamageMultiplier(total);
        }

        private class StatusEffectInstance
        {
            public EffectType type;
            public float durationRemaining;
            public float magnitude;
            public float tickInterval;
            public float tickTimer;
        }
    }
}