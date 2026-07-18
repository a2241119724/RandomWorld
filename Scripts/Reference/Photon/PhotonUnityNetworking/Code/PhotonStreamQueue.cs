// ----------------------------------------------------------------------------
// <copyright file="PhotonStreamQueue.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>
// 包含PhotonStreamQueue。
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------


namespace Photon.Pun
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// PhotonStreamQueue帮助以比PhotonNetwork.SendRate规定的频率更高的频率
    /// 轮询对象状态，然后在调用Serialize()时一次性发送所有这些状态。
    /// 在接收端，你可以调用Deserialize()，然后流将按照记录的相同顺序
    /// 和时间步长输出接收到的对象状态。
    /// </summary>
    public class PhotonStreamQueue
    {
        private int m_SampleRate;
        private int m_SampleCount;
        private int m_ObjectsPerSample = -1;

        private float m_LastSampleTime = -Mathf.Infinity;
        private int m_LastFrameCount = -1;
        private int m_NextObjectIndex = -1;

        private List<object> m_Objects = new List<object>();

        private bool m_IsWriting;

        /// <summary>
        /// 初始化 <see cref="PhotonStreamQueue"/> 类的新实例。
        /// </summary>
        /// <param name="sampleRate">每秒采样对象状态的次数</param>
        public PhotonStreamQueue(int sampleRate)
        {
            this.m_SampleRate = sampleRate;
        }

        private void BeginWritePackage()
        {
            //如果自上次采样以来没有经过足够的时间，我们不想写入任何内容
            if (Time.realtimeSinceStartup < this.m_LastSampleTime + 1f / this.m_SampleRate)
            {
                this.m_IsWriting = false;
                return;
            }

            if (this.m_SampleCount == 1)
            {
                this.m_ObjectsPerSample = this.m_Objects.Count;
                //Debug.Log( "Setting m_ObjectsPerSample to " + m_ObjectsPerSample );
            }
            else if (this.m_SampleCount > 1)
            {
                if (this.m_Objects.Count / this.m_SampleCount != this.m_ObjectsPerSample)
                {
                    Debug.LogWarning("The number of objects sent via a PhotonStreamQueue has to be the same each frame");
                    Debug.LogWarning("Objects in List: " + this.m_Objects.Count + " / Sample Count: " + this.m_SampleCount + " = " + this.m_Objects.Count / this.m_SampleCount + " != " + this.m_ObjectsPerSample);
                }
            }

            this.m_IsWriting = true;
            this.m_SampleCount++;
            this.m_LastSampleTime = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// 重置PhotonStreamQueue。当你观察的对象数量发生变化时需要进行此操作。
        /// </summary>
        public void Reset()
        {
            this.m_SampleCount = 0;
            this.m_ObjectsPerSample = -1;

            this.m_LastSampleTime = -Mathf.Infinity;
            this.m_LastFrameCount = -1;

            this.m_Objects.Clear();
        }

        /// <summary>
        /// 将下一个对象添加到队列中。这与PhotonStream.SendNext的工作方式相同。
        /// </summary>
        /// <param name="obj">你想要添加到队列中的对象</param>
        public void SendNext(object obj)
        {
            if (Time.frameCount != this.m_LastFrameCount)
            {
                this.BeginWritePackage();
            }

            this.m_LastFrameCount = Time.frameCount;

            if (this.m_IsWriting == false)
            {
                return;
            }

            this.m_Objects.Add(obj);
        }

        /// <summary>
        /// 判断队列中是否有存储的对象。
        /// </summary>
        public bool HasQueuedObjects()
        {
            return this.m_NextObjectIndex != -1;
        }

        /// <summary>
        /// 从队列中接收下一个对象。这与PhotonStream.ReceiveNext的工作方式相同。
        /// </summary>
        /// <returns></returns>
        public object ReceiveNext()
        {
            if (this.m_NextObjectIndex == -1)
            {
                return null;
            }

            if (this.m_NextObjectIndex >= this.m_Objects.Count)
            {
                this.m_NextObjectIndex -= this.m_ObjectsPerSample;
            }

            return this.m_Objects[this.m_NextObjectIndex++];
        }

        /// <summary>
        /// 序列化指定的流。在你的OnPhotonSerializeView方法中调用此方法以发送整个记录的流。
        /// </summary>
        /// <param name="stream">在OnPhotonSerializeView中作为参数接收的PhotonStream</param>
        public void Serialize(PhotonStream stream)
        {
            // TODO: 为这个问题找到更好的解决方案：
            // 这个"if"是对每帧只有1个采样的数据包的临时方案。在这种情况下，SendNext没有设置每个采样的对象数。
            if (this.m_Objects.Count > 0 && this.m_ObjectsPerSample < 0)
            {
                this.m_ObjectsPerSample = this.m_Objects.Count;
            }

            stream.SendNext(this.m_SampleCount);
            stream.SendNext(this.m_ObjectsPerSample);

            for (int i = 0; i < this.m_Objects.Count; ++i)
            {
                stream.SendNext(this.m_Objects[i]);
            }

            this.m_Objects.Clear();
            this.m_SampleCount = 0;
        }

        /// <summary>
        /// 反序列化指定的流。在你的OnPhotonSerializeView方法中调用此方法以接收整个记录的流。
        /// </summary>
        /// <param name="stream">在OnPhotonSerializeView中作为参数接收的PhotonStream</param>
        public void Deserialize(PhotonStream stream)
        {
            this.m_Objects.Clear();

            this.m_SampleCount = (int)stream.ReceiveNext();
            this.m_ObjectsPerSample = (int)stream.ReceiveNext();

            for (int i = 0; i < this.m_SampleCount * this.m_ObjectsPerSample; ++i)
            {
                this.m_Objects.Add(stream.ReceiveNext());
            }

            if (this.m_Objects.Count > 0)
            {
                this.m_NextObjectIndex = 0;
            }
            else
            {
                this.m_NextObjectIndex = -1;
            }
        }
    }
}