namespace LAB2D.Editor.Tests.Tool
{
    using System.Collections.Generic;
    using LAB2D.Render;
    using NUnit.Framework;

    /// <summary>
    /// SpriteFrameAnimator.CollectFrames 纯函数单测：
    /// 按 {prefix}_0/_1/... 收集帧序列、首个缺失即停、上限兜底、命名拼接格式。
    /// </summary>
    [TestFixture]
    public class SpriteFrameAnimatorTests
    {
        /// <summary>
        /// 用 string 代替 Sprite 构造加载器：字典存在返回名，缺失返回 null。
        /// </summary>
        private static System.Func<string, string> MakeLoader(params string[] names)
        {
            HashSet<string> set = new HashSet<string>(names);
            return name => set.Contains(name) ? name : null;
        }

        [Test]
        public void CollectFrames_ContinuousSequence_CollectsAll()
        {
            // Torch_0/Torch_1/Torch_2 连续存在 → 收集 3 帧
            string[] frames = SpriteFrameAnimator.CollectFrames(
                MakeLoader("Torch_0", "Torch_1", "Torch_2"), "Torch");
            Assert.AreEqual(3, frames.Length);
            Assert.AreEqual("Torch_0", frames[0]);
            Assert.AreEqual("Torch_1", frames[1]);
            Assert.AreEqual("Torch_2", frames[2]);
        }

        [Test]
        public void CollectFrames_GapAtSecond_StopsAtFirstMissing()
        {
            // 只有 Torch_0/Torch_1，Torch_2 缺失 → 收集 2 帧，不继续探测 Torch_3
            string[] frames = SpriteFrameAnimator.CollectFrames(
                MakeLoader("Torch_0", "Torch_1"), "Torch");
            Assert.AreEqual(2, frames.Length);
            Assert.AreEqual("Torch_0", frames[0]);
            Assert.AreEqual("Torch_1", frames[1]);
        }

        [Test]
        public void CollectFrames_NoFrames_ReturnsEmpty()
        {
            // 前缀对应任何 _N 帧都不存在 → 空数组（调用方回退静态 tile 图）
            string[] frames = SpriteFrameAnimator.CollectFrames(MakeLoader(), "Torch");
            Assert.IsEmpty(frames);
        }

        [Test]
        public void CollectFrames_AlwaysPresent_RespectsMaxFrames()
        {
            // 加载器永不返回 null → 上限兜底，不无限循环
            string[] frames = SpriteFrameAnimator.CollectFrames(name => name, "Torch", 5);
            Assert.AreEqual(5, frames.Length);
            Assert.AreEqual("Torch_4", frames[4]);
        }

        [Test]
        public void CollectFrames_IndexesFromZero_WithUnderscore()
        {
            // 命名格式为 {prefix}_{index}，从 0 起
            string[] frames = SpriteFrameAnimator.CollectFrames(
                MakeLoader("Campfire_0"), "Campfire");
            Assert.AreEqual(1, frames.Length);
            Assert.AreEqual("Campfire_0", frames[0]);
        }
    }
}
