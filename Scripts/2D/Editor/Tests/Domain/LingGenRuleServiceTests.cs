namespace LAB2D.Editor.Tests.Domain
{
    using System;
    using System.Collections.Generic;
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Gameplay.Cultivation;
    using NUnit.Framework;

    [TestFixture]
    public class LingGenRuleServiceTests
    {
        [TearDown]
        public void TearDown()
        {
            LingGenRuleService.RandomFloatProvider = null;
        }

        /// <summary>依序取自固定序列的随机桩（耗尽后重复最后一个值）。</summary>
        private static void UseSequence(params float[] values)
        {
            int index = 0;
            LingGenRuleService.RandomFloatProvider = (min, max) =>
            {
                float value = values[Math.Min(index, values.Length - 1)];
                index++;
                return value;
            };
        }

        [Test]
        public void RollIfNotGenerated_NoProvider_Skips()
        {
            LingGenRuleService.RandomFloatProvider = null;
            GrowthData growth = new GrowthData();
            growth.Ensure();

            LingGenRuleService.RollIfNotGenerated(growth, true);

            Assert.IsFalse(growth.LingGenGenerated);
            Assert.AreEqual(0, growth.LingGenElements.Count);
        }

        [Test]
        public void RollIfNotGenerated_NonPlayer_Skips()
        {
            UseSequence(0f, 0f, 0f, 0f, 0f);
            GrowthData growth = new GrowthData();
            growth.Ensure();

            LingGenRuleService.RollIfNotGenerated(growth, false);

            Assert.IsFalse(growth.LingGenGenerated);
        }

        [Test]
        public void RollIfNotGenerated_GeneratesElementsAndMarks()
        {
            // 条数 roll=0.5 → 1 条；元素 roll=0 → 金（池首）
            UseSequence(0.5f, 0f);
            GrowthData growth = new GrowthData();
            growth.Ensure();

            LingGenRuleService.RollIfNotGenerated(growth, true);

            Assert.IsTrue(growth.LingGenGenerated);
            Assert.AreEqual(1, growth.LingGenElements.Count);
            Assert.AreEqual((int)Element.Metal, growth.LingGenElements[0]);
        }

        [Test]
        public void RollIfNotGenerated_ThreeElements_NoDuplicate()
        {
            // 条数 roll=0.99 → 3 条；元素 roll 依次取池首 → 金木水
            UseSequence(0.99f, 0f, 0f, 0f);
            GrowthData growth = new GrowthData();
            growth.Ensure();

            LingGenRuleService.RollIfNotGenerated(growth, true);

            Assert.AreEqual(3, growth.LingGenElements.Count);
            Assert.AreEqual((int)Element.Metal, growth.LingGenElements[0]);
            Assert.AreEqual((int)Element.Wood, growth.LingGenElements[1]);
            Assert.AreEqual((int)Element.Water, growth.LingGenElements[2]);
        }

        [Test]
        public void RollIfNotGenerated_AlreadyGenerated_NeverRerolls()
        {
            UseSequence(0.5f, 0f);
            GrowthData growth = new GrowthData();
            growth.Ensure();
            LingGenRuleService.RollIfNotGenerated(growth, true);
            int first = growth.LingGenElements[0];

            LingGenRuleService.RollIfNotGenerated(growth, true);

            Assert.AreEqual(1, growth.LingGenElements.Count);
            Assert.AreEqual(first, growth.LingGenElements[0]);
        }

        [Test]
        public void GetCultivationMul_CountsMatches()
        {
            GrowthData growth = new GrowthData();
            growth.Ensure();
            growth.LingGenElements.Add((int)Element.Fire);
            growth.LingGenElements.Add((int)Element.Earth);

            Assert.AreEqual(1.2f, LingGenRuleService.GetCultivationMul(growth, Element.Fire), 0.0001f);
            Assert.AreEqual(1.2f, LingGenRuleService.GetCultivationMul(growth, Element.Earth), 0.0001f);
            Assert.AreEqual(1f, LingGenRuleService.GetCultivationMul(growth, Element.Metal), 0.0001f);
            Assert.AreEqual(1f, LingGenRuleService.GetCultivationMul(null, Element.Fire), 0.0001f);
        }

        [Test]
        public void GetElementName_AllElementsCovered()
        {
            var names = new HashSet<string>
            {
                LingGenRuleService.GetElementName(Element.Metal),
                LingGenRuleService.GetElementName(Element.Wood),
                LingGenRuleService.GetElementName(Element.Water),
                LingGenRuleService.GetElementName(Element.Fire),
                LingGenRuleService.GetElementName(Element.Earth),
            };

            Assert.AreEqual(5, names.Count);
        }

        [Test]
        public void FormatLingGenName_JoinsWithDelimiter()
        {
            GrowthData growth = new GrowthData();
            growth.Ensure();
            growth.LingGenElements.Add((int)Element.Metal);
            growth.LingGenElements.Add((int)Element.Water);

            Assert.AreEqual("金、水", LingGenRuleService.FormatLingGenName(growth));
            Assert.AreEqual("无", LingGenRuleService.FormatLingGenName(new GrowthData()));
            Assert.AreEqual("无", LingGenRuleService.FormatLingGenName(null));
        }

        [Test]
        public void FormatRevealMessage_NotGenerated_ReturnsNull()
        {
            // 未 roll（LingGenGenerated=false）不揭晓——调用方跳过
            GrowthData growth = new GrowthData();
            growth.Ensure();
            growth.LingGenElements.Add((int)Element.Fire);
            Assert.IsNull(LingGenRuleService.FormatRevealMessage(growth));
            Assert.IsNull(LingGenRuleService.FormatRevealMessage(null));
        }

        [Test]
        public void FormatRevealMessage_SingleElement_NoRaritySuffix()
        {
            GrowthData growth = new GrowthData();
            growth.Ensure();
            growth.LingGenGenerated = true;
            growth.LingGenElements.Add((int)Element.Fire);

            string message = LingGenRuleService.FormatRevealMessage(growth);
            Assert.IsTrue(message.StartsWith("天命灵根：火，"));
            StringAssert.DoesNotContain("双灵根", message);
            StringAssert.DoesNotContain("多元天资", message);
        }

        [Test]
        public void FormatRevealMessage_MultiElement_ContainsRarityAndBonus()
        {
            GrowthData growth = new GrowthData();
            growth.Ensure();
            growth.LingGenGenerated = true;
            growth.LingGenElements.Add((int)Element.Metal);
            growth.LingGenElements.Add((int)Element.Water);

            string message = LingGenRuleService.FormatRevealMessage(growth);
            Assert.IsTrue(message.Contains("金、水"));
            Assert.IsTrue(message.Contains("双灵根"));
            Assert.IsTrue(message.Contains("+20%/条"));

            GrowthData triple = new GrowthData();

            triple.Ensure();
            triple.LingGenGenerated = true;
            triple.LingGenElements.Add((int)Element.Metal);
            triple.LingGenElements.Add((int)Element.Water);
            triple.LingGenElements.Add((int)Element.Wood);
            Assert.IsTrue(LingGenRuleService.FormatRevealMessage(triple).Contains("多元天资"));
        }
    }
}
