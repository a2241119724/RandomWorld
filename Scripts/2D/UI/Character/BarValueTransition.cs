namespace LAB2D.UI.Character
{
    using System;
    using System.Collections.Generic;
    using LAB2D.Domain.Common;

    /// <summary>
    /// Slider 数值过渡播放队列 — 目标值入队，一次播放一段过渡，
    /// 一段播完才取下一条，避免同一时刻多次变化互相覆盖。
    /// 纯 C# 逻辑类，不引用 UnityEngine，便于编辑模式单测。
    /// 通过 getter/setter 委托抽象 Slider，宿主在 Update() 里驱动 Tick。
    /// </summary>
    public sealed class BarValueTransition
    {
        private const float Epsilon = 1e-4f;

        private readonly Func<float> getValue;
        private readonly Action<float> setValue;
        private readonly float duration;
        private readonly Func<float, float> easing;

        private readonly Queue<float> pending = new Queue<float>();

        private float from;
        private float to;
        private float elapsed;
        private float lastQueued = float.NaN;
        private bool animating;
        private bool initialized;

        /// <param name="getValue">读取当前显示值（如 slider.value）</param>
        /// <param name="setValue">写入显示值</param>
        /// <param name="duration">单段过渡时长（秒），&lt;=0 时直接吸附</param>
        /// <param name="easing">缓动函数，默认 SmoothStep</param>
        public BarValueTransition(
            Func<float> getValue,
            Action<float> setValue,
            float duration = 0.3f,
            Func<float, float> easing = null)
        {
            this.getValue = getValue;
            this.setValue = setValue;
            this.duration = duration > 0f ? duration : 0f;
            this.easing = easing ?? DefaultEasing;
        }

        /// <summary>
        /// 设置目标值（0~1 归一化）。
        /// 首次调用直接吸附不动画（防出生从 0 拉满）；播放中则入队按 FIFO 顺序播放。
        /// </summary>
        public void SetTarget(float target)
        {
            target = MathHelper.Clamp01(target);

            if (this.duration <= 0f)
            {
                this.SnapTo(target);
                return;
            }

            if (!this.initialized)
            {
                this.setValue(target);
                this.initialized = true;
                return;
            }

            if (this.animating)
            {
                // 播放中只对"当前段终点 / 队尾"去重；不能拿实时显示值比，
                // 否则插值途中的瞬时值会误吞真实变化造成条与实际值失步。
                if (Approximately(target, this.to))
                {
                    return;
                }

                if (this.pending.Count > 0 && Approximately(target, this.lastQueued))
                {
                    return;
                }

                this.pending.Enqueue(target);
                this.lastQueued = target;
            }
            else
            {
                if (Approximately(target, this.getValue()))
                {
                    return;
                }

                this.StartSegment(this.getValue(), target);
            }
        }

        /// <summary>
        /// 立即吸附到目标值，清空队列并停止动画（用于初始化/读档/重生重置）。
        /// </summary>
        public void SnapTo(float target)
        {
            target = MathHelper.Clamp01(target);
            this.setValue(target);
            this.animating = false;
            this.pending.Clear();
            this.lastQueued = float.NaN;
            this.initialized = true;
        }

        /// <summary>
        /// 每帧推进过渡；当前段播完后若队列非空则取下一段继续播放。
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!this.animating)
            {
                return;
            }

            this.elapsed += deltaTime;
            float t = this.duration > 0f ? this.elapsed / this.duration : 1f;

            if (t >= 1f)
            {
                this.setValue(this.to);
                this.animating = false;

                if (this.pending.Count > 0)
                {
                    // 以当前显示值（== 刚播完的 to）为起点启动下一段，保证段间连续不跳变。
                    // 队尾 lastQueued 不变（移除的是队头），仅队列清空时重置。
                    this.StartSegment(this.to, this.pending.Dequeue());
                }
                else
                {
                    this.lastQueued = float.NaN;
                }
            }
            else
            {
                this.setValue(this.from + (this.to - this.from) * this.easing(t));
            }
        }

        private void StartSegment(float from, float to)
        {
            this.from = from;
            this.to = to;
            this.elapsed = 0f;
            this.animating = true;
        }

        private static float DefaultEasing(float t)
        {
            return t * t * (3f - 2f * t); // SmoothStep
        }

        private static bool Approximately(float a, float b)
        {
            float diff = a - b;
            return diff < Epsilon && diff > -Epsilon;
        }
    }
}
