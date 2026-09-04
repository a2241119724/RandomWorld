namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Gameplay.Cultivation;
    using LAB2D.Domain.Gameplay.GongFa;
    using NUnit.Framework;

    [TestFixture]
    public class GongFaRuleServiceTests
    {
        [Test]
        public void CanLearn_NullInputs_ReturnsFalse()
        {
            Assert.IsFalse(GongFaRuleService.CanLearn(null, GongFaLibrary.ChangChun));
            Assert.IsFalse(GongFaRuleService.CanLearn(new GrowthData(), null));
        }

        [Test]
        public void CanLearn_RealmTooLow_ReturnsFalse()
        {
            GrowthData growth = new GrowthData { RealmIndex = 0 };
            growth.Ensure();

            // 长春功需练气（Index 1）
            Assert.IsFalse(GongFaRuleService.CanLearn(growth, GongFaLibrary.ChangChun));

            growth.RealmIndex = 1;
            Assert.IsTrue(GongFaRuleService.CanLearn(growth, GongFaLibrary.ChangChun));
        }

        [Test]
        public void CanLearn_AlreadyLearned_ReturnsFalse()
        {
            GrowthData growth = new GrowthData { RealmIndex = 3 };
            growth.Ensure();
            growth.LearnedGongFaIds.Add(GongFaLibrary.HengSao.Id);

            Assert.IsFalse(GongFaRuleService.CanLearn(growth, GongFaLibrary.HengSao));
        }

        [Test]
        public void CanLearn_ExternalSkill_AnyRealm()
        {
            // 外功招式凡人可学（RequiredRealmIndex=0）
            GrowthData mortal = new GrowthData();
            mortal.Ensure();
            Assert.IsTrue(GongFaRuleService.CanLearn(mortal, GongFaLibrary.HengSao));
        }

        [Test]
        public void CanActivate_ExternalSkill_ReturnsFalse()
        {
            GrowthData growth = new GrowthData { RealmIndex = 3 };
            growth.Ensure();
            growth.LearnedGongFaIds.Add(GongFaLibrary.HengSao.Id);

            Assert.IsFalse(GongFaRuleService.CanActivate(growth, GongFaLibrary.HengSao));
        }

        [Test]
        public void CanActivate_RequiresLearned()
        {
            GrowthData growth = new GrowthData { RealmIndex = 3 };
            growth.Ensure();

            Assert.IsFalse(GongFaRuleService.CanActivate(growth, GongFaLibrary.XuanYang));

            growth.LearnedGongFaIds.Add(GongFaLibrary.XuanYang.Id);
            Assert.IsTrue(GongFaRuleService.CanActivate(growth, GongFaLibrary.XuanYang));
        }

        [Test]
        public void GetCultivationMul_MatchesLingGenElements()
        {
            GrowthData growth = new GrowthData();
            growth.Ensure();
            growth.LingGenElements.Add((int)Element.Fire);

            // 玄阳功为火系：匹配一行 +20%
            Assert.AreEqual(1.2f, GongFaRuleService.GetCultivationMul(growth, GongFaLibrary.XuanYang), 0.0001f);

            // 长春功为木系：无匹配
            Assert.AreEqual(1f, GongFaRuleService.GetCultivationMul(growth, GongFaLibrary.ChangChun), 0.0001f);

            // 无激活内功：1
            Assert.AreEqual(1f, GongFaRuleService.GetCultivationMul(growth, null), 0.0001f);
            Assert.AreEqual(1f, GongFaRuleService.GetCultivationMul(null, GongFaLibrary.XuanYang), 0.0001f);
        }

        [Test]
        public void GetLearnedExternalSkills_PreservesLearningOrder()
        {
            // 先学破空斩再学横扫千军：重建顺序应保持学习序（槽位稳定）
            var ids = new System.Collections.Generic.List<string>
            {
                GongFaLibrary.PoKong.Id,
                GongFaLibrary.ChangChun.Id, // 内功应被过滤
                GongFaLibrary.HengSao.Id,
            };

            System.Collections.Generic.List<GongFaDef> result = GongFaLibrary.GetLearnedExternalSkills(ids);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(GongFaLibrary.PoKong.Id, result[0].Id);
            Assert.AreEqual(GongFaLibrary.HengSao.Id, result[1].Id);
        }

        [Test]
        public void Library_AllFiveElementsCovered()
        {
            // 功法池按五行补全（包 5）：金木水火土全有，水系由玄水诀覆盖
            var elements = new System.Collections.Generic.HashSet<Element>();
            foreach (GongFaDef def in GongFaLibrary.All)
            {
                elements.Add(def.Element);
            }

            Assert.AreEqual(5, elements.Count, $"功法池应覆盖全部五行，实际: {string.Join(",", elements)}");
            Assert.IsTrue(elements.Contains(Element.Water));
        }

        [Test]
        public void CanLearn_XuanShui_WaterElementNeiGong()
        {
            // 玄水诀：水系内功，练气可修
            GrowthData growth = new GrowthData { RealmIndex = 1 };
            growth.Ensure();

            Assert.IsTrue(GongFaRuleService.CanLearn(growth, GongFaLibrary.XuanShui));

            GrowthData mortal = new GrowthData();
            mortal.Ensure();
            Assert.IsFalse(GongFaRuleService.CanLearn(mortal, GongFaLibrary.XuanShui));

            // 水灵根匹配修炼速度 +20%/条
            GrowthData waterRoot = new GrowthData { RealmIndex = 1 };
            waterRoot.Ensure();
            waterRoot.LingGenElements.Add((int)Element.Water);
            Assert.AreEqual(1.2f, GongFaRuleService.GetCultivationMul(waterRoot, GongFaLibrary.XuanShui), 0.0001f);
        }
    }
}
