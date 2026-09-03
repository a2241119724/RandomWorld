namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Worker;
    using NUnit.Framework;

    /// <summary>
    /// 熟练度→进度倍率纯算术 — 0 点无加成、满 100 点 +40%、越界夹取 [0,100]。
    /// </summary>
    [TestFixture]
    public class WorkerSkillProgressServiceTests
    {
        [Test]
        public void ZeroProficiency_NoBonus()
        {
            Assert.AreEqual(1.0f, WorkerSkillProgressService.GetMultiplier(0f), 1e-4f);
        }

        [Test]
        public void MaxProficiency_FortyPercentBonus()
        {
            Assert.AreEqual(1.4f, WorkerSkillProgressService.GetMultiplier(100f), 1e-4f);
        }

        [Test]
        public void MidProficiency_LinearScale()
        {
            // 50 点 × 0.004 = +0.2
            Assert.AreEqual(1.2f, WorkerSkillProgressService.GetMultiplier(50f), 1e-4f);
        }

        [Test]
        public void NegativeProficiency_ClampedToZero()
        {
            Assert.AreEqual(1.0f, WorkerSkillProgressService.GetMultiplier(-5f), 1e-4f);
        }

        [Test]
        public void OverflowProficiency_ClampedTo100()
        {
            Assert.AreEqual(1.4f, WorkerSkillProgressService.GetMultiplier(150f), 1e-4f);
        }

        [Test]
        public void GainPerCompletion_Constant()
        {
            Assert.AreEqual(0.8f, WorkerSkillProgressService.SkillGainPerCompletion);
        }
    }
}
