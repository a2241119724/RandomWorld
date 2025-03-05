using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class FrameControl : Singleton<FrameControl>
    {
        private float deltaTime;
        /// <summary>
        /// 最低帧率设置
        /// </summary>
        private const long MIN_FRAME = 120;

        /// <summary>
        /// 到达当前帧最后的时间
        /// 超过一定时间退出当前帧,DateTime.Now.Ticks单位100纳秒
        /// </summary>
        /// <returns></returns>
        public bool isNeedStop(long customFrame = MIN_FRAME)
        {
            bool isStop = Time.realtimeSinceStartup - deltaTime + Time.deltaTime > 1.0f / customFrame;
            if (isStop) 
            {
                deltaTime = Time.realtimeSinceStartup;
            }
            return isStop;
        }
    }
}
