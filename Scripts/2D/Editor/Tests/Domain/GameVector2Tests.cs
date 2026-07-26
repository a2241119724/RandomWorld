namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Common;
    using NUnit.Framework;

    [TestFixture]
    public class GameVector2Tests
    {
        [Test]
        public void SqrDistanceTo_SamePoint_ReturnsZero()
        {
            GameVector2 a = new GameVector2(3f, 4f);
            Assert.AreEqual(0f, a.SqrDistanceTo(a), 0.0001f);
        }

        [Test]
        public void SqrDistanceTo_DifferentPoints_ReturnsCorrectSquaredDistance()
        {
            GameVector2 a = new GameVector2(0f, 0f);
            GameVector2 b = new GameVector2(3f, 4f);
            Assert.AreEqual(25f, a.SqrDistanceTo(b), 0.0001f);
        }

        [Test]
        public void Add_Operator_ReturnsSum()
        {
            GameVector2 result = new GameVector2(1f, 2f) + new GameVector2(3f, 4f);
            Assert.AreEqual(4f, result.X, 0.0001f);
            Assert.AreEqual(6f, result.Y, 0.0001f);
        }

        [Test]
        public void Subtract_Operator_ReturnsDifference()
        {
            GameVector2 result = new GameVector2(5f, 8f) - new GameVector2(2f, 3f);
            Assert.AreEqual(3f, result.X, 0.0001f);
            Assert.AreEqual(5f, result.Y, 0.0001f);
        }

        [Test]
        public void Multiply_Operator_ReturnsScaled()
        {
            GameVector2 result = new GameVector2(2f, 3f) * 2f;
            Assert.AreEqual(4f, result.X, 0.0001f);
            Assert.AreEqual(6f, result.Y, 0.0001f);
        }
    }
}
