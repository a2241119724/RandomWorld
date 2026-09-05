namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Character;
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Gameplay.AwakenedPower;
    using NUnit.Framework;

    [TestFixture]
    public class AwakenedPowerRuleServiceTests
    {
        [TearDown]
        public void TearDown()
        {
            AwakenedPowerRuleService.RandomFloatProvider = null;
        }

        [Test]
        public void GetAwakenChance_FullHp_IsBaseChance()
        {
            Assert.AreEqual(AwakenedPowerRuleService.BaseAwakenChance,
                AwakenedPowerRuleService.GetAwakenChance(100f, 100f), 0.0001f);
        }

        [Test]
        public void GetAwakenChance_LowHp_IncreasesTowardMax()
        {
            // 半血：0.03 + 0.5×0.07 = 0.065
            Assert.AreEqual(0.065f, AwakenedPowerRuleService.GetAwakenChance(50f, 100f), 0.0001f);

            // 濒死（hp=0 才是 0% 血——首版用 hp=1 实为 1% 血得 0.0993，差超容差）
            Assert.AreEqual(0.10f, AwakenedPowerRuleService.GetAwakenChance(0f, 100f), 0.0001f);
        }

        [Test]
        public void GetAwakenChance_InvalidMaxHp_ReturnsZero()
        {
            Assert.AreEqual(0f, AwakenedPowerRuleService.GetAwakenChance(100f, 0f), 0.0001f);
        }

        [Test]
        public void CanAwaken_NullOrReachedLimit_ReturnsFalse()
        {
            Assert.IsFalse(AwakenedPowerRuleService.CanAwaken(null));

            GrowthData growth = new GrowthData();
            growth.Ensure(); // 集合字段兜底（构造不自动 Ensure，Manager 层同款约定）
            Assert.IsTrue(AwakenedPowerRuleService.CanAwaken(growth));

            // 上限 2（包 5 扩池后）：第 1 个仍可，第 2 个到顶
            growth.AwakenedPowerIds.Add(AwakenedPowerLibrary.FireBall.Id);
            Assert.IsTrue(AwakenedPowerRuleService.CanAwaken(growth));

            growth.AwakenedPowerIds.Add(AwakenedPowerLibrary.Telekinesis.Id);
            Assert.IsFalse(AwakenedPowerRuleService.CanAwaken(growth));
        }

        [Test]
        public void RollPowerId_NoProvider_ReturnsNull()
        {
            GrowthData growth = new GrowthData();
            growth.Ensure(); // 集合字段兜底（构造不自动 Ensure，Manager 层同款约定）

            Assert.IsNull(AwakenedPowerRuleService.RollPowerId(growth));
        }

        [Test]
        public void RollPowerId_PicksFromPool()
        {
            // 序列桩固定取 0 → 池首（念力）
            AwakenedPowerRuleService.RandomFloatProvider = (min, max) => 0f;
            GrowthData growth = new GrowthData();
            growth.Ensure(); // 集合字段兜底（构造不自动 Ensure，Manager 层同款约定）

            Assert.AreEqual(AwakenedPowerLibrary.Telekinesis.Id, AwakenedPowerRuleService.RollPowerId(growth));

            // 已有 1 个（上限 2）仍可 roll；加满 2 个后再 roll → null
            growth.AwakenedPowerIds.Add(AwakenedPowerLibrary.Telekinesis.Id);
            Assert.IsNotNull(AwakenedPowerRuleService.RollPowerId(growth));

            growth.AwakenedPowerIds.Add(AwakenedPowerLibrary.FireBall.Id);
            Assert.IsNull(AwakenedPowerRuleService.RollPowerId(growth));
        }

        [Test]
        public void Library_SixPools_UniqueIds_ResolveToSkillConstants()
        {
            // 包 5 扩池：6 条异能，Id/SkillId 唯一且都对应 SkillConstant 真实技能
            var skillIds = new System.Collections.Generic.HashSet<string>
            {
                SkillConstant.SkillWhirlwind,
                SkillConstant.SkillDash,
                SkillConstant.SkillPowerSurge,
                SkillConstant.SkillHealingLight,
                SkillConstant.SkillSweepAll,
                SkillConstant.SkillSkySplit,
                SkillConstant.SkillTelekinesis,
                SkillConstant.SkillFireBall,
            };
            var ids = new System.Collections.Generic.HashSet<string>();

            Assert.AreEqual(6, AwakenedPowerLibrary.All.Count);
            foreach (AwakenedPowerDef def in AwakenedPowerLibrary.All)
            {
                Assert.IsTrue(ids.Add(def.Id), $"异能 Id 重复: {def.Id}");
                Assert.IsTrue(skillIds.Contains(def.SkillId), $"{def.Name} 的 SkillId 未对应 SkillConstant: {def.SkillId}");

                // Worker 被动非中性（Stats 8 维之和或 MaxHpFlat 至少一项有值）
                BattleStats stats = def.WorkerPassiveBonus.Stats;
                float statSum = stats.ATN + stats.INT + stats.DEF + stats.RES
                    + stats.CRT + stats.CSD + stats.SPD + stats.HIT;
                Assert.IsTrue(
                    statSum > 0f || def.WorkerPassiveBonus.MaxHpFlat > 0f,
                    $"{def.Name} 的 Worker 被动不应为中性值");
            }
        }

        [Test]
        public void Get_NullOrUnknownId_ReturnsNull()
        {
            Assert.IsNull(AwakenedPowerLibrary.Get(null));
            Assert.IsNull(AwakenedPowerLibrary.Get("power_unknown"));
            Assert.AreEqual(AwakenedPowerLibrary.FireBall.Id, AwakenedPowerLibrary.Get("power_fireball").Id);
        }
    }
}
