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

		/// <summary>
		/// 是否正在关闭 — OnDestroy 后置为 true，阻止后台线程继续入队。
		/// </summary>
		public static bool IsShuttingDown { get; private set; }

		public void Awake()
		{
			Instance = this;
			IsShuttingDown = false;
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
		/// 应用退出 / GameObject 销毁时，清空待执行队列，设置关闭标志。
		/// 阻止后台线程（如 ASeek 寻路线程）在 Unity 对象销毁后
		/// 继续调用 Enqueue/EnqueueAsync 导致挂起。
		/// </summary>
		public void OnDestroy()
		{
			IsShuttingDown = true;
			Instance = null;

			lock (ExecutionQueue)
			{
				ExecutionQueue.Clear();
			}
		}

		/// <summary>
		/// 应用退出时最早触发，提前停止所有后台寻路线程。
		/// </summary>
		public void OnApplicationQuit()
		{
			IsShuttingDown = true;
			LAB2D.Core.Seek.ASeek.Shutdown();
		}

		/// <summary>
		/// 入队
		/// </summary>
		/// <param name="action">任务</param>
		public void Enqueue(IEnumerator action)
		{
			if (IsShuttingDown)
			{
				return;
			}

			lock (ExecutionQueue)
			{
				ExecutionQueue.Enqueue(() =>
				{
					if (!IsShuttingDown)
					{
						this.StartCoroutine(action);
					}
				});
			}
		}

		/// <summary>
		/// 入队
		/// </summary>
		/// <param name="action">任务</param>
		public void Enqueue(Action action)
		{
			if (IsShuttingDown)
			{
				return;
			}

			this.Enqueue(this.ActionWrapper(action));
		}

		/// <summary>
		/// 入队异步
		/// </summary>
		/// <param name="action">任务</param>
		/// <returns>执行任务</returns>
		public Task EnqueueAsync(Action action)
		{
			if (IsShuttingDown)
			{
				return Task.CompletedTask;
			}

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
