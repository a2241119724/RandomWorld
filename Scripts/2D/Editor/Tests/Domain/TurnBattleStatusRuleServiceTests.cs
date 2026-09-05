namespace LAB2D.Editor.Tests.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LAB2D.Domain.Character;
    using LAB2D.Domain.Gameplay.Cultivation;
    using LAB2D.Domain.TurnBattle;
    using NUnit.Framework;
    using SkillType = LAB2D.Enum.SkillType;

    /// <summary>状态效果 / 防御行动 / 暴击追击 / 顺序预测 规则测试。</summary>
    [TestFixture]
    public class TurnBattleStatusRuleServiceTests
    {
        [TearDown]
        public void TearDown()
        {
            TurnBattleRuleService.RandomFloatProvider = null;
        }

        /// <summary>依序取自固定序列的随机桩（耗尽后重复最后一个值）。</summary>
        private static void UseSequence(params float[] values)
        {
            int index = 0;
            TurnBattleRuleService.RandomFloatProvider = (min, max) =>
            {
                float value = values[Math.Min(index, values.Length - 1)];
                index++;
                return value;
            };
        }

        private static TurnBattleUnit MakeUnit(
            long id, TurnBattleFaction faction,
            float spd = 1f, float atn = 10f, float def = 0f, float crt = 0f,
            float hit = 0.5f, float intStat = 5f, bool isPlayer = false)
        {
            return new TurnBattleUnit
            {
                UnitId = id,
                DisplayName = "Unit" + id,
                Faction = faction,
                Hp = 100f,
                MaxHp = 100f,
                Mp = 100,
                MaxMp = 100,
                Level = 1,
                Stats = new BattleStats(atn, intStat, def, 0f, crt, 1.5f, spd, hit),
                IsPlayerUnit = isPlayer,
            };
        }

        private static TurnBattleSkillState MakeSkill(
            SkillType type, float mult = 1f, int mana = 0, int cdTurns = 0,
            Element? element = null, bool scaleByInt = false,
            TurnBattleStatusKind? statusKind = null, float statusChance = 1f,
            int statusTurns = 0, float statusRatio = 0f)
        {
            return new TurnBattleSkillState
            {
                SkillId = "skill_test",
                SkillName = "测试技",
                Type = type,
                EffectMultiplier = mult,
                ManaCost = mana,
                CooldownTurns = cdTurns,
                Element = element,
                ScaleByInt = scaleByInt,
                StatusKind = statusKind,
                StatusChance = statusChance,
                StatusDurationTurns = statusTurns,
                StatusValueRatio = statusRatio,
            };
        }

        private static TurnBattleState MakeState(params TurnBattleUnit[] units)
        {
            TurnBattleState state = new TurnBattleState();
            state.Units.AddRange(units);
            return state;
        }

        // ---- 施加 / 叠加规则 ----

        [Test]
        public void Apply_SameKindRefreshesDurationAndValue()
        {
            TurnBattleUnit unit = MakeUnit(1, TurnBattleFaction.Ally);
            TurnBattleStatusRuleService.Apply(unit, TurnBattleStatusKind.Poison, 3, 5f, 2, "毒牙");
            TurnBattleStatusRuleService.Apply(unit, TurnBattleStatusKind.Poison, 2, 8f, 2, "毒牙");

            Assert.AreEqual(1, unit.StatusEffects.Count);
            Assert.AreEqual(2, unit.StatusEffects[0].RemainingTurns);
            Assert.AreEqual(8f, unit.StatusEffects[0].Value, 1e-3f);

            // 异类并存
            TurnBattleStatusRuleService.Apply(unit, TurnBattleStatusKind.Stun, 1, 0f, 2, "撞击");
            Assert.AreEqual(2, unit.StatusEffects.Count);
        }

        // ---- DOT 跳动 / 到期 ----

        [Test]
        public void AdvanceRound_PoisonTicksDamageAndExpires()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));
            TurnBattleUnit player = state.FindUnit(1);
            TurnBattleStatusRuleService.Apply(player, TurnBattleStatusKind.Poison, 3, 6f, 2, "毒牙");

            List<TurnBattleEffectEntry> first = TurnBattleRuleService.AdvanceRound(state);

            Assert.AreEqual(1, first.Count);
            Assert.AreEqual(TurnBattleEffectKind.StatusTick, first[0].Kind);
            Assert.AreEqual(6f, first[0].Value, 1e-3f);
            Assert.AreEqual(94f, player.Hp, 1e-3f);
            Assert.AreEqual(1, first[0].TargetId); // 受害者=玩家
            Assert.AreEqual(-1, first[0].ActorId); // 无行动者（不触发前冲）
            Assert.AreEqual(94f, first[0].RemainingHp, 1e-3f);

            TurnBattleRuleService.AdvanceRound(state);
            List<TurnBattleEffectEntry> third = TurnBattleRuleService.AdvanceRound(state);

            Assert.AreEqual(82f, player.Hp, 1e-3f); // 3 跳 ×6
            Assert.AreEqual(0, player.StatusEffects.Count);
            Assert.AreEqual(0, TurnBattleRuleService.AdvanceRound(state).Count); // 已到期不再跳
        }

        [Test]
        public void AdvanceRound_PoisonCanKill_AddsDownEntry()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));
            TurnBattleUnit player = state.FindUnit(1);
            player.Hp = 5f;
            TurnBattleStatusRuleService.Apply(player, TurnBattleStatusKind.Poison, 2, 6f, 2, "毒牙");

            List<TurnBattleEffectEntry> entries = TurnBattleRuleService.AdvanceRound(state);

            Assert.AreEqual(2, entries.Count);
            Assert.AreEqual(TurnBattleEffectKind.StatusTick, entries[0].Kind);
            Assert.AreEqual(TurnBattleEffectKind.Down, entries[1].Kind);
            Assert.IsTrue(player.IsDown);
            Assert.AreEqual(TurnBattleResult.Defeat, TurnBattleRuleService.EvaluateBattleResult(state));
        }

        // ---- 眩晕 ----

        [Test]
        public void ResolveAction_StunnedSkipsAction_EmptyResolution()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));
            TurnBattleUnit player = state.FindUnit(1);
            player.Skills.Add(MakeSkill(SkillType.SingleTarget, mult: 2f, mana: 20));
            TurnBattleStatusRuleService.Apply(player, TurnBattleStatusKind.Stun, 1, 0f, 2, "撞击");

            TurnBattleActionResolution r = TurnBattleRuleService.ResolveAction(
                state, player, new TurnBattleAction { Kind = TurnBattleActionKind.UseSkill, SkillIndex = 0, TargetUnitId = 2 });

            Assert.AreEqual(1, r.Effects.Count);
            Assert.AreEqual(TurnBattleEffectKind.Stunned, r.Effects[0].Kind);
            Assert.AreEqual(100, player.Mp); // 不耗蓝
            Assert.AreEqual(100f, state.FindUnit(2).Hp, 1e-3f); // 不出伤
        }

        [Test]
        public void StunExpiresAfterTurns()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, atn: 10f, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));
            TurnBattleUnit player = state.FindUnit(1);
            TurnBattleStatusRuleService.Apply(player, TurnBattleStatusKind.Stun, 1, 0f, 2, "撞击");

            TurnBattleRuleService.AdvanceRound(state);

            Assert.IsFalse(TurnBattleStatusRuleService.IsStunned(player));

            TurnBattleActionResolution r = TurnBattleRuleService.ResolveAction(
                state, player, new TurnBattleAction { Kind = TurnBattleActionKind.Attack, TargetUnitId = 2 });
            Assert.AreEqual(TurnBattleEffectKind.Damage, r.Effects[0].Kind);
        }

        // ---- 增减益 / 防御减伤 ----

        [Test]
        public void CalculateDamage_ScalesWithAttackStatuses()
        {
            TurnBattleUnit attacker = MakeUnit(1, TurnBattleFaction.Ally, atn: 10f);
            attacker.AttackBuffMultiplier = 1.5f;
            TurnBattleStatusRuleService.Apply(attacker, TurnBattleStatusKind.AttackUp, 2, TurnBattleStatusRuleService.AttackUpMultiplier, 1, "测试");
            TurnBattleStatusRuleService.Apply(attacker, TurnBattleStatusKind.AttackDown, 2, TurnBattleStatusRuleService.AttackDownMultiplier, 1, "测试");
            TurnBattleUnit target = MakeUnit(2, TurnBattleFaction.Enemy, def: 0f);

            // 10 × 1.5 × 1.3 × 0.75 = 14.625
            Assert.AreEqual(14.625f, TurnBattleRuleService.CalculateDamage(attacker, target, false, 1f, false, 1f), 1e-3f);
        }

        [Test]
        public void CalculateDamage_DefenseStatusesAndDefendAction()
        {
            TurnBattleUnit attacker = MakeUnit(1, TurnBattleFaction.Ally, atn: 10f);

            TurnBattleUnit guardUp = MakeUnit(2, TurnBattleFaction.Enemy, def: 0f);
            TurnBattleStatusRuleService.Apply(guardUp, TurnBattleStatusKind.DefenseUp, 2, TurnBattleStatusRuleService.DefenseUpIncomingMultiplier, 1, "测试");
            Assert.AreEqual(7f, TurnBattleRuleService.CalculateDamage(attacker, guardUp, false, 1f, false, 1f), 1e-3f);

            guardUp.IsDefending = true; // 防增减益 × 防御姿态叠乘
            Assert.AreEqual(3.5f, TurnBattleRuleService.CalculateDamage(attacker, guardUp, false, 1f, false, 1f), 1e-3f);

            TurnBattleUnit guardDown = MakeUnit(3, TurnBattleFaction.Enemy, def: 0f);
            TurnBattleStatusRuleService.Apply(guardDown, TurnBattleStatusKind.DefenseDown, 2, TurnBattleStatusRuleService.DefenseDownIncomingMultiplier, 1, "测试");
            Assert.AreEqual(13f, TurnBattleRuleService.CalculateDamage(attacker, guardDown, false, 1f, false, 1f), 1e-3f);

            TurnBattleUnit plain = MakeUnit(4, TurnBattleFaction.Enemy, def: 0f);
            plain.IsDefending = true;
            Assert.AreEqual(5f, TurnBattleRuleService.CalculateDamage(attacker, plain, false, 1f, false, 1f), 1e-3f);
        }

        [Test]
        public void Defend_ClearsAtRoundEnd()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));

            TurnBattleActionResolution r = TurnBattleRuleService.ResolveAction(
                state, state.FindUnit(1), new TurnBattleAction { Kind = TurnBattleActionKind.Defend });
            Assert.IsTrue(state.FindUnit(1).IsDefending);
            Assert.AreEqual(TurnBattleEffectKind.Defend, r.Effects[0].Kind);

            TurnBattleRuleService.AdvanceRound(state);
            Assert.IsFalse(state.FindUnit(1).IsDefending);
        }

        [Test]
        public void AdvanceRound_DotRespectsDefend()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));
            TurnBattleUnit player = state.FindUnit(1);
            player.IsDefending = true;
            TurnBattleStatusRuleService.Apply(player, TurnBattleStatusKind.Poison, 1, 6f, 2, "毒牙");

            List<TurnBattleEffectEntry> entries = TurnBattleRuleService.AdvanceRound(state);

            Assert.AreEqual(3f, entries[0].Value, 1e-3f); // 6 × 0.5
            Assert.AreEqual(97f, player.Hp, 1e-3f);
        }

        // ---- 敌方 AI 防御 ----

        [Test]
        public void EnemyAi_DefendsAtLowHp()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));
            TurnBattleUnit enemy = state.FindUnit(2);
            enemy.Hp = 10f; // 10% < 25% 门槛

            UseSequence(0.2f); // < 0.30 → 防御
            Assert.AreEqual(TurnBattleActionKind.Defend, TurnBattleRuleService.SelectEnemyAction(state, enemy).Kind);

            UseSequence(0.9f); // ≥ 0.30 → 不防御，走攻击
            Assert.AreEqual(TurnBattleActionKind.Attack, TurnBattleRuleService.SelectEnemyAction(state, enemy).Kind);
        }

        // ---- 暴击追击 ----

        [Test]
        public void ResolveStrike_CriticalCombo_HalfDamage()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, atn: 10f, crt: 1f, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy, def: 0f));
            TurnBattleUnit enemy = state.FindUnit(2);

            // 命中 0.5 → 暴击 0.3 → 追击 roll 0.4 < 0.5 触发
            UseSequence(0.5f, 0.3f, 0.4f);
            TurnBattleActionResolution r = TurnBattleRuleService.ResolveAction(
                state, state.FindUnit(1), new TurnBattleAction { Kind = TurnBattleActionKind.Attack, TargetUnitId = 2 });

            Assert.AreEqual(77.5f, enemy.Hp, 1e-3f); // 15 主击(ATN10×CSD1.5) + 7.5 追击
            List<TurnBattleEffectEntry> damages = r.Effects.FindAll(e => e.Kind == TurnBattleEffectKind.Damage);
            Assert.AreEqual(2, damages.Count);
            Assert.IsTrue(damages[0].IsCritical);
            Assert.IsFalse(damages[0].IsCombo);
            Assert.IsTrue(damages[1].IsCombo);
            Assert.AreEqual(7.5f, damages[1].Value, 1e-3f);
            Assert.IsFalse(damages[1].IsCritical);

            // 追击 roll 0.9 ≥ 0.5 不触发
            state.FindUnit(2).Hp = 100f;
            UseSequence(0.5f, 0.3f, 0.9f);
            TurnBattleRuleService.ResolveAction(
                state, state.FindUnit(1), new TurnBattleAction { Kind = TurnBattleActionKind.Attack, TargetUnitId = 2 });
            Assert.AreEqual(85f, enemy.Hp, 1e-3f); // 主击 15（无追击）

            // 目标被主击打死 → 不追击
            state.FindUnit(2).Hp = 8f;
            UseSequence(0.5f, 0.3f);
            TurnBattleActionResolution kill = TurnBattleRuleService.ResolveAction(
                state, state.FindUnit(1), new TurnBattleAction { Kind = TurnBattleActionKind.Attack, TargetUnitId = 2 });
            Assert.IsTrue(enemy.IsDown);
            Assert.AreEqual(1, kill.Effects.Count(e => e.Kind == TurnBattleEffectKind.Damage));
            Assert.AreEqual(1, kill.Effects.Count(e => e.Kind == TurnBattleEffectKind.Down));
        }

        // ---- 顺序预测 ----

        [Test]
        public void PredictTurnOrder_CyclesAndExcludesDead()
        {
            TurnBattleUnit dead = MakeUnit(4, TurnBattleFaction.Ally, spd: 99f);
            dead.Hp = 0f;
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, spd: 5f, isPlayer: true),
                MakeUnit(3, TurnBattleFaction.Enemy, spd: 9f),
                MakeUnit(5, TurnBattleFaction.Enemy, spd: 7f),
                dead);
            TurnBattleStatusRuleService.Apply(state.FindUnit(5), TurnBattleStatusKind.Stun, 1, 0f, 3, "撞击");

            List<TurnBattleUnit> order = TurnBattleRuleService.PredictTurnOrder(state, 5);

            // 眩晕单位保留（UI 标记），死者排除，SPD 循环展开
            Assert.AreEqual(new long[] { 3, 5, 1, 3, 5 }, order.Select(u => u.UnitId).ToArray());
        }

        // ---- 技能附带状态 ----

        [Test]
        public void ResolveStrike_SkillAppliesStatusByChance()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, atn: 10f, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy, def: 0f));
            TurnBattleUnit player = state.FindUnit(1);
            TurnBattleUnit enemy = state.FindUnit(2);
            player.Skills.Add(MakeSkill(
                SkillType.SingleTarget, mult: 1f,
                statusKind: TurnBattleStatusKind.Poison, statusChance: 0.5f, statusTurns: 2, statusRatio: 0.05f));

            // 命中 0.5 → 不暴击 0.2 → 状态 roll 0.1 < 0.5 触发
            UseSequence(0.5f, 0.2f, 0.1f);
            TurnBattleActionResolution r = TurnBattleRuleService.ResolveAction(
                state, player, new TurnBattleAction { Kind = TurnBattleActionKind.UseSkill, SkillIndex = 0, TargetUnitId = 2 });

            TurnBattleEffectEntry apply = r.Effects.Find(e => e.Kind == TurnBattleEffectKind.StatusApply);
            Assert.IsNotNull(apply);
            Assert.AreEqual(2, apply.Value, 1e-3f); // 持续回合
            Assert.AreEqual("中毒", apply.SourceName);
            Assert.AreEqual(1, enemy.StatusEffects.Count);
            Assert.AreEqual(5f, enemy.StatusEffects[0].Value, 1e-3f); // 0.05 × MaxHp100
            Assert.AreEqual(2, enemy.StatusEffects[0].RemainingTurns);

            // 状态 roll 0.9 ≥ 0.5 不触发
            enemy.StatusEffects.Clear();
            UseSequence(0.5f, 0.2f, 0.9f);
            TurnBattleActionResolution miss = TurnBattleRuleService.ResolveAction(
                state, player, new TurnBattleAction { Kind = TurnBattleActionKind.UseSkill, SkillIndex = 0, TargetUnitId = 2 });
            Assert.IsNull(miss.Effects.Find(e => e.Kind == TurnBattleEffectKind.StatusApply));
            Assert.AreEqual(0, enemy.StatusEffects.Count);
        }
    }
}
