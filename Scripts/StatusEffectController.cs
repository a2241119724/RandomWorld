using System;
using System.Collections.Generic;
using LAB2D;
using LAB2D.Domain.Common;
using UnityEngine;

namespace LAB2D.AgentGenerated
{
    /// <summary>
    /// 角色脚本必须实现的接口，用于从 StatusEffectController 接收状态效果。
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
        [Tooltip("要应用效果的目标角色。如果为空，将在同一 GameObject 上查找。")]
        [SerializeField] private IStatusEffectTarget target;

        private readonly Dictionary<int, StatusEffectInstance> activeEffects = new Dictionary<int, StatusEffectInstance>();
        private readonly List<int> speedEffectIDs = new List<int>();
        private readonly List<int> damageEffectIDs = new List<int>();
        private int nextEffectId;
        private IGameLogger gameLogger;

        private IGameLogger GameLogger => this.gameLogger ?? (this.gameLogger = GameLoggerFactory.Get());

        private void Awake()
        {
            if (target == null)
            {
                target = GetComponent<IStatusEffectTarget>();
                if (target == null)
                {
                    this.GameLogger.LogWarning($"StatusEffectController on {gameObject.name}: No IStatusEffectTarget found. Controller disabled.");
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
        /// 添加新的状态效果。
        /// </summary>
        /// <param name="type">效果类型。</param>
        /// <param name="duration">持续时间（秒）。</param>
        /// <param name="magnitude">
        /// 对于速度/伤害修正：倍率（1 = 正常）。对于持续治疗/伤害：每次刻度的数值。
        /// </param>
        /// <param name="tickInterval">效果触发频率（0 表示无刻度）。对于持续效果，典型值通常为 0.1 秒。</param>
        /// <returns>效果的唯一 ID，可用于手动移除。</returns>
        public int AddEffect(EffectType type, float duration, float magnitude, float tickInterval = 0f)
        {
            if (target == null)
            {
                this.GameLogger.LogError("Cannot add effect: no valid IStatusEffectTarget.");
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
        /// 按 ID 移除活动效果。
        /// </summary>
        public void RemoveEffect(int effectId)
        {
            RemoveEffectInternal(effectId);
        }

        /// <summary>
        /// 移除指定类型的所有活动效果。
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