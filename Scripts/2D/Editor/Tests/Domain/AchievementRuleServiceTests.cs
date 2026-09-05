namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Gameplay;
    using LAB2D.Enum;
    using LAB2D.Gameplay;
    using LAB2D.Tool;
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.Reflection;

    [TestFixture]
    public class AchievementRuleServiceTests
    {
        private readonly AchievementRuleService service = new AchievementRuleService();

        [Test]
        public void GetElapsedMinutes_120Seconds_Returns2()
        {
            Assert.AreEqual(2, this.service.GetElapsedMinutes(120f));
        }

        /// <summary>
        /// 修仙成就注册完整性：9 条 cultivation_* 全部注册、类别正确、数值合法。
        /// 防回归：注册处删改 ID/TargetValue 时此处先红。读数映射在 UpdateProgressAll（运行时），不在此断言。
        /// </summary>
        [Test]
        public void RegisterAchievements_CultivationEntries_ExistAndWellFormed()
        {
            AchievementManager manager = AchievementManager.Instance;
            MethodInfo register = typeof(AchievementManager).GetMethod(
                "RegisterAchievements", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(register, "RegisterAchievements 应为 private 可反射方法");
            register.Invoke(manager, null);

            Dictionary<string, AchievementData> byId = new Dictionary<string, AchievementData>();
            foreach (AchievementData a in manager.AllAchievements)
            {
                byId[a.Id] = a;
            }

            string[] expectedIds =
            {
                "cultivation_realm_1", "cultivation_realm_2", "cultivation_realm_3",
                "cultivation_realm_4", "cultivation_realm_5",
                "cultivation_gongfa_3", "cultivation_gongfa_5",
                "cultivation_awaken_1", "cultivation_awaken_2",
                "alchemy_craft_1", "alchemy_craft_10",
                "cave_explore_1", "cave_explore_3",
            };
            foreach (string id in expectedIds)
            {
                Assert.IsTrue(byId.ContainsKey(id), $"缺少修仙成就 {id}");
                AchievementData a = byId[id];
                Assert.AreEqual(AchievementCategory.Cultivation, a.Category, $"{id} 类别应为修仙");
                Assert.Greater(a.TargetValue, 0, $"{id} TargetValue 应为正");
                Assert.Greater(a.Points, 0, $"{id} Points 应为正");
                Assert.IsFalse(string.IsNullOrEmpty(a.ConditionTemplate), $"{id} 条件模板不应为空");
            }

            // 境界五档：TargetValue 严格对应 RealmIndex 档位 1~5（练气~化神）
            for (int realm = 1; realm <= 5; realm++)
            {
                Assert.AreEqual(realm, byId[$"cultivation_realm_{realm}"].TargetValue,
                    $"境界档成就 realm_{realm} 的 TargetValue 应等于 RealmIndex 档位");
            }
        }

        [Test]
        public void GetCategoryDisplayName_Cultivation_ReturnsChinese()
        {
            Assert.AreEqual("修仙", AchievementTool.GetCategoryDisplayName(AchievementCategory.Cultivation));
        }

        /// <summary>
        /// 新增三成就的类别归属：回合制两入战斗、挚友入工人（面板 tab 按类别聚合，归属错则显示错页）。
        /// </summary>
        [Test]
        public void RegisterAchievements_TurnBattleAndSocial_CategoriesCorrect()
        {
            AchievementManager manager = AchievementManager.Instance;
            MethodInfo register = typeof(AchievementManager).GetMethod(
                "RegisterAchievements", BindingFlags.NonPublic | BindingFlags.Instance);
            register.Invoke(manager, null);

            Dictionary<string, AchievementData> byId = new Dictionary<string, AchievementData>();
            foreach (AchievementData a in manager.AllAchievements)
            {
                byId[a.Id] = a;
            }

            Assert.AreEqual(AchievementCategory.Combat, byId["combat_turnbattle_1"].Category);
            Assert.AreEqual(AchievementCategory.Combat, byId["combat_turnbattle_10"].Category);
            Assert.AreEqual(AchievementCategory.Worker, byId["social_friend_first"].Category);
        }

        [Test]
        public void GetElapsedMinutes_0Seconds_Returns0()
        {
            Assert.AreEqual(0, this.service.GetElapsedMinutes(0f));
        }

        [Test]
        public void GetElapsedMinutes_Negative_Returns0()
        {
            Assert.AreEqual(0, this.service.GetElapsedMinutes(-1f));
        }

        [Test]
        public void ClampProgressToTarget_UnderTarget_ReturnsProgress()
        {
            Assert.AreEqual(3, this.service.ClampProgressToTarget(3, 5));
        }

        [Test]
        public void ClampProgressToTarget_OverTarget_ReturnsTarget()
        {
            Assert.AreEqual(5, this.service.ClampProgressToTarget(7, 5));
        }

        [Test]
        public void ClampProgressToTarget_Equal_ReturnsTarget()
        {
            Assert.AreEqual(5, this.service.ClampProgressToTarget(5, 5));
        }

        [Test]
        public void GetProgressRatio_Half_Returns0_5()
        {
            Assert.AreEqual(0.5f, this.service.GetProgressRatio(5, 10), 0.0001f);
        }

        [Test]
        public void GetProgressRatio_Complete_Returns1()
        {
            Assert.AreEqual(1.0f, this.service.GetProgressRatio(10, 10), 0.0001f);
        }

        [Test]
        public void GetProgressRatio_ZeroTarget_Returns1()
        {
            Assert.AreEqual(1.0f, this.service.GetProgressRatio(5, 0), 0.0001f);
        }
    }
}
