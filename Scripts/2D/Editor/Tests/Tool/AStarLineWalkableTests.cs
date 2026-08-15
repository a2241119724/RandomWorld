namespace LAB2D.Editor.Tests.Tool
{
    using System.Collections.Generic;
    using LAB2D.Core.Seek;
    using NUnit.Framework;

    /// <summary>
    /// AStar.IsLineWalkable 直线走查纯逻辑单测（回归"治本"修复 C）：
    /// 对角移动的角落检测由 &&（两角格都不可通才拒绝）改为 ||（任一不可通即拒绝），
    /// 禁止压缩路径斜穿墙角——"网格判可通、物理 Sprite 碰撞体挡"的卡死根因之一（见 bug-fixes.md）。
    /// </summary>
    [TestFixture]
    public class AStarLineWalkableTests
    {
        private static bool IsWalkable(HashSet<(int, int)> walls, int x, int y)
        {
            return !walls.Contains((x, y));
        }

        /// <summary>
        /// 修复前放行（&& 需 both 角格不可通）、修复后拒绝（|| 任一即可）的回归用例：
        /// 对角 (0,0)→(2,2) 的 Bresenham 角步经过角格 (1,0)，(1,0) 为墙 → 必须拒绝。
        /// </summary>
        [Test]
        public void IsLineWalkable_DiagonalCornerWall_Rejects()
        {
            var walls = new HashSet<(int, int)> { (1, 0) };
            Assert.IsFalse(AStar.IsLineWalkable(0, 0, 2, 2, (x, y) => IsWalkable(walls, x, y)));
        }

        /// <summary>
        /// 对称角格：墙在 (0,1)（另一个角格）同样拒绝（OR 语义的两侧都生效）。
        /// </summary>
        [Test]
        public void IsLineWalkable_OtherCornerWall_Rejects()
        {
            var walls = new HashSet<(int, int)> { (0, 1) };
            Assert.IsFalse(AStar.IsLineWalkable(0, 0, 2, 2, (x, y) => IsWalkable(walls, x, y)));
        }

        /// <summary>
        /// 开阔对角全可通 → 放行。
        /// </summary>
        [Test]
        public void IsLineWalkable_OpenDiagonal_Allows()
        {
            var walls = new HashSet<(int, int)>();
            Assert.IsTrue(AStar.IsLineWalkable(0, 0, 2, 2, (x, y) => IsWalkable(walls, x, y)));
        }

        /// <summary>
        /// 直线 (0,0)→(2,0) 穿过中间墙 (1,0) → 拒绝（Bresenham 逐格检查）。
        /// </summary>
        [Test]
        public void IsLineWalkable_StraightThroughWall_Rejects()
        {
            var walls = new HashSet<(int, int)> { (1, 0) };
            Assert.IsFalse(AStar.IsLineWalkable(0, 0, 2, 0, (x, y) => IsWalkable(walls, x, y)));
        }
    }
}
