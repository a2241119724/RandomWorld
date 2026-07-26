namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Player;
    using NUnit.Framework;

    [TestFixture]
    public class PlayerMovementServiceTests
    {
        private PlayerMovementService service;

        [SetUp]
        public void SetUp()
        {
            this.service = new PlayerMovementService();
        }

        [Test]
        public void CalculateMovement_BasicWalk_ReturnsBaseSpeed()
        {
            PlayerMoveResult result = this.service.CalculateMovement(
                5.0f, 1.6f, false, 1.0f, 1.0f,
                new GameVector2(1.0f, 0.0f));

            Assert.AreEqual(5.0f, result.MoveSpeed, 0.01f);
            Assert.AreEqual(1.0f, result.Direction.X, 0.01f);
            Assert.AreEqual(0.0f, result.Direction.Y, 0.01f);
        }

        [Test]
        public void CalculateMovement_Running_AppliesRunMultiplier()
        {
            PlayerMoveResult result = this.service.CalculateMovement(
                5.0f, 1.6f, true, 1.0f, 1.0f,
                new GameVector2(1.0f, 0.0f));

            Assert.AreEqual(5.0f * 1.6f, result.MoveSpeed, 0.01f);
        }

        [Test]
        public void CalculateMovement_RunMultiplierBelowOne_ClampedToOne()
        {
            PlayerMoveResult result = this.service.CalculateMovement(
                5.0f, 0.5f, true, 1.0f, 1.0f,
                new GameVector2(1.0f, 0.0f));

            Assert.AreEqual(5.0f, result.MoveSpeed, 0.01f);
        }

        [Test]
        public void CalculateMovement_WeatherRain_SlowsMovement()
        {
            PlayerMoveResult clear = this.service.CalculateMovement(
                5.0f, 1.6f, false, 1.0f, 1.0f,
                new GameVector2(1.0f, 0.0f));

            PlayerMoveResult rain = this.service.CalculateMovement(
                5.0f, 1.6f, false, 0.7f, 1.0f,
                new GameVector2(1.0f, 0.0f));

            Assert.Less(rain.MoveSpeed, clear.MoveSpeed);
        }

        [Test]
        public void CalculateMovement_WaveReward_BoostsMovement()
        {
            PlayerMoveResult result = this.service.CalculateMovement(
                5.0f, 1.6f, false, 1.0f, 1.3f,
                new GameVector2(1.0f, 0.0f));

            Assert.AreEqual(5.0f * 1.3f, result.MoveSpeed, 0.01f);
        }

        [Test]
        public void CalculateMovement_AllModifiersStacked()
        {
            PlayerMoveResult result = this.service.CalculateMovement(
                5.0f, 1.6f, true, 0.8f, 1.2f,
                new GameVector2(0.0f, 1.0f));

            float expected = 5.0f * 0.8f * 1.2f * 1.6f;
            Assert.AreEqual(expected, result.MoveSpeed, 0.01f);
        }

        [Test]
        public void CalculateMovement_Velocity_MatchesDirection()
        {
            PlayerMoveResult result = this.service.CalculateMovement(
                10.0f, 1.0f, false, 1.0f, 1.0f,
                new GameVector2(0.6f, 0.8f));

            Assert.AreEqual(6.0f, result.Velocity.X, 0.01f);
            Assert.AreEqual(8.0f, result.Velocity.Y, 0.01f);
        }

        [Test]
        public void CalculateMovement_ZeroDirection_VelocityIsZero()
        {
            PlayerMoveResult result = this.service.CalculateMovement(
                5.0f, 1.6f, true, 1.0f, 1.0f,
                new GameVector2(0.0f, 0.0f));

            Assert.AreEqual(0.0f, result.Velocity.X, 0.01f);
            Assert.AreEqual(0.0f, result.Velocity.Y, 0.01f);
        }

        [Test]
        public void CalculateMovement_BaseSpeedZero_NoMovement()
        {
            PlayerMoveResult result = this.service.CalculateMovement(
                0.0f, 2.0f, true, 1.5f, 1.5f,
                new GameVector2(1.0f, 0.0f));

            Assert.AreEqual(0.0f, result.MoveSpeed, 0.01f);
            Assert.AreEqual(0.0f, result.Velocity.X, 0.01f);
        }

        [Test]
        public void CalculateMovement_NegativeDirection_Preserved()
        {
            PlayerMoveResult result = this.service.CalculateMovement(
                5.0f, 1.0f, false, 1.0f, 1.0f,
                new GameVector2(-1.0f, 0.0f));

            Assert.AreEqual(5.0f, result.MoveSpeed, 0.01f);
            Assert.AreEqual(-5.0f, result.Velocity.X, 0.01f);
        }
    }
}
