namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay;
    using NUnit.Framework;

    [TestFixture]
    public class TemperatureRuleServiceTests
    {
        private readonly TemperatureRuleService service = new TemperatureRuleService();

        // ---- 季节 ----

        [Test]
        public void GetSeasonByGameDay_StartIsSpring()
        {
            Assert.AreEqual(SeasonType.Spring, this.service.GetSeasonByGameDay(0));
            Assert.AreEqual(SeasonType.Spring, this.service.GetSeasonByGameDay(4));
        }

        [Test]
        public void GetSeasonByGameDay_SeasonCycles()
        {
            Assert.AreEqual(SeasonType.Summer, this.service.GetSeasonByGameDay(5));
            Assert.AreEqual(SeasonType.Autumn, this.service.GetSeasonByGameDay(10));
            Assert.AreEqual(SeasonType.Winter, this.service.GetSeasonByGameDay(15));
            Assert.AreEqual(SeasonType.Spring, this.service.GetSeasonByGameDay(20));
        }

        [Test]
        public void GetBaseTemperature_MatchesTable()
        {
            Assert.AreEqual(18f, this.service.GetBaseTemperature(SeasonType.Spring), 0.0001f);
            Assert.AreEqual(30f, this.service.GetBaseTemperature(SeasonType.Summer), 0.0001f);
            Assert.AreEqual(18f, this.service.GetBaseTemperature(SeasonType.Autumn), 0.0001f);
            Assert.AreEqual(2f, this.service.GetBaseTemperature(SeasonType.Winter), 0.0001f);
        }

        // ---- 天气偏移 ----

        [Test]
        public void GetWeatherOffset_MatchesTable()
        {
            Assert.AreEqual(0f, this.service.GetWeatherOffset(WeatherType.Clear), 0.0001f);
            Assert.AreEqual(-6f, this.service.GetWeatherOffset(WeatherType.Rain), 0.0001f);
            Assert.AreEqual(-12f, this.service.GetWeatherOffset(WeatherType.Snow), 0.0001f);
        }

        // ---- 室外温度 ----

        [Test]
        public void GetOutdoorTemperature_DawnPhaseNearZero_SpringClear()
        {
            // gameDaySeconds=1800 时，curGameTime=445.5 → gameDay≈0.2475 → 相位 sin 项≈0
            // 第 0 游戏天 → Spring，Clear 偏移 0 → 温度 ≈ 18
            float temp = this.service.GetOutdoorTemperature(445.5, 1800.0, WeatherType.Clear);
            Assert.AreEqual(18f, temp, 0.5f);
        }

        [Test]
        public void GetOutdoorTemperature_WinterSnow_ColderThanBase()
        {
            // 第 19 游戏天 → Winter(2)，Snow(-12)，昼夜波动 ±4 内 → 恒在 [-14, -6] 区间
            float temp = this.service.GetOutdoorTemperature(19 * 1800.0, 1800.0, WeatherType.Snow);
            Assert.Less(temp, -5f);
        }

        [Test]
        public void GetOutdoorTemperature_SummerClear_WarmerThanBase()
        {
            // 第 7 游戏天 → Summer(30)，Clear(0)，昼夜波动 ±4 内 → 恒在 [26, 34] 区间
            float temp = this.service.GetOutdoorTemperature(7 * 1800.0, 1800.0, WeatherType.Clear);
            Assert.Greater(temp, 25.5f);
        }

        // ---- 房间温度 ----

        [Test]
        public void GetRoomTemperature_AddsInsulation()
        {
            Assert.AreEqual(24f, this.service.GetRoomTemperature(18f, 0f), 0.0001f);
        }

        [Test]
        public void GetRoomTemperature_AddsHeatPower()
        {
            Assert.AreEqual(39f, this.service.GetRoomTemperature(18f, 15f), 0.0001f);
        }

        // ---- 移动速度倍率 ----

        [Test]
        public void GetMoveSpeedMultiplier_ComfortZone_Is1()
        {
            Assert.AreEqual(1.0f, this.service.GetMoveSpeedMultiplier(20f), 0.0001f);
            Assert.AreEqual(1.0f, this.service.GetMoveSpeedMultiplier(15f), 0.0001f);
            Assert.AreEqual(1.0f, this.service.GetMoveSpeedMultiplier(30f), 0.0001f);
        }

        [Test]
        public void GetMoveSpeedMultiplier_Hot_Penalizes()
        {
            Assert.AreEqual(0.9f, this.service.GetMoveSpeedMultiplier(35f), 0.0001f);
            Assert.AreEqual(0.7f, this.service.GetMoveSpeedMultiplier(45f), 0.0001f);
        }

        [Test]
        public void GetMoveSpeedMultiplier_Cold_Penalizes()
        {
            Assert.AreEqual(0.9f, this.service.GetMoveSpeedMultiplier(10f), 0.0001f);
            Assert.AreEqual(0.7f, this.service.GetMoveSpeedMultiplier(0f), 0.0001f);
        }

        [Test]
        public void GetMoveSpeedMultiplier_ExtremeCold_ClampsToMin()
        {
            Assert.AreEqual(0.5f, this.service.GetMoveSpeedMultiplier(-15f), 0.0001f);
        }

        // ---- 疲劳消耗倍率 ----

        [Test]
        public void GetFatigueDecayMultiplier_ComfortZone_Is1()
        {
            Assert.AreEqual(1.0f, this.service.GetFatigueDecayMultiplier(20f), 0.0001f);
            Assert.AreEqual(1.0f, this.service.GetFatigueDecayMultiplier(10f), 0.0001f);
            Assert.AreEqual(1.0f, this.service.GetFatigueDecayMultiplier(30f), 0.0001f);
        }

        [Test]
        public void GetFatigueDecayMultiplier_Hot_Accelerates()
        {
            Assert.AreEqual(1.15f, this.service.GetFatigueDecayMultiplier(35f), 0.0001f);
            Assert.AreEqual(1.3f, this.service.GetFatigueDecayMultiplier(40f), 0.0001f);
        }

        [Test]
        public void GetFatigueDecayMultiplier_Cold_Accelerates()
        {
            Assert.AreEqual(1.3f, this.service.GetFatigueDecayMultiplier(0f), 0.0001f);
            Assert.AreEqual(1.15f, this.service.GetFatigueDecayMultiplier(5f), 0.0001f);
        }

        [Test]
        public void GetFatigueDecayMultiplier_ExtremeCold_ClampsToMax()
        {
            Assert.AreEqual(1.6f, this.service.GetFatigueDecayMultiplier(-20f), 0.0001f);
        }
    }
}
