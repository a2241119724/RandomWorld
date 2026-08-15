namespace LAB2D.Editor.Tests.Tool
{
    using System.Collections.Generic;
    using LAB2D.Map;
    using NUnit.Framework;
    using UnityEngine;

    /// <summary>
    /// TileVisualSpawner 纯函数单测：
    /// Around 的 8 邻域坐标集合（RuleTile 刷新邻域范围）、无重复、半径扩展。
    /// </summary>
    [TestFixture]
    public class TileVisualPolicyTests
    {
        [Test]
        public void Around_Radius1_ReturnsCenterPlus8Neighbors()
        {
            // 3x3 邻域：中心 + 上下左右 + 4 对角
            Vector3Int[] around = TileVisualSpawner.Around(new Vector3Int(0, 0, 0), 1);
            Assert.AreEqual(9, around.Length);

            HashSet<Vector3Int> set = new HashSet<Vector3Int>(around);
            Assert.AreEqual(9, set.Count, "9 个坐标不应重复");

            // 中心在内
            Assert.Contains(new Vector3Int(0, 0, 0), around);
            // 4 方向与 4 对角都在
            Assert.Contains(new Vector3Int(1, 0, 0), around);
            Assert.Contains(new Vector3Int(-1, 0, 0), around);
            Assert.Contains(new Vector3Int(0, 1, 0), around);
            Assert.Contains(new Vector3Int(0, -1, 0), around);
            Assert.Contains(new Vector3Int(1, 1, 0), around);
            Assert.Contains(new Vector3Int(-1, -1, 0), around);
        }

        [Test]
        public void Around_Radius1_OffsetsFromCenter()
        {
            // 中心偏移到 (5,-3)，邻域坐标应随之平移
            Vector3Int[] around = TileVisualSpawner.Around(new Vector3Int(5, -3, 0), 1);
            Assert.Contains(new Vector3Int(5, -3, 0), around);
            Assert.Contains(new Vector3Int(6, -4, 0), around);
            Assert.Contains(new Vector3Int(4, -2, 0), around);
            Assert.Contains(new Vector3Int(6, -3, 0), around);
        }

        [Test]
        public void Around_Radius2_ExpandsTo25()
        {
            Vector3Int[] around = TileVisualSpawner.Around(new Vector3Int(0, 0, 0), 2);
            Assert.AreEqual(25, around.Length);

            HashSet<Vector3Int> set = new HashSet<Vector3Int>(around);
            Assert.AreEqual(25, set.Count, "25 个坐标不应重复");
            Assert.Contains(new Vector3Int(2, 2, 0), around);
            Assert.Contains(new Vector3Int(-2, -2, 0), around);
        }

        [Test]
        public void Around_Radius0_ReturnsCenterOnly()
        {
            Vector3Int[] around = TileVisualSpawner.Around(new Vector3Int(3, 4, 0), 0);
            Assert.AreEqual(1, around.Length);
            Assert.AreEqual(new Vector3Int(3, 4, 0), around[0]);
        }
    }
}
