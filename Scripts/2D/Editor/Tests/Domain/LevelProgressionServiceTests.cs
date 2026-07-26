namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Character;
    using NUnit.Framework;

    [TestFixture]
    public class LevelProgressionServiceTests
    {
        private readonly LevelProgressionService service = new LevelProgressionService();

        [Test]
        public void AddExperience_NotEnough_NoLevelUp()
        {
            LevelProgressionResult result = this.service.AddExperience(0, 100, 1, 50);
            Assert.AreEqual(50, result.CurrentExperience);
            Assert.AreEqual(100, result.MaxExperience);
            Assert.AreEqual(1, result.Level);
            Assert.IsFalse(result.LeveledUp);
        }

        [Test]
        public void AddExperience_ExactlyEnough_LevelsUp()
        {
            LevelProgressionResult result = this.service.AddExperience(0, 100, 1, 100);
            Assert.AreEqual(0, result.CurrentExperience);
            Assert.AreEqual(200, result.MaxExperience);
            Assert.AreEqual(2, result.Level);
            Assert.IsTrue(result.LeveledUp);
        }

        [Test]
        public void AddExperience_Overflow_CarriesOverWithDoubledMax()
        {
            LevelProgressionResult result = this.service.AddExperience(50, 100, 1, 120);
            Assert.AreEqual(70, result.CurrentExperience);
            Assert.AreEqual(200, result.MaxExperience);
            Assert.AreEqual(2, result.Level);
            Assert.IsTrue(result.LeveledUp);
        }

        [Test]
        public void AddExperience_MultipleLevels_LevelsUpOnceOnly()
        {
            LevelProgressionResult result = this.service.AddExperience(0, 100, 1, 250);
            Assert.AreEqual(50, result.CurrentExperience);
            Assert.AreEqual(200, result.MaxExperience);
            Assert.AreEqual(2, result.Level);
            Assert.IsTrue(result.LeveledUp);
        }

        [Test]
        public void AddExperience_ZeroMaxExperience_UsesFallback()
        {
            LevelProgressionResult result = this.service.AddExperience(0, 0, 5, 10);
            Assert.AreEqual(0, result.CurrentExperience);
            Assert.AreEqual(2, result.MaxExperience);
            Assert.AreEqual(6, result.Level);
            Assert.IsTrue(result.LeveledUp);
        }

        [Test]
        public void Result_Struct_AllPropertiesMatch()
        {
            LevelProgressionResult result = new LevelProgressionResult(30, 200, 3, true);
            Assert.AreEqual(30, result.CurrentExperience);
            Assert.AreEqual(200, result.MaxExperience);
            Assert.AreEqual(3, result.Level);
            Assert.IsTrue(result.LeveledUp);
        }
    }
}
