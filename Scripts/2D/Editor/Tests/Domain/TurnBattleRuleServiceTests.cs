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

    [TestFixture]
    public class TurnBattleRuleServiceTests
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
            Element? element = null, bool scaleByInt = false, int buffTurns = 0)
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
                BuffDurationTurns = buffTurns,
            };
        }

        private static TurnBattleState MakeState(params TurnBattleUnit[] units)
        {
            TurnBattleState state = new TurnBattleState();
            state.Units.AddRange(units);
            return state;
        }

        // ---- 行动顺序 ----

        [Test]
        public void TurnOrder_SpdDescending_AllyBreaksTie()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, spd: 5f),
                MakeUnit(2, TurnBattleFaction.Enemy, spd: 5f),
                MakeUnit(3, TurnBattleFaction.Enemy, spd: 9f),
                MakeUnit(4, TurnBattleFaction.Ally, spd: 1f));

            List<TurnBattleUnit> order = TurnBattleRuleService.DetermineTurnOrder(state);

            Assert.AreEqual(new long[] { 3, 1, 2, 4 }, order.Select(u => u.UnitId).ToArray());
        }

        [Test]
        public void TurnOrder_DownUnitsExcluded()
        {
            TurnBattleUnit down = MakeUnit(2, TurnBattleFaction.Enemy, spd: 99f);
            down.Hp = 0f;
            TurnBattleState state = MakeState(down, MakeUnit(1, TurnBattleFaction.Ally, spd: 1f));

            List<TurnBattleUnit> order = TurnBattleRuleService.DetermineTurnOrder(state);

            Assert.AreEqual(new long[] { 1 }, order.Select(u => u.UnitId).ToArray());
        }

        // ---- 命中 / 暴击 ----

        [Test]
        public void HitRate_FollowsHitDiffAndClamps()
        {
            TurnBattleUnit attacker = MakeUnit(1, TurnBattleFaction.Ally, hit: 0.6f);
            TurnBattleUnit target = MakeUnit(2, TurnBattleFaction.Enemy, hit: 0.5f);
            Assert.AreEqual(0.95f, TurnBattleRuleService.GetHitRate(attacker, target), 1e-4f);

            Assert.AreEqual(0.85f, TurnBattleRuleService.GetHitRate(target, attacker), 1e-4f);

            TurnBattleUnit huge = MakeUnit(3, TurnBattleFaction.Ally, hit: 100f);
            Assert.AreEqual(1.0f, TurnBattleRuleService.GetHitRate(huge, target), 1e-4f);
            Assert.AreEqual(0.5f, TurnBattleRuleService.GetHitRate(target, huge), 1e-4f);
        }

        [Test]
        public void RollHit_BoundaryRolls()
        {
            // 双方同 HIT → 命中率 0.90
            TurnBattleUnit attacker = MakeUnit(1, TurnBattleFaction.Ally, hit: 0.5f);
            TurnBattleUnit target = MakeUnit(2, TurnBattleFaction.Enemy, hit: 0.5f);

            UseSequence(0.899f);
            Assert.IsTrue(TurnBattleRuleService.RollHit(attacker, target));

            UseSequence(0.90f);
            Assert.IsFalse(TurnBattleRuleService.RollHit(attacker, target));
        }

        [Test]
        public void RollCritical_UsesCrtRatio()
        {
            TurnBattleUnit attacker = MakeUnit(1, TurnBattleFaction.Ally, crt: 0.05f);

            UseSequence(0.049f);
            Assert.IsTrue(TurnBattleRuleService.RollCritical(attacker));

            UseSequence(0.051f);
            Assert.IsFalse(TurnBattleRuleService.RollCritical(attacker));
        }

        // ---- 伤害 / 治疗 / 冷却 ----

        [Test]
        public void Damage_BaseFormula_NoModifier()
        {
            TurnBattleUnit attacker = MakeUnit(1, TurnBattleFaction.Ally, atn: 10f);
            TurnBattleUnit target = MakeUnit(2, TurnBattleFaction.Enemy, def: 0f);

            float damage = TurnBattleRuleService.CalculateDamage(attacker, target, false, 1f, false, 1f);

            Assert.AreEqual(10f, damage, 1e-4f);
        }

        [Test]
        public void Damage_LevelCriticalElementBuff_AllStack()
        {
            TurnBattleUnit attacker = MakeUnit(1, TurnBattleFaction.Ally, atn: 10f, crt: 1f);
            attacker.Level = 3;
            attacker.AttackBuffMultiplier = 1.5f;
            TurnBattleUnit target = MakeUnit(2, TurnBattleFaction.Enemy, def: 0f);

            // 10 × 1.0 × (1+0.1×2) × 1.5(buff) = 18 → 暴击 ×1.5 = 27 → 克制 ×1.3 = 35.1
            float damage = TurnBattleRuleService.CalculateDamage(attacker, target, false, 1f, true, 1.3f);

            Assert.AreEqual(35.1f, damage, 1e-3f);
        }

        [Test]
        public void Damage_DefenseFloor()
        {
            TurnBattleUnit attacker = MakeUnit(1, TurnBattleFaction.Ally, atn: 10f);
            TurnBattleUnit target = MakeUnit(2, TurnBattleFaction.Enemy, def: 100f);

            float damage = TurnBattleRuleService.CalculateDamage(attacker, target, false, 1f, false, 1f);

            Assert.AreEqual(0.1f, damage, 1e-4f);
        }

        [Test]
        public void Damage_ScaleByInt_UsesIntStat()
        {
            TurnBattleUnit attacker = MakeUnit(1, TurnBattleFaction.Ally, atn: 10f, intStat: 5f);
            TurnBattleUnit target = MakeUnit(2, TurnBattleFaction.Enemy, def: 0f);

            Assert.AreEqual(5f, TurnBattleRuleService.CalculateDamage(attacker, target, true, 1f, false, 1f), 1e-4f);
        }

        [Test]
        public void Heal_ScalesByLevel()
        {
            TurnBattleUnit actor = MakeUnit(1, TurnBattleFaction.Ally);
            actor.Level = 3;

            Assert.AreEqual(30f, TurnBattleRuleService.CalculateHeal(MakeUnit(1, TurnBattleFaction.Ally), 30f), 1e-4f);
            Assert.AreEqual(36f, TurnBattleRuleService.CalculateHeal(actor, 30f), 1e-4f);
        }

        [Test]
        public void CooldownMapping_SecondsToTurns()
        {
            Assert.AreEqual(0, TurnBattleRuleService.ConvertSecondsToTurns(0f));
            Assert.AreEqual(0, TurnBattleRuleService.ConvertSecondsToTurns(3f));
            Assert.AreEqual(1, TurnBattleRuleService.ConvertSecondsToTurns(4f));
            Assert.AreEqual(2, TurnBattleRuleService.ConvertSecondsToTurns(5f));
            Assert.AreEqual(2, TurnBattleRuleService.ConvertSecondsToTurns(6f));
            Assert.AreEqual(3, TurnBattleRuleService.ConvertSecondsToTurns(8f));
            Assert.AreEqual(3, TurnBattleRuleService.ConvertSecondsToTurns(9f));
        }

        // ---- 逃跑 ----

        [Test]
        public void FleeRate_ComponentsAndClamp()
        {
            // 基线：我方 1 人 SPD1、敌方 1 人 SPD1、0 次失败 → 0.50 - 0.05 = 0.45
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, spd: 1f, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy, spd: 1f));
            Assert.AreEqual(0.45f, TurnBattleRuleService.GetFleeSuccessRate(state), 1e-4f);

            // 我方均 SPD 高敌均 10 → +0.30
            TurnBattleState fast = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, spd: 11f, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy, spd: 1f));
            Assert.AreEqual(0.75f, TurnBattleRuleService.GetFleeSuccessRate(fast), 1e-4f);

            // 敌 3 个 → 再 -0.10
            TurnBattleState crowded = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, spd: 1f, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy, spd: 1f),
                MakeUnit(3, TurnBattleFaction.Enemy, spd: 1f),
                MakeUnit(4, TurnBattleFaction.Enemy, spd: 1f));
            Assert.AreEqual(0.35f, TurnBattleRuleService.GetFleeSuccessRate(crowded), 1e-4f);

            // 失败 2 次 → +0.30
            crowded.FleeAttemptCount = 2;
            Assert.AreEqual(0.65f, TurnBattleRuleService.GetFleeSuccessRate(crowded), 1e-4f);

            // clamp 两端
            TurnBattleState godly = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, spd: 1000f, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy, spd: 1f));
            Assert.AreEqual(0.95f, TurnBattleRuleService.GetFleeSuccessRate(godly), 1e-4f);
        }

        // ---- 普攻结算 ----

        [Test]
        public void ResolveAttack_HitDealsDamage()
        {
            // 未注入随机 → 0.5 < 0.90 必中、不暴击
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, atn: 10f, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy, def: 0f));
            TurnBattleUnit enemy = state.FindUnit(2);

            TurnBattleActionResolution r = TurnBattleRuleService.ResolveAction(
                state, state.FindUnit(1), new TurnBattleAction { Kind = TurnBattleActionKind.Attack, TargetUnitId = 2 });

            Assert.AreEqual(90f, enemy.Hp, 1e-3f);
            Assert.AreEqual(1, r.Effects.Count);
            Assert.AreEqual(TurnBattleEffectKind.Damage, r.Effects[0].Kind);
            Assert.AreEqual(10f, r.Effects[0].Value, 1e-3f);
            Assert.AreEqual(90f, r.Effects[0].RemainingHp, 1e-3f);
        }

        [Test]
        public void ResolveAttack_MissKeepsHp()
        {
            UseSequence(0.99f); // > 0.90 → miss
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));
            TurnBattleUnit enemy = state.FindUnit(2);

            TurnBattleActionResolution r = TurnBattleRuleService.ResolveAction(
                state, state.FindUnit(1), new TurnBattleAction { Kind = TurnBattleActionKind.Attack, TargetUnitId = 2 });

            Assert.AreEqual(100f, enemy.Hp, 1e-3f);
            Assert.AreEqual(TurnBattleEffectKind.Miss, r.Effects[0].Kind);
        }

        [Test]
        public void ResolveAttack_KillAddsDownEntry()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, atn: 50f, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));
            state.FindUnit(2).Hp = 30f;

            TurnBattleActionResolution r = TurnBattleRuleService.ResolveAction(
                state, state.FindUnit(1), new TurnBattleAction { Kind = TurnBattleActionKind.Attack, TargetUnitId = 2 });

            Assert.IsTrue(state.FindUnit(2).IsDown);
            Assert.AreEqual(2, r.Effects.Count);
            Assert.AreEqual(TurnBattleEffectKind.Damage, r.Effects[0].Kind);
            Assert.AreEqual(TurnBattleEffectKind.Down, r.Effects[1].Kind);
        }

        [Test]
        public void ResolveAttack_InvalidTarget_FallsBackToAliveOpponent()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, atn: 10f, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy),
                MakeUnit(3, TurnBattleFaction.Enemy));
            state.FindUnit(2).Hp = 0f; // 指定目标已倒下

            TurnBattleActionResolution r = TurnBattleRuleService.ResolveAction(
                state, state.FindUnit(1), new TurnBattleAction { Kind = TurnBattleActionKind.Attack, TargetUnitId = 2 });

            Assert.AreEqual(90f, state.FindUnit(3).Hp, 1e-3f);
            Assert.AreEqual(3, r.Effects[0].TargetId);
        }

        // ---- 技能结算 ----

        [Test]
        public void ResolveSkill_ConsumesManaAndSetsCooldown()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, atn: 10f, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy, def: 0f));
            TurnBattleUnit player = state.FindUnit(1);
            player.Skills.Add(MakeSkill(SkillType.SingleTarget, mult: 2f, mana: 20, cdTurns: 2));

            TurnBattleActionResolution r = TurnBattleRuleService.ResolveAction(
                state, player, new TurnBattleAction { Kind = TurnBattleActionKind.UseSkill, SkillIndex = 0, TargetUnitId = 2 });

            Assert.AreEqual(80, player.Mp);
            Assert.AreEqual(2, player.Skills[0].RemainingCooldown);
            // 10 ATN × 2.0 = 20 伤害
            Assert.AreEqual(80f, state.FindUnit(2).Hp, 1e-3f);
            Assert.AreEqual(TurnBattleEffectKind.Damage, r.Effects[0].Kind);
        }

        [Test]
        public void ResolveSkill_ManaShort_FallsBackToAttack()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, atn: 10f, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));
            TurnBattleUnit player = state.FindUnit(1);
            player.Mp = 10;
            player.Skills.Add(MakeSkill(SkillType.SingleTarget, mult: 2f, mana: 20));

            TurnBattleActionResolution r = TurnBattleRuleService.ResolveAction(
                state, player, new TurnBattleAction { Kind = TurnBattleActionKind.UseSkill, SkillIndex = 0, TargetUnitId = 2 });

            Assert.AreEqual(TurnBattleActionKind.Attack, r.ActionKind);
            Assert.AreEqual(10, player.Mp); // 不扣蓝
            Assert.AreEqual(90f, state.FindUnit(2).Hp, 1e-3f); // 普攻 10
        }

        [Test]
        public void ResolveSkill_OnCooldown_FallsBackToAttack()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, atn: 10f, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));
            TurnBattleUnit player = state.FindUnit(1);
            TurnBattleSkillState skill = MakeSkill(SkillType.SingleTarget, mult: 2f, mana: 0, cdTurns: 2);
            skill.RemainingCooldown = 1;
            player.Skills.Add(skill);

            TurnBattleActionResolution r = TurnBattleRuleService.ResolveAction(
                state, player, new TurnBattleAction { Kind = TurnBattleActionKind.UseSkill, SkillIndex = 0, TargetUnitId = 2 });

            Assert.AreEqual(TurnBattleActionKind.Attack, r.ActionKind);
            Assert.AreEqual(90f, state.FindUnit(2).Hp, 1e-3f);
        }

        [Test]
        public void ResolveSkill_SelfAoe_HitsAllWithPenalty()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, atn: 10f, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy),
                MakeUnit(3, TurnBattleFaction.Enemy));
            TurnBattleUnit player = state.FindUnit(1);
            player.Skills.Add(MakeSkill(SkillType.SelfAOE, mult: 1f));

            TurnBattleActionResolution r = TurnBattleRuleService.ResolveAction(
                state, player, new TurnBattleAction { Kind = TurnBattleActionKind.UseSkill, SkillIndex = 0 });

            // 单体 10 × 0.6 = 6，两个敌各中一发
            Assert.AreEqual(94f, state.FindUnit(2).Hp, 1e-3f);
            Assert.AreEqual(94f, state.FindUnit(3).Hp, 1e-3f);
            Assert.AreEqual(2, r.Effects.Count(e => e.Kind == TurnBattleEffectKind.Damage));
        }

        [Test]
        public void ResolveSkill_SelfHeal_ClampsToMaxHp()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));
            TurnBattleUnit player = state.FindUnit(1);
            player.Hp = 95f;
            player.Skills.Add(MakeSkill(SkillType.SelfHeal, mult: 30f));

            TurnBattleActionResolution r = TurnBattleRuleService.ResolveAction(
                state, player, new TurnBattleAction { Kind = TurnBattleActionKind.UseSkill, SkillIndex = 0 });

            Assert.AreEqual(100f, player.Hp, 1e-3f);
            Assert.AreEqual(5f, r.Effects[0].Value, 1e-3f); // 实际恢复 5
            Assert.AreEqual(TurnBattleEffectKind.Heal, r.Effects[0].Kind);
        }

        [Test]
        public void ResolveSkill_SelfBuff_SetsBuffTurns()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));
            TurnBattleUnit player = state.FindUnit(1);
            player.Skills.Add(MakeSkill(SkillType.SelfBuff, mult: 1.5f, buffTurns: 3));

            TurnBattleActionResolution r = TurnBattleRuleService.ResolveAction(
                state, player, new TurnBattleAction { Kind = TurnBattleActionKind.UseSkill, SkillIndex = 0 });

            Assert.AreEqual(3, player.AttackBuffTurns);
            Assert.AreEqual(1.5f, player.AttackBuffMultiplier, 1e-3f);
            Assert.AreEqual(TurnBattleEffectKind.Buff, r.Effects[0].Kind);
        }

        [Test]
        public void ResolveItem_HealsAndClamps()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));
            TurnBattleUnit player = state.FindUnit(1);
            player.Hp = 95f;

            TurnBattleActionResolution r = TurnBattleRuleService.ResolveAction(
                state, player, new TurnBattleAction { Kind = TurnBattleActionKind.UseItem, ItemHealAmount = 10f, ItemDisplayName = "血瓶" });

            Assert.AreEqual(100f, player.Hp, 1e-3f);
            Assert.AreEqual(5f, r.Effects[0].Value, 1e-3f);
            Assert.AreEqual("血瓶", r.Effects[0].SourceName);
        }

        // ---- 逃跑结算 ----

        [Test]
        public void ResolveFlee_SuccessEndsBattleAsEscaped()
        {
            UseSequence(0.1f); // < 0.45 → 成功
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));

            TurnBattleActionResolution r = TurnBattleRuleService.ResolveAction(
                state, state.FindUnit(1), new TurnBattleAction { Kind = TurnBattleActionKind.Flee });

            Assert.AreEqual(TurnBattlePhase.BattleOver, state.Phase);
            Assert.AreEqual(TurnBattleResult.Escaped, state.Result);
            Assert.AreEqual(TurnBattleEffectKind.FleeSuccess, r.Effects[0].Kind);
        }

        [Test]
        public void ResolveFlee_FailIncrementsAttemptCount()
        {
            UseSequence(0.9f); // ≥ 0.45 → 失败
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));

            TurnBattleActionResolution r = TurnBattleRuleService.ResolveAction(
                state, state.FindUnit(1), new TurnBattleAction { Kind = TurnBattleActionKind.Flee });

            Assert.AreEqual(1, state.FleeAttemptCount);
            Assert.AreEqual(TurnBattleEffectKind.FleeFail, r.Effects[0].Kind);
            Assert.AreNotEqual(TurnBattlePhase.BattleOver, state.Phase);
        }

        // ---- 结果判定 / 回合推进 ----

        [Test]
        public void EvaluateResult_EnemyWiped_Victory()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));
            state.FindUnit(2).Hp = 0f;

            Assert.AreEqual(TurnBattleResult.Victory, TurnBattleRuleService.EvaluateBattleResult(state));
        }

        [Test]
        public void EvaluateResult_PlayerDown_Defeat()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Ally), // Worker 存活也不救 — 玩家倒下立即判负
                MakeUnit(3, TurnBattleFaction.Enemy));
            state.FindUnit(1).Hp = 0f;

            Assert.AreEqual(TurnBattleResult.Defeat, TurnBattleRuleService.EvaluateBattleResult(state));
        }

        [Test]
        public void EvaluateResult_WorkerDownOnly_Continues()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Ally),
                MakeUnit(3, TurnBattleFaction.Enemy));
            state.FindUnit(2).Hp = 0f; // Worker 倒下

            Assert.IsNull(TurnBattleRuleService.EvaluateBattleResult(state));
        }

        [Test]
        public void AdvanceRound_DecrementsCooldownAndExpiresBuff()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));
            TurnBattleUnit player = state.FindUnit(1);
            player.Skills.Add(MakeSkill(SkillType.SingleTarget, cdTurns: 2));
            player.Skills[0].RemainingCooldown = 2;
            player.AttackBuffTurns = 1;
            player.AttackBuffMultiplier = 1.5f;

            TurnBattleRuleService.AdvanceRound(state);

            Assert.AreEqual(2, state.Round);
            Assert.AreEqual(1, player.Skills[0].RemainingCooldown);
            Assert.AreEqual(0, player.AttackBuffTurns);
            Assert.AreEqual(1f, player.AttackBuffMultiplier, 1e-3f);
        }

        // ---- 敌方 AI ----

        [Test]
        public void EnemyAi_NoSkills_PicksAttack()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));

            TurnBattleAction action = TurnBattleRuleService.SelectEnemyAction(state, state.FindUnit(2));

            Assert.AreEqual(TurnBattleActionKind.Attack, action.Kind);
            Assert.AreEqual(1, action.TargetUnitId);
        }

        [Test]
        public void EnemyAi_ManaShort_FallsBackToAttack()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));
            TurnBattleUnit enemy = state.FindUnit(2);
            enemy.Mp = 5;
            enemy.Skills.Add(MakeSkill(SkillType.SingleTarget, mult: 3f, mana: 20));

            TurnBattleAction action = TurnBattleRuleService.SelectEnemyAction(state, enemy);

            Assert.AreEqual(TurnBattleActionKind.Attack, action.Kind);
        }

        [Test]
        public void EnemyAi_PicksHighestExpectedDamage()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy, atn: 10f));
            TurnBattleUnit enemy = state.FindUnit(2);
            enemy.Skills.Add(MakeSkill(SkillType.SingleTarget, mult: 1f, mana: 0)); // 期望 10
            enemy.Skills.Add(MakeSkill(SkillType.SingleTarget, mult: 2f, mana: 0)); // 期望 20 ← 选这个

            TurnBattleAction action = TurnBattleRuleService.SelectEnemyAction(state, enemy);

            Assert.AreEqual(TurnBattleActionKind.UseSkill, action.Kind);
            Assert.AreEqual(1, action.SkillIndex);
        }

        [Test]
        public void EnemyAi_BiasedToLowestHpTarget()
        {
            UseSequence(0.3f); // < 0.4 → 偏向最低血
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(5, TurnBattleFaction.Ally));
            state.FindUnit(5).Hp = 20f;
            TurnBattleUnit enemy = MakeUnit(2, TurnBattleFaction.Enemy);
            state.Units.Add(enemy);

            TurnBattleAction action = TurnBattleRuleService.SelectEnemyAction(state, enemy);

            Assert.AreEqual(5, action.TargetUnitId);
        }

        // ---- 防御 ----

        [Test]
        public void ResolveAction_DownActor_ReturnsEmpty()
        {
            TurnBattleState state = MakeState(
                MakeUnit(1, TurnBattleFaction.Ally, isPlayer: true),
                MakeUnit(2, TurnBattleFaction.Enemy));
            state.FindUnit(1).Hp = 0f;

            TurnBattleActionResolution r = TurnBattleRuleService.ResolveAction(
                state, state.FindUnit(1), new TurnBattleAction { Kind = TurnBattleActionKind.Attack, TargetUnitId = 2 });

            Assert.AreEqual(0, r.Effects.Count);
            Assert.AreEqual(100f, state.FindUnit(2).Hp, 1e-3f);
        }
    }
}
