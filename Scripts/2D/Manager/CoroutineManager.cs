namespace LAB2D.Manager
{
    using LAB2D;
    using System;
    using System.Collections;
    using UnityEngine;

    /// <summary>
    /// 没用.
    /// </summary>
    public class CoroutineManager : MonoBehaviour
    {
        /// <summary>
        /// 初始化 <see cref="CoroutineManager"/> 类的新实例。
        /// </summary>
        /// <param name="maxCount">协程最大数量。</param>
        public CoroutineManager(int maxCount)
        {
        }

        /// <summary>
        /// 单例.
        /// </summary>
        public static CoroutineManager Instance { get; private set; }

        /// <summary>
        /// 开启协程.
        /// </summary>
        /// <param name="coroutineDelegate">携程执行的任务.</param>
        public void StartCoroutine(Func<IEnumerator> coroutineDelegate)
        {
            this.StartCoroutine(coroutineDelegate());
        }

        public void Awake()
        {
            Instance = this;
        }
    }
}
