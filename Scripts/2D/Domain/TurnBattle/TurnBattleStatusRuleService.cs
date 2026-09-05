namespace LAB2D.Domain.TurnBattle
{
    using System.Collections.Generic;
    using LAB2D.Domain.Character;

    /// <summary>
    /// 回合制状态效果规则 — 纯函数：施加/回合跳动/到期、增减益倍率聚合、状态数值算定。
    /// DOT 跳动挂在 <see cref="TurnBattleRuleService.AdvanceRound"/>（回合结束统一结算并返回演出条目）；
    /// 眩晕在 <see cref="TurnBattleRuleService.ResolveAction"/> 入口拦截。
    /// </summary>
    public static class TurnBattleStatusRuleService
    {
        /// <summary>攻击提升的施加倍率（AttackUp 的 Value）。</summary>
        public const float AttackUpMultiplier = 1.3f;

        /// <summary>攻击降低的施加倍率（AttackDown 的 Value）。</summary>
        public const float AttackDownMultiplier = 0.75f;

        /// <summary>防御提升的受伤倍率（&lt;1 减伤，DefenseUp 的 Value）。</summary>
        public const float DefenseUpIncomingMultiplier = 0.7f;

        /// <summary>防御降低的受伤倍率（&gt;1 增伤，DefenseDown 的 Value）。</summary>
        public const float DefenseDownIncomingMultiplier = 1.3f;

        private static readonly DamageCalculator Calculator = new DamageCalculator();

        /// <summary>
        /// 施加状态 — 同类单实例：已存在则刷新剩余回合并覆盖数值与来源。
        /// </summary>
        public static void Apply(TurnBattleUnit target, TurnBattleStatusKind kind, int turns, float value, long sourceUnitId, string sourceName)
        {
            if (target == null || turns <= 0)
            {
                return;
            }

            TurnBattleStatusEffect existing = GetStatus(target, kind);
            if (existing != null)
            {
                existing.RemainingTurns = turns;
                existing.Value = value;
                existing.SourceUnitId = sourceUnitId;
                existing.SourceName = sourceName;
                return;
            }

            target.StatusEffects.Add(new TurnBattleStatusEffect
            {
                Kind = kind,
                RemainingTurns = turns,
                Value = value,
                SourceUnitId = sourceUnitId,
                SourceName = sourceName,
            });
        }

        /// <summary>
        /// 回合结束统一跳动 — DOT 掉血（防御姿态减半仍生效，钳血经 DamageCalculator）
        /// → StatusTick 条目 → 致死追加 Down 条目（归因施加者）→ 递减到期移除。
        /// 非 DOT 状态只递减到期。倒下单位不再受 DOT 伤害但状态照常到期。
        /// </summary>
        public static List<TurnBattleEffectEntry> TickAndExpire(TurnBattleState state)
        {
            List<TurnBattleEffectEntry> entries = new List<TurnBattleEffectEntry>();
            foreach (TurnBattleUnit unit in state.Units)
            {
                if (unit.StatusEffects.Count == 0)
                {
                    continue;
                }

                // 倒序遍历：边递减边安全移除到期项
                for (int i = unit.StatusEffects.Count - 1; i >= 0; i--)
                {
                    TurnBattleStatusEffect status = unit.StatusEffects[i];
                    if (IsDotKind(status.Kind) && !unit.IsDown)
                    {
                        float damage = status.Value * (unit.IsDefending ? TurnBattleRuleService.GuardIncomingMultiplier : 1f);
                        CharacterHealthResult result = Calculator.ApplyDamageToHealth(unit.Hp, damage);
                        unit.Hp = result.RemainingHp;
                        entries.Add(new TurnBattleEffectEntry
                        {
                            Kind = TurnBattleEffectKind.StatusTick,
                            ActorId = -1, // 无行动者（回合末系统跳动，UI 不做前冲）
                            TargetId = unit.UnitId,
                            Value = damage,
                            StatusKind = status.Kind,
                            RemainingHp = unit.Hp,
                            SourceName = GetStatusDisplayName(status.Kind),
                        });

                        if (result.IsDead)
                        {
                            entries.Add(new TurnBattleEffectEntry
                            {
                                Kind = TurnBattleEffectKind.Down,
                                ActorId = status.SourceUnitId, // DOT 致死归因施加者（TrackDefeatKiller 复用）
                                TargetId = unit.UnitId,
                                StatusKind = status.Kind,
                                RemainingHp = unit.Hp,
                                SourceName = GetStatusDisplayName(status.Kind),
                            });
                        }
                    }

                    status.RemainingTurns--;
                    if (status.RemainingTurns <= 0)
                    {
                        unit.StatusEffects.RemoveAt(i);
                    }
                }
            }

            return entries;
        }

        /// <summary>造成伤害倍率 = SelfBuff AttackBuffMultiplier × Π 攻击增减益。</summary>
        public static float GetAttackMultiplier(TurnBattleUnit unit)
        {
            float multiplier = unit.AttackBuffMultiplier;
            foreach (TurnBattleStatusEffect status in unit.StatusEffects)
            {
                if (status.Kind == TurnBattleStatusKind.AttackUp || status.Kind == TurnBattleStatusKind.AttackDown)
                {
                    multiplier *= status.Value;
                }
            }

            return multiplier;
        }

        /// <summary>受到伤害倍率 = Π 防御增减益（防御姿态/DOT 由调用方另乘）。</summary>
        public static float GetIncomingDamageMultiplier(TurnBattleUnit unit)
        {
            float multiplier = 1f;
            foreach (TurnBattleStatusEffect status in unit.StatusEffects)
            {
                if (status.Kind == TurnBattleStatusKind.DefenseUp || status.Kind == TurnBattleStatusKind.DefenseDown)
                {
                    multiplier *= status.Value;
                }
            }

            return multiplier;
        }

        /// <summary>是否处于眩晕（行动被拦截跳过）。</summary>
        public static bool IsStunned(TurnBattleUnit unit)
        {
            return GetStatus(unit, TurnBattleStatusKind.Stun) != null;
        }

        /// <summary>取同类状态实例（无则 null）。</summary>
        public static TurnBattleStatusEffect GetStatus(TurnBattleUnit unit, TurnBattleStatusKind kind)
        {
            if (unit == null)
            {
                return null;
            }

            for (int i = 0; i < unit.StatusEffects.Count; i++)
            {
                if (unit.StatusEffects[i].Kind == kind)
                {
                    return unit.StatusEffects[i];
                }
            }

            return null;
        }

        /// <summary>状态显示名（UI 飘字/日志/顺序条共用）。</summary>
        public static string GetStatusDisplayName(TurnBattleStatusKind kind)
        {
            switch (kind)
            {
                case TurnBattleStatusKind.Poison: return "中毒";
                case TurnBattleStatusKind.Burn: return "灼烧";
                case TurnBattleStatusKind.Bleed: return "流血";
                case TurnBattleStatusKind.Stun: return "眩晕";
                case TurnBattleStatusKind.AttackUp: return "攻击提升";
                case TurnBattleStatusKind.AttackDown: return "攻击降低";
                case TurnBattleStatusKind.DefenseUp: return "防御提升";
                case TurnBattleStatusKind.DefenseDown: return "防御降低";
                default: return kind.ToString();
            }
        }

        /// <summary>是否 DOT（回合结束掉血）类状态。</summary>
        public static bool IsDotKind(TurnBattleStatusKind kind)
        {
            return kind == TurnBattleStatusKind.Poison
                || kind == TurnBattleStatusKind.Burn
                || kind == TurnBattleStatusKind.Bleed;
        }

        /// <summary>
        /// 施加时算定状态数值 — DOT：Poison/Bleed = 比率 × 目标 MaxHp、Burn = 比率 × 施加者基数（ScaleByInt 取 INT）；
        /// 增减益：固定常量倍率；眩晕未用（0）。
        /// </summary>
        public static float CalculateStatusValue(TurnBattleStatusKind kind, TurnBattleSkillState skill, TurnBattleUnit attacker, TurnBattleUnit target)
        {
            switch (kind)
            {
                case TurnBattleStatusKind.Poison:
                case TurnBattleStatusKind.Bleed:
                    return skill.StatusValueRatio * (target != null ? target.MaxHp : 0f);

                case TurnBattleStatusKind.Burn:
                    return skill.StatusValueRatio * (skill.ScaleByInt ? attacker.Stats.INT : attacker.Stats.ATN);

                case TurnBattleStatusKind.AttackUp:
                    return AttackUpMultiplier;

                case TurnBattleStatusKind.AttackDown:
                    return AttackDownMultiplier;

                case TurnBattleStatusKind.DefenseUp:
                    return DefenseUpIncomingMultiplier;

                case TurnBattleStatusKind.DefenseDown:
                    return DefenseDownIncomingMultiplier;

                default:
                    return 0f;
            }
        }
    }
}
