namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Common;
    using NUnit.Framework;

    [TestFixture]
    public class MathHelperTests
    {
        [Test]
        public void Clamp_ValueInRange_ReturnsValue()
        {
            Assert.AreEqual(5f, MathHelper.Clamp(5f, 0f, 10f), 0.0001f);
        }

        [Test]
        public void Clamp_ValueBelowMin_ReturnsMin()
        {
            Assert.AreEqual(0f, MathHelper.Clamp(-1f, 0f, 10f), 0.0001f);
        }

        [Test]
        public void Clamp_ValueAboveMax_ReturnsMax()
        {
            Assert.AreEqual(10f, MathHelper.Clamp(15f, 0f, 10f), 0.0001f);
        }

        [Test]
        public void Clamp_EqualMinAndMax_ReturnsBoundary()
        {
            Assert.AreEqual(5f, MathHelper.Clamp(3f, 5f, 5f), 0.0001f);
            Assert.AreEqual(5f, MathHelper.Clamp(7f, 5f, 5f), 0.0001f);
        }

        [Test]
        public void Approximately_ExactEqual_ReturnsTrue()
        {
            Assert.IsTrue(MathHelper.Approximately(1.0f, 1.0f));
        }

        [Test]
        public void Approximately_VerySmallDifference_ReturnsTrue()
        {
            Assert.IsTrue(MathHelper.Approximately(1.0f, 1.0f + 5E-07f));
        }

        [Test]
        public void Approximately_LargeDifference_ReturnsFalse()
        {
            Assert.IsFalse(MathHelper.Approximately(1.0f, 2.0f));
        }

        [Test]
        public void Deg2Rad_Convert180_ReturnsPI()
        {
            Assert.AreEqual(MathHelper.PI, 180f * MathHelper.Deg2Rad, 0.0001f);
        }

        [Test]
        public void Rad2Deg_ConvertPI_Returns180()
        {
            Assert.AreEqual(180f, MathHelper.PI * MathHelper.Rad2Deg, 0.0001f);
        }
    }
}
