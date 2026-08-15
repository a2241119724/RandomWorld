namespace LAB2D.Editor.Tests.Tool
{
    using LAB2D.Core.Seek;
    using NUnit.Framework;
    using UnityEngine;

    /// <summary>
    /// ASeek.TryGetSlideDirection 纯逻辑单测（碰撞预检测的切向投影）：
    /// 正对墙 head-on、贴墙平行走、45° 墙斜向投影、接近正对仍判 head-on。
    /// 该函数决定角色撞墙是"停下（head-on）"还是"沿墙滑动"，误判会导致
    /// 平滑绕角失效或正对墙不停。阈值 |slide|²&lt;0.15 见 ASeek.HeadOnSlideSqr。
    /// </summary>
    [TestFixture]
    public class ASeekSlideDirectionTests
    {
        /// <summary>
        /// 正对墙：移动方向与墙面法线完全相反 → 切向投影为 0 → 判定 head-on（返回 false）。
        /// </summary>
        [Test]
        public void TryGetSlideDirection_HeadOn_ReturnsFalse()
        {
            Assert.IsFalse(ASeek.TryGetSlideDirection(new Vector2(1f, 0f), new Vector2(1f, 0f), out _));
            Assert.IsFalse(ASeek.TryGetSlideDirection(new Vector2(0f, 1f), new Vector2(0f, 1f), out _));
            Assert.IsFalse(ASeek.TryGetSlideDirection(new Vector2(-1f, 0f), new Vector2(-1f, 0f), out _));
        }

        /// <summary>
        /// 贴墙平行走：移动方向与墙切向重合（法线 dot≈0）→ 投影≈原方向，不误判。
        /// </summary>
        [Test]
        public void TryGetSlideDirection_ParallelSlide_KeepsOriginalDirection()
        {
            Vector2 slide;
            Assert.IsTrue(ASeek.TryGetSlideDirection(new Vector2(1f, 0f), new Vector2(0f, 1f), out slide));
            Assert.AreEqual(1f, slide.x, 0.001f);
            Assert.AreEqual(0f, slide.y, 0.001f);
        }

        /// <summary>
        /// 斜向撞墙（45° 墙）：投影方向应与墙面切向共线（dot=±1）。
        /// </summary>
        [Test]
        public void TryGetSlideDirection_DiagonalWall_ProjectsToWallTangent()
        {
            Vector2 moveDir = new Vector2(1f, 0f);
            Vector2 wallNormal = new Vector2(0.7071f, 0.7071f); // 45° 墙面法线
            Vector2 slide;
            Assert.IsTrue(ASeek.TryGetSlideDirection(moveDir, wallNormal, out slide));

            // 切向投影应与切向共线（可同向或反向，都算沿墙）。
            Vector2 tangent = new Vector2(-wallNormal.y, wallNormal.x);
            float dot = Vector2.Dot(slide.normalized, tangent.normalized);
            Assert.AreEqual(1f, Mathf.Abs(dot), 0.001f);
        }

        /// <summary>
        /// 接近正对（与法线夹角约 11.5°，|slide|²=0.04 &lt; 0.15）→ 仍判 head-on 停下。
        /// 保证角色不是几乎正对墙时还被投影滑动、硬蹭过墙。
        /// </summary>
        [Test]
        public void TryGetSlideDirection_NearHeadOn_ReturnsFalse()
        {
            Vector2 moveDir = new Vector2(0.98f, 0.2f);
            Vector2 wallNormal = new Vector2(1f, 0f);
            Assert.IsFalse(ASeek.TryGetSlideDirection(moveDir, wallNormal, out _));
        }
    }
}
