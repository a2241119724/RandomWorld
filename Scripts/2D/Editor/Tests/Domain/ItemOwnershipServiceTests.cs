namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Worker;
    using NUnit.Framework;

    /// <summary>
    /// 物品所有权纯规则 — 无主可捡/本人可捡/他人不可捡/null 防御、
    /// 转移与置无主、标签解析（含自定义 OwnerNameProvider 注入）。
    /// </summary>
    [TestFixture]
    public class ItemOwnershipServiceTests
    {
        private static ResourceInfo Make(int ownerId)
        {
            return new ResourceInfo(1001) { OwnerId = ownerId };
        }

        [Test]
        public void NullResource_CannotPickUp()
        {
            Assert.IsFalse(ItemOwnershipService.CanPickUp(null, 0));
        }

        [Test]
        public void UnownedResource_AnyoneCanPickUp()
        {
            ResourceInfo resource = Make(ItemOwnershipService.UnownedId);

            Assert.IsTrue(ItemOwnershipService.CanPickUp(resource, ItemOwnershipService.PlayerOwnerId));
            Assert.IsTrue(ItemOwnershipService.CanPickUp(resource, 12345));
        }

        [Test]
        public void OwnedBySelf_CanPickUp()
        {
            ResourceInfo resource = Make(12345);

            Assert.IsTrue(ItemOwnershipService.CanPickUp(resource, 12345));
        }

        [Test]
        public void OwnedByOther_CannotPickUp()
        {
            ResourceInfo resource = Make(12345);

            Assert.IsFalse(ItemOwnershipService.CanPickUp(resource, 67890));
            // Player（0）也不能捡 Worker 的私有物
            Assert.IsFalse(ItemOwnershipService.CanPickUp(resource, ItemOwnershipService.PlayerOwnerId));
        }

        [Test]
        public void TransferOwnership_ChangesOwner()
        {
            ResourceInfo resource = Make(12345);

            ItemOwnershipService.TransferOwnership(resource, 67890);

            Assert.AreEqual(67890, resource.OwnerId);
        }

        [Test]
        public void TransferOwnership_NullResource_NoThrow()
        {
            Assert.DoesNotThrow(() => ItemOwnershipService.TransferOwnership(null, 1));
            Assert.DoesNotThrow(() => ItemOwnershipService.SetUnowned(null));
        }

        [Test]
        public void SetUnowned_ResetsToZero()
        {
            ResourceInfo resource = Make(12345);

            ItemOwnershipService.SetUnowned(resource);

            Assert.AreEqual(ItemOwnershipService.UnownedId, resource.OwnerId);
            Assert.IsTrue(ItemOwnershipService.CanPickUp(resource, 999));
        }

        [Test]
        public void GetOwnerLabel_NullResource_ReturnsWu()
        {
            Assert.AreEqual("无", ItemOwnershipService.GetOwnerLabel(null));
        }

        [Test]
        public void GetOwnerLabel_DefaultProvider_FormatsWorkerId()
        {
            Assert.AreEqual(
                $"Worker#{12345}",
                ItemOwnershipService.GetOwnerLabel(Make(12345)));
        }

        [Test]
        public void GetOwnerLabel_CustomProvider_IsUsed()
        {
            System.Func<int, string> original = ItemOwnershipService.OwnerNameProvider;
            try
            {
                ItemOwnershipService.OwnerNameProvider = (id) => $"测试#{id}";
                Assert.AreEqual("测试#7", ItemOwnershipService.GetOwnerLabel(Make(7)));
            }
            finally
            {
                ItemOwnershipService.OwnerNameProvider = original; // 静态状态还原，防污染其他测试
            }
        }
    }
}
