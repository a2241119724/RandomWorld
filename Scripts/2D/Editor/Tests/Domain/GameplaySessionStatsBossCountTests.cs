namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Common;
    using LAB2D.Gameplay;
    using NUnit.Framework;

    /// <summary>
    /// Boss 击杀计数：RecordEnemyDefeated 的 isBoss 区分、快照透出与 ResetSession 清零。
    /// Boss 标记的生产者是 WaveBossRewardManager.ConfigureSpawnedEnemy（生成时刻写入 EnemyData.IsBoss），
    /// 此处验证统计侧消费链（4 参重载为纯参数入口，可裸测）。
    /// </summary>
    [TestFixture]
    public class GameplaySessionStatsBossCountTests
    {
        private class FakeGameTime : IGameTime
        {
            public float DeltaTime => 0.016f;
            public float Time => 1f;
            public float RealtimeSinceStartup => 10f;
        }

        private GameplaySessionStats stats;

        [SetUp]
        public void SetUp()
        {
            Core.ServiceLocator.Register<IGameTime>(new FakeGameTime());
            this.stats = new GameplaySessionStats();
        }

        [TearDown]
        public void TearDown()
        {
            Core.ServiceLocator.Unregister<IGameTime>();
        }

        [Test]
        public void RecordEnemyDefeated_Boss_IncrementsBothCounters()
        {
            this.stats.RecordEnemyDefeated("CommonEnemy", "Player", 10, true);

            GameplaySessionStatsSnapshot snap = this.stats.CreateScalarSnapshot();
            Assert.AreEqual(1, snap.TotalBossDefeatedCount, "Boss 击杀应计入 Boss 计数");
            Assert.AreEqual(1, snap.TotalDefeatedEnemyCount, "Boss 也是敌人，总击杀计数同步 +1");
        }

        [Test]
        public void RecordEnemyDefeated_Normal_DoesNotIncrementBossCounter()
        {
            this.stats.RecordEnemyDefeated("CommonEnemy", "Player", 10, false);

            GameplaySessionStatsSnapshot snap = this.stats.CreateScalarSnapshot();
            Assert.AreEqual(0, snap.TotalBossDefeatedCount, "普通敌人不应计入 Boss 计数");
            Assert.AreEqual(1, snap.TotalDefeatedEnemyCount);
        }

        [Test]
        public void RecordEnemyDefeated_ThreeParamOverload_DefaultsToNonBoss()
        {
            this.stats.RecordEnemyDefeated("CommonEnemy", "Player", 10);

            Assert.AreEqual(0, this.stats.CreateScalarSnapshot().TotalBossDefeatedCount,
                "3 参重载（旧调用方）默认非 Boss，行为不变");
        }

        [Test]
        public void RecordEnemyDefeated_Mixed_PreservesBothCounts()
        {
            this.stats.RecordEnemyDefeated("CommonEnemy", "Player", 10, false);
            this.stats.RecordEnemyDefeated("BossEnemy", "Player", 50, true);
            this.stats.RecordEnemyDefeated("CommonEnemy", "Player", 10, false);
            this.stats.RecordEnemyDefeated("BossEnemy", "Player", 50, true);

            GameplaySessionStatsSnapshot snap = this.stats.CreateScalarSnapshot();
            Assert.AreEqual(2, snap.TotalBossDefeatedCount);
            Assert.AreEqual(4, snap.TotalDefeatedEnemyCount);
        }

        [Test]
        public void ResetSession_ClearsBossCount()
        {
            this.stats.RecordEnemyDefeated("BossEnemy", "Player", 50, true);
            this.stats.ResetSession();

            Assert.AreEqual(0, this.stats.CreateScalarSnapshot().TotalBossDefeatedCount, "重置会话应清零 Boss 计数");
        }

        [Test]
        public void CreateSnapshot_Full_IncludesBossCount()
        {
            this.stats.RecordEnemyDefeated("BossEnemy", "Player", 50, true);

            Assert.AreEqual(1, this.stats.CreateSnapshot().TotalBossDefeatedCount,
                "完整快照也应透出 Boss 计数（供结算/调试页消费）");
        }
    }
}
