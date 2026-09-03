namespace LAB2D.Editor.Tests.Domain
{
    using System.Collections.Generic;
    using LAB2D.Domain.Gameplay.Cultivation;
    using LAB2D.Domain.TurnBattle;
    using NUnit.Framework;

    [TestFixture]
    public class ElementCounterRuleServiceTests
    {
        [Test]
        public void CounterRing_FiveCounteredPairs_AllCounter()
        {
            // 相克环：金→木→土→水→火→金（克者打被克者灵根 = 1.30）
            Assert.AreEqual(ElementCounterRuleService.CounterMultiplier, ElementCounterRuleService.GetCounterMultiplier(Element.Metal, LingGen(Element.Wood)));
            Assert.AreEqual(ElementCounterRuleService.CounterMultiplier, ElementCounterRuleService.GetCounterMultiplier(Element.Wood, LingGen(Element.Earth)));
            Assert.AreEqual(ElementCounterRuleService.CounterMultiplier, ElementCounterRuleService.GetCounterMultiplier(Element.Earth, LingGen(Element.Water)));
            Assert.AreEqual(ElementCounterRuleService.CounterMultiplier, ElementCounterRuleService.GetCounterMultiplier(Element.Water, LingGen(Element.Fire)));
            Assert.AreEqual(ElementCounterRuleService.CounterMultiplier, ElementCounterRuleService.GetCounterMultiplier(Element.Fire, LingGen(Element.Metal)));
        }

        [Test]
        public void CounterRing_FiveReversedPairs_AllResist()
        {
            // 反向（被克者打克者灵根）= 0.75
            Assert.AreEqual(ElementCounterRuleService.ResistMultiplier, ElementCounterRuleService.GetCounterMultiplier(Element.Wood, LingGen(Element.Metal)));
            Assert.AreEqual(ElementCounterRuleService.ResistMultiplier, ElementCounterRuleService.GetCounterMultiplier(Element.Earth, LingGen(Element.Wood)));
            Assert.AreEqual(ElementCounterRuleService.ResistMultiplier, ElementCounterRuleService.GetCounterMultiplier(Element.Water, LingGen(Element.Earth)));
            Assert.AreEqual(ElementCounterRuleService.ResistMultiplier, ElementCounterRuleService.GetCounterMultiplier(Element.Fire, LingGen(Element.Water)));
            Assert.AreEqual(ElementCounterRuleService.ResistMultiplier, ElementCounterRuleService.GetCounterMultiplier(Element.Metal, LingGen(Element.Fire)));
        }

        [Test]
        public void MultiLingGen_CounterTakesPriority()
        {
            // 灵根 [火, 木]：金克木（克）且火克金（被克）并存 → 克制优先 1.30
            List<Element> lingGen = new List<Element> { Element.Fire, Element.Wood };
            Assert.AreEqual(
                ElementCounterRuleService.CounterMultiplier,
                ElementCounterRuleService.GetCounterMultiplier(Element.Metal, lingGen));
        }

        [Test]
        public void MultiLingGen_AnyCounteredLingGen_AppliesCounter()
        {
            // 灵根 [火, 水]，攻击土：土克水（第二条灵根被克）→ 1.30——多灵根逐条扫描
            List<Element> lingGen = new List<Element> { Element.Fire, Element.Water };
            Assert.AreEqual(
                ElementCounterRuleService.CounterMultiplier,
                ElementCounterRuleService.GetCounterMultiplier(Element.Earth, lingGen));
        }

        [Test]
        public void MultiLingGen_AnyCounteringLingGen_AppliesResist()
        {
            // 灵根 [火, 水]，攻击火：无灵根被火克（火克金不在），水克火（第二条灵根反克）→ 0.75
            List<Element> lingGen = new List<Element> { Element.Fire, Element.Water };
            Assert.AreEqual(
                ElementCounterRuleService.ResistMultiplier,
                ElementCounterRuleService.GetCounterMultiplier(Element.Fire, lingGen));
        }

        [Test]
        public void NeutralCases_ReturnOne()
        {
            // 相生（金生水）：无克制关系 → 1.0
            Assert.AreEqual(ElementCounterRuleService.NeutralMultiplier, ElementCounterRuleService.GetCounterMultiplier(Element.Metal, LingGen(Element.Water)));
            // 同元素（金 vs 金）：不自克 → 1.0
            Assert.AreEqual(ElementCounterRuleService.NeutralMultiplier, ElementCounterRuleService.GetCounterMultiplier(Element.Metal, LingGen(Element.Metal)));
        }

        [Test]
        public void NoElementOrNullLingGen_ReturnOne()
        {
            // 无元素技能（默认四技能）恒 1.0
            Assert.AreEqual(ElementCounterRuleService.NeutralMultiplier, ElementCounterRuleService.GetCounterMultiplier(null, LingGen(Element.Metal)));
            // Enemy 无灵根恒 1.0
            Assert.AreEqual(ElementCounterRuleService.NeutralMultiplier, ElementCounterRuleService.GetCounterMultiplier(Element.Fire, new List<Element>()));
            Assert.AreEqual(ElementCounterRuleService.NeutralMultiplier, ElementCounterRuleService.GetCounterMultiplier(Element.Fire, null));
        }

        [Test]
        public void GetCounteredElement_FollowsRing()
        {
            Assert.AreEqual(Element.Wood, ElementCounterRuleService.GetCounteredElement(Element.Metal));
            Assert.AreEqual(Element.Metal, ElementCounterRuleService.GetCounteredElement(Element.Fire));
            Assert.IsTrue(ElementCounterRuleService.Counters(Element.Metal, Element.Wood));
            Assert.IsFalse(ElementCounterRuleService.Counters(Element.Wood, Element.Metal));
        }

        private static List<Element> LingGen(params Element[] elements)
        {
            return new List<Element>(elements);
        }
    }
}
