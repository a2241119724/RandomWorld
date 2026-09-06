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
			// [GcDiag] 本 Update 内 mono 堆增量采样（定位 Profiler「UnityMainThreadDispatcher.Update
			// GC 耗时」热点用——直接 Invoke 后 action 内部分配全归本行递归合计，采样给出队列
			// action 的真实分配量与 action 数，指认后删除）
			long gcUpdateStart = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
			int gcActionCount = 0;

			lock (ExecutionQueue)
			{
				while (ExecutionQueue.Count > 0)
				{
					// 单项异常隔离：原协程版异常只停该协程不打断队列，直接 Invoke 后
					// 需显式 catch 对齐语义（否则一个 action 抛异常会中断本帧剩余队列）。
					try
					{
						ExecutionQueue.Dequeue().Invoke();
						gcActionCount++;
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}
				}
			}

			// [GcDiag] 结算（见方法开头采样注释）：5s 窗口汇总 + 单帧 max（附该帧 action 数）。
			long gcUpdateDelta = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() - gcUpdateStart;
			if (gcUpdateDelta > 0)
			{
				this.gcWindowBytes += gcUpdateDelta;
				if (gcUpdateDelta > this.gcMaxFrameBytes)
				{
					this.gcMaxFrameBytes = gcUpdateDelta;
					this.gcMaxFrameActions = gcActionCount;
				}
			}

			this.gcReportTimer += Time.deltaTime;
			if (this.gcReportTimer >= 5f)
			{
				this.gcReportTimer -= 5f;
				if (this.gcWindowBytes > 10240)
				{
					LAB2D.Character.Worker.Task.AWorkerTask.LogProvider(
						$"[GcDiag] dispatcher 5s窗口action内分配≈{this.gcWindowBytes / 1024}KB 单帧max={this.gcMaxFrameBytes}B(max帧action数={this.gcMaxFrameActions}) frame={Time.frameCount}",
						LAB2D.Manager.LogManager.LogLevelEnum.Debug);
				}

				this.gcWindowBytes = 0;
				this.gcMaxFrameBytes = 0;
				this.gcMaxFrameActions = 0;
			}
		}

		// ---- [GcDiag] dispatcher Update 采样字段（见 Update 内注释，排查完成后整体删除）----
		private float gcReportTimer;
		private long gcWindowBytes;
		private long gcMaxFrameBytes;
		private int gcMaxFrameActions;

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

			// 直接入队由 Update 执行：原实现 ActionWrapper 协程化（迭代器状态机 +
			// 入队闭包 + StartCoroutine native 分配，每 action ~600B+）只为把 action
			// 搬到主线程 Update 栈内执行——直接入队执行时机完全一致（本帧 Update
			// dequeue 即 Invoke，协程版 StartCoroutine 首段也是同步执行），白付三份
			// 分配。ASeek.FlushPendingEnqueues 每帧入队一次（PendingEnqueue 自续），
			// 100 Worker 场景 Profiler 中 ActionWrapper.MoveNext 成为可见 GC 源
			/// （2026-09-06）。方法组静态委托由编译器缓存，热路径零分配。
			lock (ExecutionQueue)
			{
				ExecutionQueue.Enqueue(action);
			}
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

			// 同 Enqueue(Action)：直接入队，不走协程（见该方法的注释）。
			lock (ExecutionQueue)
			{
				ExecutionQueue.Enqueue(WrappedAction);
			}

			return tcs.Task;
		}
	}
}
