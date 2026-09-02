namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Time;
    using NUnit.Framework;

    /// <summary>
    /// DayNightRuleService 纯函数单测 — 天索引/进度/相位边界/光照曲线。
    /// 光照公式迁移自旧版 GameTimeUI（sin × 6.2624 - 1.55 + 0.7, clamp [0.2, 0.8]），
    /// 以"一天"为严格周期（旧实现对总时间直接 sin，因 6.2624≠2π 每天相位微漂移，已修正）。
    /// </summary>
    [TestFixture]
    public class DayNightRuleServiceTests
    {
        private const float DayLength = 600f; // 10 分钟一天（与 0.2 压缩后的 GameDayTime 同量级）

        [Test]
        public void DayIndex_AdvancesAtDayBoundary()
        {
            Assert.AreEqual(0, DayNightRuleService.DayIndex(0.0, DayLength));
            Assert.AreEqual(0, DayNightRuleService.DayIndex(DayLength - 0.001f, DayLength));
            Assert.AreEqual(1, DayNightRuleService.DayIndex(DayLength, DayLength));
            Assert.AreEqual(3, DayNightRuleService.DayIndex(DayLength * 3.5f, DayLength));
        }

        [Test]
        public void DayIndex_ZeroOrNegativeDayLength_ReturnsZero()
        {
            Assert.AreEqual(0, DayNightRuleService.DayIndex(100.0, 0f));
            Assert.AreEqual(0, DayNightRuleService.DayIndex(100.0, -1f));
        }

        [Test]
        public void DayProgress_MidnightIsZeroNoonIsHalf()
        {
            Assert.AreEqual(0.0, DayNightRuleService.DayProgress(0.0, DayLength), 0.0001);
            Assert.AreEqual(0.5, DayNightRuleService.DayProgress(DayLength * 0.5f, DayLength), 0.0001);
            Assert.AreEqual(0.25, DayNightRuleService.DayProgress(DayLength * 1.25f, DayLength), 0.0001);
        }

        [Test]
        public void GetPhase_FourQuadrantsOfADay()
        {
            // 0=午夜：夜；0.25=清晨：晨；0.5=正午：昼；0.75=黄昏：昏
            Assert.AreEqual(GamePhase.Night, DayNightRuleService.GetPhase(0.0, DayLength));
            Assert.AreEqual(GamePhase.Dawn, DayNightRuleService.GetPhase(DayLength * 0.25f, DayLength));
            Assert.AreEqual(GamePhase.Day, DayNightRuleService.GetPhase(DayLength * 0.5f, DayLength));
            Assert.AreEqual(GamePhase.Dusk, DayNightRuleService.GetPhase(DayLength * 0.75f, DayLength));
        }

        [Test]
        public void GetPhase_BoundariesMatchConstants()
        {
            float day = DayLength;
            // 夜：[0.80, 1.20)
            Assert.AreEqual(GamePhase.Night, DayNightRuleService.GetPhase(day * 0.80f, day));
            Assert.AreEqual(GamePhase.Night, DayNightRuleService.GetPhase(day * 0.19f, day));
            // 晨：[0.20, 0.30)
            Assert.AreEqual(GamePhase.Dawn, DayNightRuleService.GetPhase(day * 0.20f, day));
            Assert.AreEqual(GamePhase.Dawn, DayNightRuleService.GetPhase(day * 0.29f, day));
            // 昼：[0.30, 0.70)
            Assert.AreEqual(GamePhase.Day, DayNightRuleService.GetPhase(day * 0.30f, day));
            Assert.AreEqual(GamePhase.Day, DayNightRuleService.GetPhase(day * 0.69f, day));
            // 昏：[0.70, 0.80)
            Assert.AreEqual(GamePhase.Dusk, DayNightRuleService.GetPhase(day * 0.70f, day));
            Assert.AreEqual(GamePhase.Dusk, DayNightRuleService.GetPhase(day * 0.79f, day));
        }

        [Test]
        public void GetPhase_SamplesCoverAllPhasesInOrder()
        {
            GamePhase[] seen = new GamePhase[4];
            int count = 0;
            GamePhase last = (GamePhase)(-1);
            for (int i = 0; i < 240; i++)
            {
                double progress = i / 240.0;
                GamePhase phase = DayNightRuleService.GetPhaseByProgress(progress);
                if (phase != last)
                {
                    seen[count++] = phase;
                    last = phase;
                }
            }

            // 一天从午夜起：夜→晨→昼→昏（各出现一次，顺序固定）
            Assert.AreEqual(4, count);
            Assert.AreEqual(GamePhase.Night, seen[0]);
            Assert.AreEqual(GamePhase.Dawn, seen[1]);
            Assert.AreEqual(GamePhase.Day, seen[2]);
            Assert.AreEqual(GamePhase.Dusk, seen[3]);
        }

        [Test]
        public void GetLightIntensity_MidnightDarkNoonBright()
        {
            float midnight = DayNightRuleService.GetLightIntensity(0.0, DayLength);
            float noon = DayNightRuleService.GetLightIntensity(DayLength * 0.5f, DayLength);
            Assert.AreEqual(DayNightRuleService.LightIntensityMin, midnight, 0.0001f);
            Assert.AreEqual(DayNightRuleService.LightIntensityMax, noon, 0.0001f);
        }

        [Test]
        public void GetLightIntensity_AlwaysWithinClampRange()
        {
            for (int i = 0; i < 100; i++)
            {
                double t = DayLength * i / 100.0;
                float intensity = DayNightRuleService.GetLightIntensity(t, DayLength);
                Assert.GreaterOrEqual(intensity, DayNightRuleService.LightIntensityMin - 0.0001f, $"t={t}");
                Assert.LessOrEqual(intensity, DayNightRuleService.LightIntensityMax + 0.0001f, $"t={t}");
            }
        }

        [Test]
        public void GetLightIntensity_PeriodicPerDay()
        {
            // 以天为严格周期：同一天进度在不同天光照一致
            double progress = 0.37;
            float day0 = DayNightRuleService.GetLightIntensity(DayLength * progress, DayLength);
            float day5 = DayNightRuleService.GetLightIntensity(DayLength * (progress + 5), DayLength);
            Assert.AreEqual(day0, day5, 0.0001f);
        }

        [Test]
        public void IsNight_MatchesPhaseCheck()
        {
            Assert.IsTrue(DayNightRuleService.IsNight(DayLength * 0.9f, DayLength));
            Assert.IsFalse(DayNightRuleService.IsNight(DayLength * 0.5f, DayLength));
        }

        [Test]
        public void SecondsUntilPhaseStart_BeforeTarget_CountsDownSameDay()
        {
            // 正午（0.5）距今晚夜始（0.80）= 0.3 天
            Assert.AreEqual(DayLength * 0.3f, DayNightRuleService.SecondsUntilPhaseStart(DayLength * 0.5f, DayLength, GamePhase.Night), 0.01f);
            // 午夜（0.0）距清晨（0.20）= 0.2 天
            Assert.AreEqual(DayLength * 0.2f, DayNightRuleService.SecondsUntilPhaseStart(0.0, DayLength, GamePhase.Dawn), 0.01f);
        }

        [Test]
        public void SecondsUntilPhaseStart_AfterTarget_WaitsNextCycle()
        {
            // 深夜 0.9（昨夜已开波）距明夜（1.80）= 0.9 天 — 保证一夜一波不重复
            Assert.AreEqual(DayLength * 0.9f, DayNightRuleService.SecondsUntilPhaseStart(DayLength * 0.9f, DayLength, GamePhase.Night), 0.01f);
            // 午夜后半夜 0.1（属夜区间 [0.8,1.2)）距今晚夜始（0.80）= 0.7 天
            Assert.AreEqual(DayLength * 0.7f, DayNightRuleService.SecondsUntilPhaseStart(DayLength * 0.1f, DayLength, GamePhase.Night), 0.01f);
        }

        [Test]
        public void SecondsUntilPhaseStart_AtExactStart_WaitsFullCycle()
        {
            // 恰在夜始 0.80：本次夜已开始（波应已开），下一波在明夜 = 1.0 天
            Assert.AreEqual(DayLength, DayNightRuleService.SecondsUntilPhaseStart(DayLength * 0.8f, DayLength, GamePhase.Night), 0.01f);
        }

        [Test]
        public void SecondsUntilPhaseStart_ZeroDayLength_ReturnsZero()
        {
            Assert.AreEqual(0f, DayNightRuleService.SecondsUntilPhaseStart(100.0, 0f, GamePhase.Night));
        }
    }
}
