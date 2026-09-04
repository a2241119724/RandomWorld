namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Character.Enemy;
    using LAB2D.Domain.Wave;
    using NUnit.Framework;
    using System.Collections.Generic;

    [TestFixture]
    public class WaveSpawnPlanServiceTests
    {
        private readonly WaveRuleService ruleService = new WaveRuleService();
        private WaveSpawnPlanService service;

        [SetUp]
        public void SetUp()
        {
            this.service = new WaveSpawnPlanService(this.ruleService);
        }

        private static WaveConfigModel TestConfig()
        {
            return new WaveConfigModel
            {
                BaseEnemyCount = 5,
                EnemiesPerWaveIncrease = 2,
                TotalWaves = 10,
                DifficultyScalePerWave = 0.1f,
            };
        }

        [Test]
        public void CreatePlan_GeneratesRequests()
        {
            var state = new WaveRuntimeState();
            state.BeginNextWave(0);
            WaveSpawnPlan plan = this.service.CreatePlan(state, TestConfig(), 10);
            Assert.AreEqual(5, plan.BaseEnemyCount);
            Assert.AreEqual(10, plan.TotalEnemyCount);
            Assert.AreEqual(10, plan.Requests.Count);
        }

        [Test]
        public void CreatePlan_NegativeAdjustedCount_ClampsTo1()
        {
            var state = new WaveRuntimeState();
            state.BeginNextWave(0);
            WaveSpawnPlan plan = this.service.CreatePlan(state, TestConfig(), -5);
            Assert.AreEqual(1, plan.TotalEnemyCount);
            Assert.AreEqual(1, plan.Requests.Count);
        }

        [Test]
        public void CreatePlan_FirstRequestHasCorrectWaveIndex()
        {
            var state = new WaveRuntimeState();
            state.BeginNextWave(0);
            WaveSpawnPlan plan = this.service.CreatePlan(state, TestConfig(), 3);
            Assert.AreEqual(1, plan.Requests[0].WaveIndex);
            Assert.AreEqual(0, plan.Requests[0].SpawnIndex);
            Assert.AreEqual(3, plan.Requests[0].TotalEnemiesInWave);
        }

        [Test]
        public void CreatePlan_LastRequestHasLastSpawnIndex()
        {
            var state = new WaveRuntimeState();
            state.BeginNextWave(0);
            WaveSpawnPlan plan = this.service.CreatePlan(state, TestConfig(), 3);
            Assert.AreEqual(2, plan.Requests[2].SpawnIndex);
        }

        [Test]
        public void CreatePlan_DifficultyScaleIsPropagated()
        {
            var state = new WaveRuntimeState();
            state.BeginNextWave(0);
            WaveSpawnPlan plan = this.service.CreatePlan(state, TestConfig(), 1);
            Assert.AreEqual(1f, plan.Requests[0].DifficultyScale, 0.0001f);
        }

        [Test]
        public void PickEnemyKind_BeforeNewEnemyStartWave_UsesLegacyPoolOnly()
        {
            // TestConfig 未设 NewEnemyStartWave，默认 3 → 第 1/2 波只用旧池
            for (int spawnIndex = 0; spawnIndex < 6; spawnIndex++)
            {
                WaveEnemyKind kind = this.ruleService.PickEnemyKind(1, spawnIndex, TestConfig());
                Assert.IsTrue(kind == WaveEnemyKind.Common || kind == WaveEnemyKind.Seek,
                    $"wave1 spawn{spawnIndex} 不应出新种，实际 {kind}");
            }
        }

        [Test]
        public void PickEnemyKind_FromNewEnemyStartWave_IncludesAllKinds()
        {
            var seen = new HashSet<WaveEnemyKind>();
            // 混池周期长 8，取满一个周期
            for (int spawnIndex = 0; spawnIndex < 8; spawnIndex++)
            {
                seen.Add(this.ruleService.PickEnemyKind(3, spawnIndex, TestConfig()));
            }

            Assert.IsTrue(seen.Contains(WaveEnemyKind.Common), "混池应含 Common");
            Assert.IsTrue(seen.Contains(WaveEnemyKind.Seek), "混池应含 Seek");
            Assert.IsTrue(seen.Contains(WaveEnemyKind.Charge), "混池应含 Charge");
            Assert.IsTrue(seen.Contains(WaveEnemyKind.Shoot), "混池应含 Shoot");
        }

        [Test]
        public void PickEnemyKind_IsDeterministicRotation()
        {
            // 轮转确定性：同 wave/spawnIndex 多次调用结果一致
            Assert.AreEqual(
                this.ruleService.PickEnemyKind(3, 2, TestConfig()),
                this.ruleService.PickEnemyKind(3, 2, TestConfig()));
        }

        [Test]
        public void CreatePlan_FillsEnemyKindIdFromRule()
        {
            var state = new WaveRuntimeState();
            state.BeginNextWave(2); // CurrentWaveIndex: 0→1（参数是波前存活数，非波号——首版注释误读）
            WaveSpawnPlan plan = this.service.CreatePlan(state, TestConfig(), 8);
            for (int i = 0; i < plan.Requests.Count; i++)
            {
                Assert.AreEqual(
                    (int)this.ruleService.PickEnemyKind(state.CurrentWaveIndex, i, TestConfig()),
                    plan.Requests[i].EnemyKindId,
                    $"spawnIndex={i} 的 EnemyKindId 应与规则轮转一致");
            }
        }

        [Test]
        public void GetPrefabNameForKind_MapsAllKinds()
        {
            Assert.AreEqual("CommonEnemy", EnemyCreator.GetPrefabNameForKind((int)WaveEnemyKind.Common));
            Assert.AreEqual("SeekEnemy", EnemyCreator.GetPrefabNameForKind((int)WaveEnemyKind.Seek));
            Assert.AreEqual("ChargeEnemy", EnemyCreator.GetPrefabNameForKind((int)WaveEnemyKind.Charge));
            Assert.AreEqual("ShootEnemy", EnemyCreator.GetPrefabNameForKind((int)WaveEnemyKind.Shoot));
            // 越界/未知 Id 兜底 Common
            Assert.AreEqual("CommonEnemy", EnemyCreator.GetPrefabNameForKind(99));
        }
    }
}
