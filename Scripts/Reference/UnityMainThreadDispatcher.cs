namespace PimDeWitte.UnityMainThreadDispatcher
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    /// 使用主线程执行任务,不能在主线程中使用
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
	{
		private static readonly Queue<Action> ExecutionQueue = new ();

		/// <summary>
		/// 单例
		/// </summary>
		public static UnityMainThreadDispatcher Instance { get; set; }

		public void Awake()
		{
			Instance = this;
		}

		public void Update()
		{
			lock (ExecutionQueue)
			{
				while (ExecutionQueue.Count > 0)
				{
					ExecutionQueue.Dequeue().Invoke();
				}
			}
		}

		/// <summary>
		/// 入队
		/// </summary>
		/// <param name="action">任务</param>
		public void Enqueue(IEnumerator action)
		{
			lock (ExecutionQueue)
			{
				ExecutionQueue.Enqueue(() =>
				{
					this.StartCoroutine(action);
				});
			}
		}

		/// <summary>
		/// 入队
		/// </summary>
		/// <param name="action">任务</param>
		public void Enqueue(Action action)
		{
			this.Enqueue(this.ActionWrapper(action));
		}

		/// <summary>
		/// 入队异步
		/// </summary>
		/// <param name="action">任务</param>
		/// <returns>执行任务</returns>
		public Task EnqueueAsync(Action action)
		{
			var tcs = new TaskCompletionSource<bool>();

			void WrappedAction()
			{
				try
				{
					action();
					tcs.TrySetResult(true);
				}
				catch (Exception ex)
				{
					tcs.TrySetException(ex);
				}
			}

			this.Enqueue(this.ActionWrapper(WrappedAction));
			return tcs.Task;
		}

		private IEnumerator ActionWrapper(Action a)
		{
			a();
			yield return null;
		}
	}
}