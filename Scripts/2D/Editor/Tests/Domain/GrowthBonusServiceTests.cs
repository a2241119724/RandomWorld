namespace LAB2D.Editor.Tests.Domain
{
    using System.Collections.Generic;
    using LAB2D.Domain.Character;
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Item;
    using LAB2D.Gameplay;
    using LAB2D.Item.Backpack.Equipment;
    using LAB2D.Item.Backpack.Equipment.Weapon;
    using NUnit.Framework;
    using GameCharacter = LAB2D.Character.Character;

    [TestFixture]
    public class GrowthBonusServiceTests
    {
        [TearDown]
        public void TearDown()
        {
            // 恢复默认空收集，避免污染其他测试
            GameCharacter.CharacterData.GrowthCollectProvider = data => new GrowthSourceResult();
        }

        [Test]
        public void FromAffixes_NullOrEmpty_ReturnsZero()
        {
            Assert.AreEqual(BattleStats.Zero.ATN, GrowthBonusService.FromAffixes(null).Stats.ATN);
            Assert.AreEqual(0f, GrowthBonusService.FromAffixes(null).LifestealRatio, 0.0001f);
            Assert.AreEqual(0f, GrowthBonusService.FromAffixes(new List<EquipmentAffix>()).ReflectRatio, 0.0001f);
        }

        [Test]
        public void FromAffixes_NumericAffixes_ProjectToStatsAndMaxHp()
        {
            List<EquipmentAffix> affixes = new List<EquipmentAffix>
            {
                new EquipmentAffix(EquipmentAffixType.FlatAtn, 5f),
                new EquipmentAffix(EquipmentAffixType.FlatInt, 3f),
                new EquipmentAffix(EquipmentAffixType.MaxHp, 20f),
            };

            GrowthBonus bonus = GrowthBonusService.FromAffixes(affixes);

            Assert.AreEqual(5f, bonus.Stats.ATN, 0.0001f);
            Assert.AreEqual(3f, bonus.Stats.INT, 0.0001f);
            Assert.AreEqual(20f, bonus.MaxHpFlat, 0.0001f);
            Assert.AreEqual(0f, bonus.LifestealRatio, 0.0001f);
            Assert.AreEqual(0f, bonus.ReflectRatio, 0.0001f);
        }

        [Test]
        public void FromAffixes_SpecialAffixes_StayOutOfStats()
        {
            List<EquipmentAffix> affixes = new List<EquipmentAffix>
            {
                new EquipmentAffix(EquipmentAffixType.Lifesteal, 0.05f),
                new EquipmentAffix(EquipmentAffixType.Reflect, 0.1f),
            };

            GrowthBonus bonus = GrowthBonusService.FromAffixes(affixes);

            Assert.AreEqual(BattleStats.Zero.ATN, bonus.Stats.ATN);
            Assert.AreEqual(0f, bonus.MaxHpFlat, 0.0001f);
            Assert.AreEqual(0.05f, bonus.LifestealRatio, 0.0001f);
            Assert.AreEqual(0.1f, bonus.ReflectRatio, 0.0001f);
        }

        [Test]
        public void FromAffixes_DuplicateTypes_Stack()
        {
            List<EquipmentAffix> affixes = new List<EquipmentAffix>
            {
                new EquipmentAffix(EquipmentAffixType.Lifesteal, 0.03f),
                new EquipmentAffix(EquipmentAffixType.Lifesteal, 0.04f),
                new EquipmentAffix(EquipmentAffixType.FlatAtn, 3f),
                new EquipmentAffix(EquipmentAffixType.FlatAtn, 4f),
            };

            GrowthBonus bonus = GrowthBonusService.FromAffixes(affixes);

            Assert.AreEqual(0.07f, bonus.LifestealRatio, 0.0001f);
            Assert.AreEqual(7f, bonus.Stats.ATN, 0.0001f);
        }

        [Test]
        public void CollectFromData_NullData_ReturnsEmptyResult()
        {
            GrowthSourceResult result = GrowthBonusService.CollectFromData(null);

            Assert.AreEqual(0, result.Sources.Count);
        }

        [Test]
        public void CollectFromData_EquipmentAndWeapon_AllCollected()
        {
            GameCharacter.CharacterData data = new GameCharacter.CharacterData();
            data.GetEquipments()[AEquipment.EquipTypeEnum.Ring] = new TestEquipment(
                new EquipmentAffix(EquipmentAffixType.FlatAtn, 2f));
            data.Weapon = new TestWeapon(
                new EquipmentAffix(EquipmentAffixType.FlatInt, 3f),
                new EquipmentAffix(EquipmentAffixType.Lifesteal, 0.05f));

            GrowthSourceResult result = GrowthBonusService.CollectFromData(data);

            Assert.AreEqual(2, result.Sources.Count);
            Assert.AreEqual(2f, result.Special.Stats.ATN, 0.0001f);
            Assert.AreEqual(3f, result.Special.Stats.INT, 0.0001f);
            Assert.AreEqual(0.05f, result.Special.LifestealRatio, 0.0001f);
        }

        [Test]
        public void Install_ProviderCollectsEquippedAffixes()
        {
            GrowthBonusService.Install();

            GameCharacter.CharacterData data = new GameCharacter.CharacterData();
            data.GetEquipments()[AEquipment.EquipTypeEnum.Belt] = new TestEquipment(
                new EquipmentAffix(EquipmentAffixType.MaxHp, 15f));

            GrowthSourceResult result = GameCharacter.CharacterData.GrowthCollectProvider(data);

            Assert.AreEqual(15f, result.Special.MaxHpFlat, 0.0001f);
            Assert.IsNotNull(AffixRuleService.RandomFloatProvider, "Install 应注入词条随机提供者");
        }

        /// <summary>AEquipment/ABackpackItem/AItem 均无抽象成员，最小子类即可实例化。</summary>
        private class TestEquipment : AEquipment
        {
            public TestEquipment(params EquipmentAffix[] affixes)
            {
                this.Affixes = new List<EquipmentAffix>(affixes);
            }
        }

        /// <summary>AWeapon 类部分无抽象成员（DoAttack 在 AWeaponObject），最小子类即可实例化。</summary>
        private class TestWeapon : AWeapon
        {
            public TestWeapon(params EquipmentAffix[] affixes)
            {
                this.Affixes = new List<EquipmentAffix>(affixes);
            }
        }
    }
}
