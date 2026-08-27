namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Character.Worker;
    using LAB2D.Domain.Worker;
    using NUnit.Framework;

    /// <summary>
    /// 性格演化（强漂移）纯规则 — 累积封顶/饱和、迁移阈值、滞回带、日限流、边界 clamp。
    /// </summary>
    [TestFixture]
    public class PersonalityDriftRuleServiceTests
    {
        private static AWorker.WorkerData NewWorker()
        {
            AWorker.WorkerData wd = new AWorker.WorkerData();
            wd.Personality = new WorkerPersonality(50f, 50f, 50f, 50f);
            return wd;
        }

        // ===== Accumulate：事件强度 → 漂移桶 =====

        [Test]
        public void Accumulate_NegativeEvent_BuildsMoodDrift()
        {
            var mind = new WorkerMindData();

            PersonalityDriftRuleService.Accumulate(mind, WorkerMindConstant.EVT_NEAR_DEATH, 50f);

            Assert.AreEqual(-6f, mind.Drift.MoodDrift, 0.0001f); // 50 * 0.12 = 6，方向 -1
        }

        [Test]
        public void Accumulate_PositiveEvent_BuildsAmbitionDrift()
        {
            var mind = new WorkerMindData();

            PersonalityDriftRuleService.Accumulate(mind, WorkerMindConstant.EVT_BOUNTY_COMPLETED, 60f);

            Assert.Greater(mind.Drift.AmbitionDrift, 0f);
            Assert.Greater(mind.Drift.MoodDrift, 0f);
        }

        [Test]
        public void Accumulate_CapsPerEvent()
        {
            var mind = new WorkerMindData();

            PersonalityDriftRuleService.Accumulate(mind, WorkerMindConstant.EVT_BOUNTY_COMPLETED, 999f);

            Assert.AreEqual(WorkerMindConstant.DriftMaxPerEvent, mind.Drift.AmbitionDrift, 0.0001f);
        }

        [Test]
        public void Accumulate_SaturatesAtBucketCap()
        {
            var mind = new WorkerMindData();

            // 每次 -6，10 次 = -60，封顶 -DriftBucketAbsCap
            for (int i = 0; i < 10; i++)
            {
                PersonalityDriftRuleService.Accumulate(mind, WorkerMindConstant.EVT_NEAR_DEATH, 50f);
            }

            Assert.AreEqual(-WorkerMindConstant.DriftBucketAbsCap, mind.Drift.MoodDrift, 0.0001f);
        }

        [Test]
        public void Accumulate_MultiDimEvent_TradeRejectedLowersSociality()
        {
            var mind = new WorkerMindData();

            PersonalityDriftRuleService.Accumulate(mind, WorkerMindConstant.EVT_TRADE_REJECTED, 50f);

            Assert.AreEqual(-3f, mind.Drift.SocialityDrift, 0.0001f); // 6 * -0.5
            Assert.AreEqual(0f, mind.Drift.MoodDrift, 0.0001f);
        }

        [Test]
        public void Accumulate_UnknownType_NoEffect()
        {
            var mind = new WorkerMindData();

            PersonalityDriftRuleService.Accumulate(mind, "no_such_event", 50f);

            Assert.AreEqual(0f, mind.Drift.MoodDrift, 0.0001f);
            Assert.AreEqual(0f, mind.Drift.AmbitionDrift, 0.0001f);
            Assert.AreEqual(0f, mind.Drift.DiligenceDrift, 0.0001f);
            Assert.AreEqual(0f, mind.Drift.SocialityDrift, 0.0001f);
        }

        [Test]
        public void Accumulate_LifeEventType_Skipped_NoDoubleCount()
        {
            // 人生事件漂移由 WorkerLifeEventDef 显式累积，Accumulate 必须跳过防双计
            var mind = new WorkerMindData();

            PersonalityDriftRuleService.Accumulate(mind, WorkerMindConstant.EVT_INSIGHT, 60f);

            Assert.AreEqual(0f, mind.Drift.AmbitionDrift, 0.0001f);
            Assert.AreEqual(0f, mind.Drift.MoodDrift, 0.0001f);
        }

        [Test]
        public void Accumulate_NullMind_NoThrow()
        {
            Assert.DoesNotThrow(() =>
                PersonalityDriftRuleService.Accumulate(null, WorkerMindConstant.EVT_NEAR_DEATH, 50f));
        }

        // ===== Migrate：桶 → Personality =====

        [Test]
        public void Migrate_AboveThreshold_MigratesPersonality()
        {
            AWorker.WorkerData wd = NewWorker();
            wd.Mind.Drift.MoodDrift = 13f;

            int n = PersonalityDriftRuleService.Migrate(wd, 3);

            Assert.AreEqual(1, n);
            Assert.AreEqual(52f, wd.Personality.Mood, 0.0001f);
            Assert.AreEqual(0f, wd.Mind.Drift.MoodDrift, 0.0001f);
            Assert.AreEqual(1, wd.Mind.Drift.MoodDir);
            Assert.AreEqual(3, (int)wd.Mind.LastDriftMigrationDay);
        }

        [Test]
        public void Migrate_BelowThreshold_NoMigration()
        {
            AWorker.WorkerData wd = NewWorker();
            wd.Mind.Drift.MoodDrift = 11f;

            Assert.AreEqual(0, PersonalityDriftRuleService.Migrate(wd, 3));
            Assert.AreEqual(50f, wd.Personality.Mood, 0.0001f);
            Assert.AreEqual(11f, wd.Mind.Drift.MoodDrift, 0.0001f);
        }

        [Test]
        public void Migrate_AtBaseThreshold_Migrates()
        {
            AWorker.WorkerData wd = NewWorker();
            wd.Mind.Drift.MoodDrift = 12f;

            Assert.AreEqual(1, PersonalityDriftRuleService.Migrate(wd, 3));
            Assert.AreEqual(52f, wd.Personality.Mood, 0.0001f);
        }

        [Test]
        public void Migrate_OppositeDirection_NeedsHysteresisBand()
        {
            // 上次正向迁移（dir=+1），反向需 (12+6)=18
            AWorker.WorkerData wd = NewWorker();
            wd.Mind.Drift.MoodDir = 1;
            wd.Mind.Drift.MoodDrift = -17f;

            Assert.AreEqual(0, PersonalityDriftRuleService.Migrate(wd, 3));
            Assert.AreEqual(50f, wd.Personality.Mood, 0.0001f);

            wd.Mind.Drift.MoodDrift = -18f;
            Assert.AreEqual(1, PersonalityDriftRuleService.Migrate(wd, 4));
            Assert.AreEqual(48f, wd.Personality.Mood, 0.0001f);
            Assert.AreEqual(-1, wd.Mind.Drift.MoodDir);
        }

        [Test]
        public void Migrate_SameDirection_ContinuesAtBaseThreshold()
        {
            AWorker.WorkerData wd = NewWorker();
            wd.Mind.Drift.MoodDir = 1;
            wd.Mind.Drift.MoodDrift = 13f;

            Assert.AreEqual(1, PersonalityDriftRuleService.Migrate(wd, 3));
            Assert.AreEqual(52f, wd.Personality.Mood, 0.0001f);
            Assert.AreEqual(1, wd.Mind.Drift.MoodDir);
        }

        [Test]
        public void Migrate_OncePerDay_MultipleDimsOnlyFirst()
        {
            AWorker.WorkerData wd = NewWorker();
            wd.Mind.Drift.MoodDrift = 13f;
            wd.Mind.Drift.AmbitionDrift = 13f;

            Assert.AreEqual(1, PersonalityDriftRuleService.Migrate(wd, 3));
            Assert.AreEqual(52f, wd.Personality.Mood, 0.0001f);       // Mood 先迁
            Assert.AreEqual(50f, wd.Personality.Ambition, 0.0001f);   // Ambition 未迁
            Assert.AreEqual(13f, wd.Mind.Drift.AmbitionDrift, 0.0001f); // Ambition 桶保留
        }

        [Test]
        public void Migrate_SameDayTwice_SecondReturnsZero()
        {
            AWorker.WorkerData wd = NewWorker();
            wd.Mind.Drift.MoodDrift = 13f;
            Assert.AreEqual(1, PersonalityDriftRuleService.Migrate(wd, 3));

            wd.Mind.Drift.MoodDrift = 13f; // 同日再累积
            Assert.AreEqual(0, PersonalityDriftRuleService.Migrate(wd, 3));
        }

        [Test]
        public void Migrate_NextDay_CanMigrateAgain()
        {
            AWorker.WorkerData wd = NewWorker();
            wd.Mind.Drift.MoodDrift = 13f;
            PersonalityDriftRuleService.Migrate(wd, 3);

            wd.Mind.Drift.MoodDrift = 13f;
            Assert.AreEqual(1, PersonalityDriftRuleService.Migrate(wd, 4));
            Assert.AreEqual(54f, wd.Personality.Mood, 0.0001f); // 50 + 2 + 2
        }

        [Test]
        public void Migrate_ClampsToBounds()
        {
            AWorker.WorkerData wd = NewWorker();
            wd.Personality = new WorkerPersonality(99f, 50f, 50f, 50f);
            wd.Mind.Drift.MoodDrift = 13f;

            PersonalityDriftRuleService.Migrate(wd, 3);

            Assert.AreEqual(100f, wd.Personality.Mood, 0.0001f);
        }

        [Test]
        public void Migrate_NegativeDrift_MigratesDown()
        {
            AWorker.WorkerData wd = NewWorker();
            wd.Mind.Drift.MoodDrift = -13f;

            PersonalityDriftRuleService.Migrate(wd, 3);

            Assert.AreEqual(48f, wd.Personality.Mood, 0.0001f);
            Assert.AreEqual(-1, wd.Mind.Drift.MoodDir);
        }

        [Test]
        public void Migrate_NoBucket_NullMind_ReturnsZero()
        {
            Assert.AreEqual(0, PersonalityDriftRuleService.Migrate(null, 3));

            AWorker.WorkerData wd = NewWorker();
            Assert.AreEqual(0, PersonalityDriftRuleService.Migrate(wd, 3));
        }
    }
}
