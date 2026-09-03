namespace LAB2D.Domain.TurnBattle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LAB2D.Domain.Character;
    using SkillType = LAB2D.Enum.SkillType;

    /// <summary>
    /// 回合制战斗规则引擎 — 纯函数结算，不持有状态。
    /// 随机依赖通过 <see cref="RandomFloatProvider"/> 注入（LingGenRuleService 同款模式），
    /// 未注入时随机取中性值 0.5（必中/不暴击，仅供防御兜底，测试请注入桩）。
    /// 伤害复用 <see cref="DamageCalculator"/>（防御减免 dmg×(1−def/10) 下限 0.1、暴击乘 CSD、钳血）。
    /// </summary>
    public static class TurnBattleRuleService
    {
        /// <summary>随机浮点数提供者 (minInclusive, maxInclusive)。由 Gameplay 层注入 UnityEngine.Random。</summary>
        public static Func<float, float, float> RandomFloatProvider { get; set; }

        private static readonly DamageCalculator Calculator = new DamageCalculator();

        /// <summary>实时秒 → 回合的换算基准（1 回合 ≈ 3 秒战斗节奏）。</summary>
        public const float SecondsPerTurn = 3f;

        /// <summary>普攻倍率（永远可用、0 耗蓝 — MP 不足的兜底行动）。</summary>
        public const float NormalAttackMultiplier = 1f;

        /// <summary>基础命中率（双方 HIT 相同时）。</summary>
        public const float BaseHitRate = 0.90f;

        /// <summary>命中率随攻守 HIT 差的修正斜率。</summary>
        public const float HitRatePerHitDiff = 0.5f;

        public const float MinHitRate = 0.50f;

        public const float MaxHitRate = 1.00f;

        /// <summary>伤害/治疗随技能等级的成长（每级 +10%）。</summary>
        public const float LevelBonusPerLevel = 0.1f;

        /// <summary>SelfAOE 技能改为打敌方全体时的单体衰减（打全体 ×0.6）。</summary>
        public const float AoeDamageMultiplier = 0.6f;

        /// <summary>逃跑基础成功率。</summary>
        public const float BaseFleeRate = 0.50f;

        /// <summary>我方均 SPD 每高于敌均 1 点的逃跑加成。</summary>
        public const float FleeRatePerSpdDiff = 0.03f;

        /// <summary>每个存活敌人对逃跑成功的压制。</summary>
        public const float FleeRatePerAliveEnemy = 0.05f;

        /// <summary>每次逃跑失败后的成功率补偿（鼓励再试）。</summary>
        public const float FleeRatePerAttempt = 0.15f;

        public const float MinFleeRate = 0.25f;

        public const float MaxFleeRate = 0.95f;

        /// <summary>敌方 AI 目标选择偏向 HP 最低我方的概率。</summary>
        public const float LowHpTargetBias = 0.4f;

        /// <summary>敌方 AI 治疗技能的考虑门槛（自身 HP 低于此比例才评估治疗）。</summary>
        public const float AiHealConsiderHpRatio = 0.5f;

        /// <summary>
        /// 行动顺序 — 存活单位 SPD 降序，同 SPD 我方先（Ally 枚举值小）。
        /// 每回合重新计算。
        /// </summary>
        public static List<TurnBattleUnit> DetermineTurnOrder(TurnBattleState state)
        {
            return state.Units
                .Where(u => u.CanAct)
                .OrderByDescending(u => u.Stats.SPD)
                .ThenBy(u => u.Faction)
                .ToList();
        }

        /// <summary>命中率 = clamp(0.90 + (攻 HIT − 守 HIT) × 0.5, 0.5, 1.0)。</summary>
        public static float GetHitRate(TurnBattleUnit attacker, TurnBattleUnit target)
        {
            float rate = BaseHitRate + ((attacker.Stats.HIT - target.Stats.HIT) * HitRatePerHitDiff);
            return Clamp(rate, MinHitRate, MaxHitRate);
        }

        /// <summary>命中判定。</summary>
        public static bool RollHit(TurnBattleUnit attacker, TurnBattleUnit target)
        {
            return Roll() < GetHitRate(attacker, target);
        }

        /// <summary>暴击判定（CRT 已是 0-1 比例）。</summary>
        public static bool RollCritical(TurnBattleUnit attacker)
        {
            return Roll() < attacker.Stats.CRT;
        }

        /// <summary>
        /// 伤害公式：基数(ATN/INT) × 倍率 × (1 + 0.1×(等级−1)) × 攻击增益，
        /// 暴击乘 CSD，再乘五行克制，最后防御减免（下限 0.1）。
        /// </summary>
        public static float CalculateDamage(TurnBattleUnit attacker, TurnBattleUnit target, bool useInt, float multiplier, bool isCritical, float elementMultiplier)
        {
            float baseStat = useInt ? attacker.Stats.INT : attacker.Stats.ATN;
            float raw = baseStat * multiplier * (1f + (LevelBonusPerLevel * (attacker.Level - 1)));
            raw *= attacker.AttackBuffMultiplier;
            raw = Calculator.GetOutgoingDamage(raw, attacker.Stats.CSD, isCritical);
            raw *= elementMultiplier;
            return Calculator.ApplyDefense(raw, target.Stats.DEF);
        }

        /// <summary>治疗量 = 基础量 × (1 + 0.1×(等级−1))；钳 MaxHp 由调用处经 ApplyHealingToHealth 完成。</summary>
        public static float CalculateHeal(TurnBattleUnit actor, float baseAmount)
        {
            return baseAmount * (1f + (LevelBonusPerLevel * (actor.Level - 1)));
        }

        /// <summary>实时秒 → 回合数：≤1 回合的短冷却归 0（只耗蓝）；其余四舍五入。</summary>
        public static int ConvertSecondsToTurns(float seconds)
        {
            if (seconds <= SecondsPerTurn)
            {
                return 0;
            }

            return (int)Math.Round(seconds / SecondsPerTurn, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// 逃跑成功率 = clamp(0.50 + (我方均 SPD − 敌均 SPD)×0.03 − 存活敌数×0.05 + 失败次数×0.15, 0.25, 0.95)。
        /// </summary>
        public static float GetFleeSuccessRate(TurnBattleState state)
        {
            List<TurnBattleUnit> allies = state.AliveAllies;
            List<TurnBattleUnit> enemies = state.AliveEnemies;
            float allyAvgSpd = allies.Count > 0 ? AverageSpd(allies) : 0f;
            float enemyAvgSpd = enemies.Count > 0 ? AverageSpd(enemies) : 0f;
            float rate = BaseFleeRate
                + ((allyAvgSpd - enemyAvgSpd) * FleeRatePerSpdDiff)
                - (enemies.Count * FleeRatePerAliveEnemy)
                + (state.FleeAttemptCount * FleeRatePerAttempt);
            return Clamp(rate, MinFleeRate, MaxFleeRate);
        }

        /// <summary>
        /// 敌方 AI 行动选择 — 可用技能中期望收益最高者，无可用技能退化为普攻。
        /// 目标选择：40% 偏向 HP 最低我方，否则随机。
        /// </summary>
        public static TurnBattleAction SelectEnemyAction(TurnBattleState state, TurnBattleUnit actor)
        {
            TurnBattleAction action = new TurnBattleAction();
            List<TurnBattleUnit> targets = state.AliveAllies;
            if (targets.Count == 0)
            {
                action.Kind = TurnBattleActionKind.Attack;
                return action;
            }

            TurnBattleUnit target = SelectTarget(targets);
            TurnBattleSkillState best = null;
            float bestScore = 0f;
            for (int i = 0; i < actor.Skills.Count; i++)
            {
                TurnBattleSkillState skill = actor.Skills[i];
                if (!skill.IsUsable(actor.Mp))
                {
                    continue;
                }

                float score = EstimateSkillScore(actor, skill, target, targets.Count);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = skill;
                }
            }

            if (best != null)
            {
                action.Kind = TurnBattleActionKind.UseSkill;
                action.SkillIndex = actor.Skills.IndexOf(best);
                action.TargetUnitId = target.UnitId;
            }
            else
            {
                action.Kind = TurnBattleActionKind.Attack;
                action.TargetUnitId = target.UnitId;
            }

            return action;
        }

        /// <summary>
        /// 结算一次行动 — 修改快照状态（扣蓝/冷却/血量/增益），返回按序演出的效果列表。
        /// 行动者已倒下时返回空结算（回合序已过滤，此处防御）。
        /// </summary>
        public static TurnBattleActionResolution ResolveAction(TurnBattleState state, TurnBattleUnit actor, TurnBattleAction action)
        {
            TurnBattleActionResolution resolution = new TurnBattleActionResolution
            {
                ActorId = actor != null ? actor.UnitId : -1,
                ActorFaction = actor != null ? actor.Faction : TurnBattleFaction.Ally,
                Round = state.Round,
                ActionKind = action != null ? action.Kind : TurnBattleActionKind.Attack,
            };

            if (actor == null || action == null || !actor.CanAct)
            {
                return resolution;
            }

            switch (action.Kind)
            {
                case TurnBattleActionKind.Attack:
                    ResolveStrike(state, actor, action.TargetUnitId, resolution, null);
                    break;

                case TurnBattleActionKind.UseSkill:
                    ResolveSkill(state, actor, action, resolution);
                    break;

                case TurnBattleActionKind.UseItem:
                    ResolveItem(actor, action, resolution);
                    break;

                case TurnBattleActionKind.Flee:
                    ResolveFlee(state, resolution);
                    break;
            }

            return resolution;
        }

        /// <summary>
        /// 战斗结束判定 — 敌方全灭 = Victory；玩家倒下 = Defeat（立即判负，Worker 倒下不判负）。
        /// 返回 null 表示继续战斗。
        /// </summary>
        public static TurnBattleResult? EvaluateBattleResult(TurnBattleState state)
        {
            if (state.AliveEnemies.Count == 0)
            {
                return TurnBattleResult.Victory;
            }

            TurnBattleUnit player = state.PlayerUnit;
            if (player == null || player.IsDown)
            {
                return TurnBattleResult.Defeat;
            }

            return null;
        }

        /// <summary>回合推进 — 回合数 +1、技能冷却集体递减、攻击增益到期清除。</summary>
        public static void AdvanceRound(TurnBattleState state)
        {
            state.Round++;
            foreach (TurnBattleUnit unit in state.Units)
            {
                foreach (TurnBattleSkillState skill in unit.Skills)
                {
                    if (skill.RemainingCooldown > 0)
                    {
                        skill.RemainingCooldown--;
                    }
                }

                if (unit.AttackBuffTurns > 0)
                {
                    unit.AttackBuffTurns--;
                    if (unit.AttackBuffTurns == 0)
                    {
                        unit.AttackBuffMultiplier = 1f;
                    }
                }
            }
        }

        /// <summary>普攻与单体技能共用的一次打击结算（命中/暴击/克制/伤害/倒下）。</summary>
        private static void ResolveStrike(TurnBattleState state, TurnBattleUnit actor, long targetUnitId, TurnBattleActionResolution resolution, TurnBattleSkillState skill)
        {
            TurnBattleUnit target = ResolveTarget(state, actor, targetUnitId);
            if (target == null)
            {
                return;
            }

            float elementMultiplier = skill != null && skill.Element != null
                ? ElementCounterRuleService.GetCounterMultiplier(skill.Element, target.LingGenElements)
                : ElementCounterRuleService.NeutralMultiplier;

            if (!RollHit(actor, target))
            {
                resolution.Effects.Add(new TurnBattleEffectEntry
                {
                    Kind = TurnBattleEffectKind.Miss,
                    ActorId = actor.UnitId,
                    TargetId = target.UnitId,
                    SourceName = skill != null ? skill.SkillName : string.Empty,
                });
                return;
            }

            bool isCritical = RollCritical(actor);
            float multiplier = skill != null ? skill.EffectMultiplier : NormalAttackMultiplier;
            float damage = CalculateDamage(actor, target, skill != null && skill.ScaleByInt, multiplier, isCritical, elementMultiplier);
            bool isDown = ApplyDamage(target, damage);

            resolution.Effects.Add(new TurnBattleEffectEntry
            {
                Kind = TurnBattleEffectKind.Damage,
                ActorId = actor.UnitId,
                TargetId = target.UnitId,
                Value = damage,
                IsCritical = isCritical,
                ElementMultiplier = elementMultiplier,
                RemainingHp = target.Hp,
                SourceName = skill != null ? skill.SkillName : string.Empty,
            });

            if (isDown)
            {
                resolution.Effects.Add(new TurnBattleEffectEntry
                {
                    Kind = TurnBattleEffectKind.Down,
                    ActorId = actor.UnitId,
                    TargetId = target.UnitId,
                    RemainingHp = target.Hp,
                });
            }
        }

        /// <summary>技能结算 — 校验可用性（不可用回退普攻）、扣蓝、进冷却，按类型分派效果。</summary>
        private static void ResolveSkill(TurnBattleState state, TurnBattleUnit actor, TurnBattleAction action, TurnBattleActionResolution resolution)
        {
            TurnBattleSkillState skill = action.SkillIndex >= 0 && action.SkillIndex < actor.Skills.Count
                ? actor.Skills[action.SkillIndex]
                : null;
            if (skill == null || !skill.IsUsable(actor.Mp))
            {
                // UI 已灰显不可用项，此处为防御回退：按普攻结算
                resolution.ActionKind = TurnBattleActionKind.Attack;
                ResolveStrike(state, actor, action.TargetUnitId, resolution, null);
                return;
            }

            actor.Mp -= skill.ManaCost;
            skill.RemainingCooldown = skill.CooldownTurns;

            switch (skill.Type)
            {
                case SkillType.SingleTarget:
                    ResolveStrike(state, actor, action.TargetUnitId, resolution, skill);
                    break;

                case SkillType.SelfAOE:
                    ResolveAoe(state, actor, resolution, skill);
                    break;

                case SkillType.SelfHeal:
                    ResolveHeal(actor, skill, resolution);
                    break;

                case SkillType.SelfBuff:
                    actor.AttackBuffTurns = skill.BuffDurationTurns;
                    actor.AttackBuffMultiplier = skill.EffectMultiplier;
                    resolution.Effects.Add(new TurnBattleEffectEntry
                    {
                        Kind = TurnBattleEffectKind.Buff,
                        ActorId = actor.UnitId,
                        TargetId = actor.UnitId,
                        Value = skill.EffectMultiplier,
                        SourceName = skill.SkillName,
                    });
                    break;

                default:
                    // Movement/Pull 在快照阶段已被过滤；未知类型按普攻兜底
                    resolution.ActionKind = TurnBattleActionKind.Attack;
                    ResolveStrike(state, actor, action.TargetUnitId, resolution, null);
                    break;
            }
        }

        /// <summary>SelfAOE — 对敌方全体各结算一次，单体伤害 ×0.6。</summary>
        private static void ResolveAoe(TurnBattleState state, TurnBattleUnit actor, TurnBattleActionResolution resolution, TurnBattleSkillState skill)
        {
            List<TurnBattleUnit> targets = actor.Faction == TurnBattleFaction.Ally ? state.AliveEnemies : state.AliveAllies;
            // 快照遍历中 ApplyDamage 只改 Hp 不增删列表，安全；倒下单位在本次 AOE 内不再被选（列表为进入时快照）
            foreach (TurnBattleUnit target in targets.ToList())
            {
                if (target.IsDown)
                {
                    continue;
                }

                float elementMultiplier = skill.Element != null
                    ? ElementCounterRuleService.GetCounterMultiplier(skill.Element, target.LingGenElements)
                    : ElementCounterRuleService.NeutralMultiplier;
                if (!RollHit(actor, target))
                {
                    resolution.Effects.Add(new TurnBattleEffectEntry
                    {
                        Kind = TurnBattleEffectKind.Miss,
                        ActorId = actor.UnitId,
                        TargetId = target.UnitId,
                        SourceName = skill.SkillName,
                    });
                    continue;
                }

                bool isCritical = RollCritical(actor);
                float damage = CalculateDamage(
                    actor, target, skill.ScaleByInt,
                    skill.EffectMultiplier * AoeDamageMultiplier, isCritical, elementMultiplier);
                bool isDown = ApplyDamage(target, damage);
                resolution.Effects.Add(new TurnBattleEffectEntry
                {
                    Kind = TurnBattleEffectKind.Damage,
                    ActorId = actor.UnitId,
                    TargetId = target.UnitId,
                    Value = damage,
                    IsCritical = isCritical,
                    ElementMultiplier = elementMultiplier,
                    RemainingHp = target.Hp,
                    SourceName = skill.SkillName,
                });

                if (isDown)
                {
                    resolution.Effects.Add(new TurnBattleEffectEntry
                    {
                        Kind = TurnBattleEffectKind.Down,
                        ActorId = actor.UnitId,
                        TargetId = target.UnitId,
                        RemainingHp = target.Hp,
                    });
                }
            }
        }

        /// <summary>SelfHeal — 治疗自身并钳 MaxHp。</summary>
        private static void ResolveHeal(TurnBattleUnit actor, TurnBattleSkillState skill, TurnBattleActionResolution resolution)
        {
            float heal = CalculateHeal(actor, skill.EffectMultiplier);
            float newHp = Calculator.ApplyHealingToHealth(actor.Hp, actor.MaxHp, heal);
            resolution.Effects.Add(new TurnBattleEffectEntry
            {
                Kind = TurnBattleEffectKind.Heal,
                ActorId = actor.UnitId,
                TargetId = actor.UnitId,
                Value = newHp - actor.Hp,
                RemainingHp = newHp,
                SourceName = skill.SkillName,
            });
            actor.Hp = newHp;
        }

        /// <summary>道具结算 — 固定治疗量（不吃等级成长），实际扣背包由 Manager 完成。</summary>
        private static void ResolveItem(TurnBattleUnit actor, TurnBattleAction action, TurnBattleActionResolution resolution)
        {
            float newHp = Calculator.ApplyHealingToHealth(actor.Hp, actor.MaxHp, action.ItemHealAmount);
            resolution.Effects.Add(new TurnBattleEffectEntry
            {
                Kind = TurnBattleEffectKind.Heal,
                ActorId = actor.UnitId,
                TargetId = actor.UnitId,
                Value = newHp - actor.Hp,
                RemainingHp = newHp,
                SourceName = action.ItemDisplayName,
            });
            actor.Hp = newHp;
        }

        /// <summary>逃跑结算 — 成功即定 Escaped 结束战斗；失败累计次数供成功率递增。</summary>
        private static void ResolveFlee(TurnBattleState state, TurnBattleActionResolution resolution)
        {
            float rate = GetFleeSuccessRate(state);
            if (Roll() < rate)
            {
                resolution.Effects.Add(new TurnBattleEffectEntry
                {
                    Kind = TurnBattleEffectKind.FleeSuccess,
                    ActorId = state.PlayerUnit != null ? state.PlayerUnit.UnitId : -1,
                });
                state.Phase = TurnBattlePhase.BattleOver;
                state.Result = TurnBattleResult.Escaped;
            }
            else
            {
                state.FleeAttemptCount++;
                resolution.Effects.Add(new TurnBattleEffectEntry
                {
                    Kind = TurnBattleEffectKind.FleeFail,
                    ActorId = state.PlayerUnit != null ? state.PlayerUnit.UnitId : -1,
                });
            }
        }

        /// <summary>目标解析 — 指定目标失效（倒下/同阵营/不存在）时自动取对面首个存活。</summary>
        private static TurnBattleUnit ResolveTarget(TurnBattleState state, TurnBattleUnit actor, long targetUnitId)
        {
            TurnBattleUnit target = targetUnitId >= 0 ? state.FindUnit(targetUnitId) : null;
            if (target == null || target.IsDown || target.Faction == actor.Faction)
            {
                List<TurnBattleUnit> opponents = actor.Faction == TurnBattleFaction.Ally ? state.AliveEnemies : state.AliveAllies;
                target = opponents.Count > 0 ? opponents[0] : null;
            }

            return target;
        }

        /// <summary>敌方 AI 目标选择：40% 偏向 HP 最低，否则均匀随机。</summary>
        private static TurnBattleUnit SelectTarget(List<TurnBattleUnit> targets)
        {
            if (targets.Count == 1)
            {
                return targets[0];
            }

            if (Roll() < LowHpTargetBias)
            {
                TurnBattleUnit lowest = targets[0];
                for (int i = 1; i < targets.Count; i++)
                {
                    if (targets[i].Hp < lowest.Hp)
                    {
                        lowest = targets[i];
                    }
                }

                return lowest;
            }

            int index = (int)(Roll() * targets.Count);
            if (index < 0)
            {
                index = 0;
            }
            else if (index >= targets.Count)
            {
                index = targets.Count - 1;
            }

            return targets[index];
        }

        /// <summary>AI 技能评分 — 伤害类取期望伤害（AOE ×目标数），治疗看缺口，增益粗估。</summary>
        private static float EstimateSkillScore(TurnBattleUnit actor, TurnBattleSkillState skill, TurnBattleUnit target, int targetCount)
        {
            switch (skill.Type)
            {
                case SkillType.SingleTarget:
                    return CalculateDamage(
                        actor, target, skill.ScaleByInt, skill.EffectMultiplier,
                        false, ElementCounterRuleService.GetCounterMultiplier(skill.Element, target.LingGenElements));

                case SkillType.SelfAOE:
                    return CalculateDamage(
                        actor, target, skill.ScaleByInt, skill.EffectMultiplier * AoeDamageMultiplier,
                        false, ElementCounterRuleService.GetCounterMultiplier(skill.Element, target.LingGenElements)) * targetCount;

                case SkillType.SelfHeal:
                    if (actor.Hp >= actor.MaxHp * AiHealConsiderHpRatio || actor.Hp >= actor.MaxHp)
                    {
                        return 0f;
                    }

                    return Math.Min(CalculateHeal(actor, skill.EffectMultiplier), actor.MaxHp - actor.Hp);

                case SkillType.SelfBuff:
                    return actor.Stats.ATN * (skill.EffectMultiplier - 1f) * 0.5f;

                default:
                    return 0f;
            }
        }

        /// <summary>扣血并返回是否倒下（HP 落 0）。</summary>
        private static bool ApplyDamage(TurnBattleUnit target, float damage)
        {
            CharacterHealthResult result = Calculator.ApplyDamageToHealth(target.Hp, damage);
            target.Hp = result.RemainingHp;
            return result.IsDead;
        }

        private static float AverageSpd(List<TurnBattleUnit> units)
        {
            float sum = 0f;
            foreach (TurnBattleUnit unit in units)
            {
                sum += unit.Stats.SPD;
            }

            return units.Count > 0 ? sum / units.Count : 0f;
        }

        private static float Roll()
        {
            // 未注入时的中性兜底：0.5 < 默认命中 0.90（必中）、> 基础 CRT（不暴击）
            return RandomFloatProvider != null ? RandomFloatProvider(0f, 1f) : 0.5f;
        }

        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }
}
