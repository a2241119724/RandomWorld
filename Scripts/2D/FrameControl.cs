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
        /// ���֡������
        /// </summary>
        private const long MIN_FRAME = 120;

        /// <summary>
        /// ���ﵱǰ֡����ʱ��
        /// ����һ��ʱ���˳���ǰ֡,DateTime.Now.Ticks��λ100����
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
