namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Common;
    using NUnit.Framework;

    [TestFixture]
    public class CharacterRuntimeStateTests
    {
        [Test]
        public void Constructor_SetsAllFields()
        {
            CharacterRuntimeState state = new CharacterRuntimeState(80f, 100f, 50, 100, 3, 10, 20, 5f, false);
            Assert.AreEqual(80f, state.Hp, 0.0001f);
            Assert.AreEqual(100f, state.MaxHp, 0.0001f);
            Assert.AreEqual(50, state.Mp);
            Assert.AreEqual(100, state.MaxMp);
            Assert.AreEqual(3, state.Level);
            Assert.AreEqual(10, state.CurExperience);
            Assert.AreEqual(20, state.MaxExperience);
            Assert.AreEqual(5f, state.LastDamageTime, 0.0001f);
            Assert.IsFalse(state.IsRespawning);
        }

        [Test]
        public void IsDead_HpPositive_ReturnsFalse()
        {
            CharacterRuntimeState state = new CharacterRuntimeState(10f, 100f);
            Assert.IsFalse(state.IsDead);
            Assert.IsTrue(state.IsAlive);
        }

        [Test]
        public void IsDead_HpZero_ReturnsTrue()
        {
            CharacterRuntimeState state = new CharacterRuntimeState(0f, 100f);
            Assert.IsTrue(state.IsDead);
            Assert.IsFalse(state.IsAlive);
        }

        [Test]
        public void IsDead_HpNegative_ReturnsTrue()
        {
            CharacterRuntimeState state = new CharacterRuntimeState(-5f, 100f);
            Assert.IsTrue(state.IsDead);
        }

        [Test]
        public void WithHp_ReturnsNewState_OriginalUnchanged()
        {
            CharacterRuntimeState original = new CharacterRuntimeState(50f, 100f);
            CharacterRuntimeState updated = original.WithHp(75f);
            Assert.AreEqual(50f, original.Hp, "原始状态不应被修改");
            Assert.AreEqual(75f, updated.Hp);
            Assert.AreEqual(original.MaxHp, updated.MaxHp);
        }

        [Test]
        public void WithHpAndDamageTime_UpdatesBoth()
        {
            CharacterRuntimeState original = new CharacterRuntimeState(50f, 100f, lastDamageTime: 1f);
            CharacterRuntimeState updated = original.WithHpAndDamageTime(40f, 5f);
            Assert.AreEqual(40f, updated.Hp);
            Assert.AreEqual(5f, updated.LastDamageTime);
            Assert.AreEqual(1f, original.LastDamageTime, "原始状态不应被修改");
        }

        [Test]
        public void WithExperience_UpdatesLevelAndExperience()
        {
            CharacterRuntimeState original = new CharacterRuntimeState(100f, 100f, level: 2, curExperience: 5, maxExperience: 10);
            CharacterRuntimeState updated = original.WithExperience(2, 20, 3);
            Assert.AreEqual(3, updated.Level);
            Assert.AreEqual(2, updated.CurExperience);
            Assert.AreEqual(20, updated.MaxExperience);
            Assert.AreEqual(2, original.Level, "原始状态不应被修改");
        }

        [Test]
        public void WithRespawning_UpdatesFlag()
        {
            CharacterRuntimeState original = new CharacterRuntimeState(100f, 100f, isRespawning: false);
            CharacterRuntimeState updated = original.WithRespawning(true);
            Assert.IsTrue(updated.IsRespawning);
            Assert.IsFalse(original.IsRespawning, "原始状态不应被修改");
        }

        [Test]
        public void FromCharacterData_CreatesCorrectState()
        {
            CharacterRuntimeState state = CharacterRuntimeState.FromCharacterData(
                80f, 100f, 50, 100, 3, 10, 20, 2f, false);
            Assert.AreEqual(80f, state.Hp);
            Assert.AreEqual(100f, state.MaxHp);
            Assert.AreEqual(50, state.Mp);
            Assert.AreEqual(3, state.Level);
            Assert.AreEqual(2f, state.LastDamageTime);
            Assert.IsFalse(state.IsRespawning);
        }

        [Test]
        public void FromCharacterData_DefaultParams_SensibleDefaults()
        {
            CharacterRuntimeState state = CharacterRuntimeState.FromCharacterData(100f, 100f, 100, 100, 1, 0, 4);
            Assert.AreEqual(-99f, state.LastDamageTime, "默认受击时间应为远古值");
            Assert.IsFalse(state.IsRespawning, "默认不应在复活中");
        }
    }
}
