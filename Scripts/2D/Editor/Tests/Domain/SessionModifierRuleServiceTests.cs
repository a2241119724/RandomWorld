namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Gameplay;
    using NUnit.Framework;
    using System.Collections.Generic;

    [TestFixture]
    public class SessionModifierRuleServiceTests
    {
        [Test]
        public void Roll_SameSeed_SameResult()
        {
            string[] first = SessionModifierRuleService.Roll(12345, 3);
            string[] second = SessionModifierRuleService.Roll(12345, 3);
            Assert.AreEqual(first, second);
        }

        [Test]
        public void Roll_NoDuplicates()
        {
            // 多个 seed 抽满池大小，逐次验证不重复
            for (int seed = 0; seed < 20; seed++)
            {
                string[] picked = SessionModifierRuleService.Roll(seed, 3);
                var seen = new HashSet<string>();
                foreach (string id in picked)
                {
                    Assert.IsTrue(seen.Add(id), $"seed={seed} 出现重复 id: {id}");
                }
            }
        }

        [Test]
        public void Roll_CountClamped()
        {
            Assert.AreEqual(0, SessionModifierRuleService.Roll(1, 0).Length);
            Assert.AreEqual(0, SessionModifierRuleService.Roll(1, -3).Length);
            Assert.AreEqual(
                SessionModifierRuleService.Pool.Length,
                SessionModifierRuleService.Roll(1, SessionModifierRuleService.Pool.Length + 5).Length);
        }

        [Test]
        public void Roll_FullPool_AllKnownIds()
        {
            // 抽满池 = 全集，验证 Roll 只从池内产出
            string[] picked = SessionModifierRuleService.Roll(777, SessionModifierRuleService.Pool.Length);
            Assert.AreEqual(SessionModifierRuleService.Pool.Length, picked.Length);
            foreach (string id in picked)
            {
                Assert.IsNotNull(SessionModifierRuleService.GetById(id));
            }
        }

        [Test]
        public void GetChannelMultiplier_NoActive_Returns1()
        {
            Assert.AreEqual(1f, SessionModifierRuleService.GetChannelMultiplier(null, ModifierChannel.LingQiRecovery), 0.0001f);
            Assert.AreEqual(
                1f,
                SessionModifierRuleService.GetChannelMultiplier(new List<string>(), ModifierChannel.EnemyStrength),
                0.0001f);
        }

        [Test]
        public void GetChannelMultiplier_SingleModifier()
        {
            var ids = new List<string> { "lingqi_surge" };
            Assert.AreEqual(
                1.25f,
                SessionModifierRuleService.GetChannelMultiplier(ids, ModifierChannel.LingQiRecovery),
                0.0001f);
            // 主通道外无效果
            Assert.AreEqual(
                1f,
                SessionModifierRuleService.GetChannelMultiplier(ids, ModifierChannel.EnemyStrength),
                0.0001f);
        }

        [Test]
        public void GetChannelMultiplier_DualChannelModifier()
        {
            // 妖兽凶猛：主通道敌方强度 1.25 + 补偿通道战利品 1.40（配对收益）
            var ids = new List<string> { "enemy_ferocious" };
            Assert.AreEqual(
                1.25f,
                SessionModifierRuleService.GetChannelMultiplier(ids, ModifierChannel.EnemyStrength),
                0.0001f);
            Assert.AreEqual(
                1.40f,
                SessionModifierRuleService.GetChannelMultiplier(ids, ModifierChannel.EnemyLoot),
                0.0001f);
        }

        [Test]
        public void GetChannelMultiplier_Stacking()
        {
            // 妖兽凶猛（补偿 1.40）+ 腰缠万贯（1.40）→ 战利品通道 1.96（同通道累乘）
            var ids = new List<string> { "enemy_ferocious", "loot_rich" };
            Assert.AreEqual(
                1.96f,
                SessionModifierRuleService.GetChannelMultiplier(ids, ModifierChannel.EnemyLoot),
                0.0001f);
        }

        [Test]
        public void GetChannelMultiplier_UnknownIdIgnored()
        {
            var ids = new List<string> { "no_such_modifier", "worker_lazy" };
            Assert.AreEqual(
                0.85f,
                SessionModifierRuleService.GetChannelMultiplier(ids, ModifierChannel.WorkerWorkSpeed),
                0.0001f);
        }

        [Test]
        public void GetById_Unknown_ReturnsNull()
        {
            Assert.IsNull(SessionModifierRuleService.GetById("no_such_modifier"));
            Assert.IsNull(SessionModifierRuleService.GetById(null));
        }

        [Test]
        public void FormatSummary_ListsNames()
        {
            var ids = new List<string> { "worker_diligent", "lingqi_surge" };
            string summary = SessionModifierRuleService.FormatSummary(ids);
            Assert.IsTrue(summary.Contains("工匠勤勉"));
            Assert.IsTrue(summary.Contains("灵气潮汐"));
            Assert.AreEqual("无", SessionModifierRuleService.FormatSummary(new List<string>()));
        }

        [Test]
        public void FormatModifierLine_ContainsEffectAndDescription()
        {
            string line = SessionModifierRuleService.FormatModifierLine(SessionModifierRuleService.GetById("enemy_ferocious"));
            Assert.IsTrue(line.Contains("妖兽凶猛"));
            Assert.IsTrue(line.Contains("敌方×1.25"));
            Assert.IsTrue(line.Contains("战利品×1.40"));
        }

        [Test]
        public void Pool_AllChannelsCovered()
        {
            // 池定义自查：4 通道各有至少 1 个主通道修饰符（防后续删池项掏空通道）
            foreach (ModifierChannel channel in System.Enum.GetValues(typeof(ModifierChannel)))
            {
                bool covered = false;
                foreach (SessionModifierDefinition definition in SessionModifierRuleService.Pool)
                {
                    if (definition.Channel == channel)
                    {
                        covered = true;
                        break;
                    }
                }

                Assert.IsTrue(covered, $"通道 {channel} 无主通道修饰符");
            }
        }
    }
}
