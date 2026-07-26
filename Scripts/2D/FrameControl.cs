namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 控制每帧运行的最大时间.
    /// </summary>
    public class FrameControl : Singleton<FrameControl>
    {
        private const long MIN_FRAME = 120; // 最低帧率设置
        private float deltaTime;

        /// <summary>
        /// 到达当前帧最后的时间
        /// 超过一定时间退出当前帧,DateTime.Now.Ticks单位100纳秒.
        /// </summary>
        /// <param name="maxTime">一帧最大的时间间隔.</param>
        /// <returns>是否到达当前帧的最大时间,需要停止.</returns>
        public bool IsNeedStop(float maxTime = 1.0f / MIN_FRAME)
        {
            bool isStop = (Time.realtimeSinceStartup - this.deltaTime + Time.deltaTime) > maxTime;
            if (isStop)
            {
                this.deltaTime = Time.realtimeSinceStartup;
            }

            return isStop;
        }
    }
}
