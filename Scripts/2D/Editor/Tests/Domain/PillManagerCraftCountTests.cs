namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Gameplay.Alchemy;
    using LAB2D.Gameplay;
    using GameCharacter = LAB2D.Character.Character;
    using NUnit.Framework;

    /// <summary>
    /// PillManager 炼成计数：TryCraft 成功路径 +1、失败不计、计数只增不重置（会话累计语义）。
    /// LogProvider 替换为静默桩（AWorkerTask.LogProvider 注释明示的可替换设计），
    /// 避免裸 Mono 环境触发 LogManager 的 Application.persistentDataPath icall。
    /// </summary>
    [TestFixture]
    public class PillManagerCraftCountTests
    {
        private System.Action<string, LAB2D.Manager.LogManager.LogLevelEnum> originalLogProvider;

        [SetUp]
        public void SetUp()
        {
            this.originalLogProvider = AWorkerTask.LogProvider;
            AWorkerTask.LogProvider = (message, level) => { };
        }

        [TearDown]
        public void TearDown()
        {
            AWorkerTask.LogProvider = this.originalLogProvider;
        }

        private static GameCharacter.CharacterData NewCultivator(int realmIndex, float qi)
        {
            GameCharacter.CharacterData data = new GameCharacter.CharacterData();
            GrowthData.Ensure(ref data.Growth);
            data.Growth.RealmIndex = realmIndex;
            data.Growth.Qi = qi;
            return data;
        }

        [Test]
        public void TryCraft_Success_IncrementsCount()
        {
            PillManager manager = PillManager.Instance;
            int before = manager.TotalPillsCrafted;
            GameCharacter.CharacterData data = NewCultivator(3, 10000f);

            bool crafted = manager.TryCraft(PillLibrary.HuiQiSan.Id, data);

            Assert.IsTrue(crafted, "金丹境界+万灵气，回气散应炼成");
            Assert.AreEqual(before + 1, manager.TotalPillsCrafted, "炼成后计数 +1");
        }

        [Test]
        public void TryCraft_RealmTooLow_DoesNotIncrement()
        {
            PillManager manager = PillManager.Instance;
            int before = manager.TotalPillsCrafted;
            GameCharacter.CharacterData mortal = NewCultivator(0, 100000f); // 凡人，境界不足

            bool crafted = manager.TryCraft(PillLibrary.HuiQiSan.Id, mortal);

            Assert.IsFalse(crafted, "凡人境界不足应炼制失败");
            Assert.AreEqual(before, manager.TotalPillsCrafted, "失败不计入炼成数");
        }

        [Test]
        public void TryCraft_UnknownPill_DoesNotIncrement()
        {
            PillManager manager = PillManager.Instance;
            int before = manager.TotalPillsCrafted;

            bool crafted = manager.TryCraft("NoSuchPill", NewCultivator(3, 10000f));

            Assert.IsFalse(crafted);
            Assert.AreEqual(before, manager.TotalPillsCrafted);
        }
    }
}
