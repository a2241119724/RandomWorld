namespace LAB2D.Editor.Tests.Tool
{
    using LAB2D.Map;
    using NUnit.Framework;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// BuildMap.ColliderFor 统一碰撞体不变量纯函数单测（回归"治本"修复 B）：
    /// 未完成→None；完成+IsPass→None；完成+阻挡→Sprite。
    /// 所有写路径经此不变量设置碰撞体，保证 GetColliderType==None 即网格可通行==物理可通行。
    /// </summary>
    [TestFixture]
    public class BuildColliderInvariantTests
    {
        [Test]
        public void ColliderFor_NotComplete_IsNone()
        {
            Assert.AreEqual(Tile.ColliderType.None, BuildMap.ColliderFor(false, true));
            Assert.AreEqual(Tile.ColliderType.None, BuildMap.ColliderFor(false, false));
        }

        [Test]
        public void ColliderFor_CompleteAndPass_IsNone()
        {
            Assert.AreEqual(Tile.ColliderType.None, BuildMap.ColliderFor(true, true));
        }

        [Test]
        public void ColliderFor_CompleteAndBlocking_IsSprite()
        {
            Assert.AreEqual(Tile.ColliderType.Sprite, BuildMap.ColliderFor(true, false));
        }
    }
}
