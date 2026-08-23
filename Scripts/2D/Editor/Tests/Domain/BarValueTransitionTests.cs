namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.UI.Character;
    using NUnit.Framework;

    /// <summary>
    /// BarValueTransition 单元测试 — 验证过渡播放队列的吸附、逐段播放、去重、FIFO 顺序。
    /// 测试不构造 Slider，用本地 float + getter/setter 委托驱动。
    /// </summary>
    [TestFixture]
    public class BarValueTransitionTests
    {
        private const float Delta = 0.0001f;

        private float display;

        private BarValueTransition Create(float duration = 1f, float initial = 1f)
        {
            this.display = initial;
            return new BarValueTransition(() => this.display, value => this.display = value, duration);
        }

        [Test]
        public void SetTarget_FirstCall_SnapsWithoutAnimating()
        {
            BarValueTransition transition = this.Create(initial: 0f);
            transition.SetTarget(0.8f);
            Assert.AreEqual(0.8f, this.display, Delta, "首次调用应直接吸附，不产生动画");
        }

        [Test]
        public void Tick_AdvancesAndCompletesAtExactTarget()
        {
            BarValueTransition transition = this.Create();
            transition.SnapTo(1f);
            transition.SetTarget(0.5f);
            transition.Tick(1f);
            Assert.AreEqual(0.5f, this.display, Delta, "播满时长后应恰好停在目标值");
        }

        [Test]
        public void Tick_NoOvershoot_EndsExactlyAtTarget()
        {
            BarValueTransition transition = this.Create();
            transition.SnapTo(1f);
            transition.SetTarget(0.2f);
            for (int i = 0; i < 20; i++)
            {
                transition.Tick(0.1f);
            }

            Assert.AreEqual(0.2f, this.display, Delta, "分段推进不应过冲，终点应恰为目标值");
        }

        [Test]
        public void SetTarget_WhileAnimating_EnqueuesAndPlaysSequentially()
        {
            BarValueTransition transition = this.Create();
            transition.SnapTo(1f);
            transition.SetTarget(0.8f);
            transition.Tick(0.5f); // 第一段播一半
            transition.SetTarget(0.6f); // 入队
            transition.Tick(0.5f); // 第一段完成
            Assert.AreEqual(0.8f, this.display, Delta, "第一段应先完成");
            transition.Tick(1f); // 第二段完成
            Assert.AreEqual(0.6f, this.display, Delta, "第二段起点应为前段终点（段间连续）");
        }

        [Test]
        public void SetTarget_EqualToDisplayedValue_Skips()
        {
            BarValueTransition transition = this.Create();
            transition.SnapTo(0.8f);
            transition.SetTarget(0.8f);
            transition.Tick(1f);
            Assert.AreEqual(0.8f, this.display, Delta, "空闲且与显示值相等时应跳过");
        }

        [Test]
        public void SetTarget_EqualToCurrentSegmentTarget_Skips()
        {
            BarValueTransition transition = this.Create();
            transition.SnapTo(1f);
            transition.SetTarget(0.7f);
            transition.Tick(0.5f);
            transition.SetTarget(0.7f); // 重复当前段终点
            transition.Tick(5f);
            Assert.AreEqual(0.7f, this.display, Delta, "播放中重复当前段终点不应入队");
        }

        [Test]
        public void SetTarget_EqualToLastEnqueued_Skips()
        {
            BarValueTransition transition = this.Create();
            transition.SnapTo(1f);
            transition.SetTarget(0.8f);
            transition.SetTarget(0.7f); // 入队 0.7
            transition.SetTarget(0.7f); // 队尾重复 → 跳过
            transition.Tick(5f);
            transition.Tick(5f);
            Assert.AreEqual(0.7f, this.display, Delta, "重复队尾值不应重复入队");
        }

        [Test]
        public void SnapTo_ClearsPendingQueue_AndStops()
        {
            BarValueTransition transition = this.Create();
            transition.SnapTo(1f);
            transition.SetTarget(0.8f);
            transition.SetTarget(0.6f); // 排队
            transition.SnapTo(0.5f);
            Assert.AreEqual(0.5f, this.display, Delta, "SnapTo 应立即吸附");
            transition.Tick(10f);
            Assert.AreEqual(0.5f, this.display, Delta, "SnapTo 应清空队列并停止后续播放");
        }

        [Test]
        public void Tick_WhenIdle_DoesNothing()
        {
            BarValueTransition transition = this.Create(initial: 0.5f);
            transition.SnapTo(0.5f);
            transition.Tick(1f);
            Assert.AreEqual(0.5f, this.display, Delta, "空闲时 Tick 不应改变显示值");
        }

        [Test]
        public void ThreeRapidHits_PlayInFifoOrder_EndsAtFinal()
        {
            BarValueTransition transition = this.Create();
            transition.SnapTo(1f);
            transition.SetTarget(0.8f);
            transition.SetTarget(0.65f);
            transition.SetTarget(0.5f);
            transition.Tick(1f);
            Assert.AreEqual(0.8f, this.display, Delta, "第一段 1→0.8");
            transition.Tick(1f);
            Assert.AreEqual(0.65f, this.display, Delta, "第二段 0.8→0.65");
            transition.Tick(1f);
            Assert.AreEqual(0.5f, this.display, Delta, "第三段 0.65→0.5");
        }

        [Test]
        public void SetTarget_SameValueRepeatedNetworkPush_EnqueuesOnce()
        {
            BarValueTransition transition = this.Create();
            transition.SnapTo(1f);
            transition.SetTarget(0.8f);
            transition.SetTarget(0.7f); // 网络推值
            transition.SetTarget(0.7f); // 重复推相同值
            transition.SetTarget(0.7f);
            transition.Tick(5f);
            transition.Tick(5f);
            Assert.AreEqual(0.7f, this.display, Delta, "重复推相同值只应入队一次，最终停在 0.7");
        }

        [Test]
        public void DurationZero_SnapsImmediately()
        {
            BarValueTransition transition = this.Create(duration: 0f);
            transition.SnapTo(1f);
            transition.SetTarget(0.4f);
            Assert.AreEqual(0.4f, this.display, Delta, "duration<=0 时应直接吸附，不产生 NaN");
            transition.Tick(1f);
            Assert.AreEqual(0.4f, this.display, Delta, "吸附后 Tick 不应改变显示值");
        }
    }
}