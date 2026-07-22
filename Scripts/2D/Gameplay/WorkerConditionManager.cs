namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Enum;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// 工人饥饿与疲劳状态管理器。
    /// 负责在运行时汇总 Worker 状态、派发状态变化事件、提供移动与工作效率倍率，并显示低状态提示。
    /// 本类不修改存档结构，不写入资源，不参与 Photon 同步。
    /// </summary>
    public class WorkerConditionManager : Singleton<WorkerConditionManager>, IWorkerConditionManager
    {
        private readonly Dictionary<int, WorkerConditionSnapshot> snapshots;
        private readonly Dictionary<int, float> lastTipTimes;
        private bool enabled = true;
        private bool tipEnabled = true;

        public WorkerConditionManager()
        {
            this.snapshots = new Dictionary<int, WorkerConditionSnapshot>();
            this.lastTipTimes = new Dictionary<int, float>();
        }

        /// <summary>
        /// Worker 状态变化事件。
        /// HUD 或其他表现层可订阅此事件刷新显示。
        /// </summary>
        public event Action<AWorker, WorkerConditionSnapshot> OnWorkerConditionChanged;

        /// <summary>
        /// Worker 状态提示请求事件。
        /// 外部可订阅此事件接管提示展示方式。
        /// </summary>
        public event Action<string> OnWorkerConditionTipRequested;

        /// <summary>
        /// 工人状态效果是否启用。
        /// 关闭后移动与工作倍率均回到 1，但 HUD 仍可显示原始状态。
        /// </summary>
        public bool IsEnabled
        {
            get { return this.enabled; }
        }

        /// <summary>
        /// 启用工人状态效果。
        /// </summary>
        public void Enable()
        {
            this.enabled = true;
        }

        /// <summary>
        /// 禁用工人状态效果，移动与工作倍率回到 1。
        /// </summary>
        public void Disable()
        {
            this.enabled = false;
        }

        /// <summary>
        /// 设置是否显示工人状态提示。
        /// </summary>
        /// <param name="enabledTip">是否显示 Tip 提示。</param>
        public void SetTipEnabled(bool enabledTip)
        {
            this.tipEnabled = enabledTip;
        }

        /// <summary>
        /// 刷新单个 Worker 的状态快照。
        /// </summary>
        /// <param name="worker">目标工人。</param>
        public void UpdateWorkerCondition(AWorker worker)
        {
            if (!WorkerConditionTool.TryGetWorkerData(worker, out AWorker.WorkerData workerData))
            {
                return;
            }

            WorkerConditionSnapshot snapshot = WorkerConditionSnapshot.FromWorker(worker, workerData);
            bool changed = !this.snapshots.TryGetValue(snapshot.WorkerInstanceId, out WorkerConditionSnapshot previous) ||
                previous.State != snapshot.State;

            this.snapshots[snapshot.WorkerInstanceId] = snapshot;
            if (!changed)
            {
                return;
            }

            this.OnWorkerConditionChanged?.Invoke(worker, snapshot);
            this.TryShowConditionTip(snapshot, previous);
        }

        /// <summary>
        /// 获取 Worker 当前状态快照。
        /// </summary>
        /// <param name="worker">目标工人。</param>
        /// <returns>状态快照，无法读取时返回 null。</returns>
        public WorkerConditionSnapshot GetWorkerCondition(AWorker worker)
        {
            if (!WorkerConditionTool.TryGetWorkerData(worker, out AWorker.WorkerData workerData))
            {
                return null;
            }

            int instanceId = worker.GetInstanceID();
            WorkerConditionSnapshot snapshot = WorkerConditionSnapshot.FromWorker(worker, workerData);
            this.snapshots[instanceId] = snapshot;
            return snapshot;
        }

        /// <summary>
        /// 获取 Worker 当前移动速度倍率。
        /// </summary>
        /// <param name="worker">目标工人。</param>
        /// <returns>移动速度倍率，禁用或无法读取时返回 1。</returns>
        public float GetWorkerMoveSpeedMultiplier(AWorker worker)
        {
            if (!this.enabled)
            {
                return 1.0f;
            }

            WorkerConditionSnapshot snapshot = this.GetWorkerCondition(worker);
            return snapshot == null ? 1.0f : snapshot.MoveSpeedMultiplier;
        }

        /// <summary>
        /// 获取套用工人状态后的移动速度。
        /// </summary>
        /// <param name="worker">目标工人。</param>
        /// <param name="baseSpeed">基础移动速度。</param>
        /// <returns>套用状态倍率后的安全速度。</returns>
        public float GetAdjustedWorkerMoveSpeed(AWorker worker, float baseSpeed)
        {
            return WeatherGameplayTool.ApplyMultiplier(
                baseSpeed,
                this.GetWorkerMoveSpeedMultiplier(worker),
                0.0f);
        }

        /// <summary>
        /// 获取 Worker 当前任务进度倍率。
        /// </summary>
        /// <param name="worker">目标工人。</param>
        /// <param name="taskType">任务类型。</param>
        /// <returns>任务进度倍率，禁用或无法读取时返回 1。</returns>
        public float GetWorkerTaskProgressMultiplier(AWorker worker, WorkerTaskType taskType)
        {
            if (!this.enabled)
            {
                return 1.0f;
            }

            WorkerConditionSnapshot snapshot = this.GetWorkerCondition(worker);
            return snapshot == null
                ? 1.0f
                : WorkerConditionTool.GetTaskProgressMultiplier(snapshot.State, taskType);
        }

        /// <summary>
        /// 构建所有 Worker 的状态摘要。
        /// </summary>
        /// <returns>适合 HUD 和 Editor 菜单展示的多行文本。</returns>
        public string BuildSummaryText()
        {
            StringBuilder builder = new StringBuilder(1024);
            builder.AppendLine(this.enabled ? "工人状态效果: 已启用" : "工人状态效果: 已禁用");

            try
            {
                if (WorkerManager.Instance == null || WorkerManager.Instance.Characters == null ||
                    WorkerManager.Instance.Characters.Count == 0)
                {
                    builder.Append(WorkerConditionConstant.EmptyHudText);
                    return builder.ToString();
                }

                List<AWorker> workers = WorkerManager.Instance.Characters;
                for (int i = 0; i < workers.Count; i++)
                {
                    AWorker worker = workers[i];
                    WorkerConditionSnapshot snapshot = this.GetWorkerCondition(worker);
                    if (snapshot == null)
                    {
                        continue;
                    }

                    builder.AppendLine(snapshot.ToDisplayLine());
                }
            }
            catch (Exception exception)
            {
                builder.Append("工人状态暂不可用: ").Append(exception.Message);
            }

            return builder.ToString();
        }

        /// <summary>
        /// 状态变化时显示 Tip。
        /// </summary>
        /// <param name="snapshot">当前状态快照。</param>
        /// <param name="previous">上一帧缓存状态。</param>
        private void TryShowConditionTip(WorkerConditionSnapshot snapshot, WorkerConditionSnapshot previous)
        {
            if (!this.tipEnabled || snapshot == null)
            {
                return;
            }

            bool recovered = snapshot.State == WorkerConditionState.Healthy &&
                previous != null &&
                previous.State != WorkerConditionState.Healthy;
            if (!recovered && snapshot.State == WorkerConditionState.Healthy)
            {
                return;
            }

            float now = Time.time;
            if (!recovered &&
                this.lastTipTimes.TryGetValue(snapshot.WorkerInstanceId, out float lastTipTime) &&
                now - lastTipTime < WorkerConditionConstant.TipCooldownSeconds)
            {
                return;
            }

            this.lastTipTimes[snapshot.WorkerInstanceId] = now;
            string message = WorkerConditionTool.BuildTipText(
                snapshot.WorkerName,
                snapshot.State,
                snapshot.MoveSpeedMultiplier,
                snapshot.WorkProgressMultiplier);
            this.ShowTip(message);
        }

        /// <summary>
        /// 显示状态提示。
        /// 优先使用现有 Tip UI，不可用时降级为日志。
        /// </summary>
        /// <param name="message">提示内容。</param>
        private void ShowTip(string message)
        {
            this.OnWorkerConditionTipRequested?.Invoke(message);

            try
            {
                AWorkerTask.ShowTipProvider(message);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[WorkerCondition] 显示 Tip 失败: " + exception.Message);
            }

            Debug.Log("[工人状态] " + message);
        }
    }

    /// <summary>
    /// 工人饥饿与疲劳状态快照。
    /// 由 WorkerConditionManager 维护，供 HUD、Editor 菜单和其他业务只读查询。
    /// </summary>
    [Serializable]
    public class WorkerConditionSnapshot
    {
        /// <summary>工人名称。</summary>
        public string WorkerName;

        /// <summary>工人运行时实例 ID。</summary>
        public int WorkerInstanceId;

        /// <summary>工人生存状态。</summary>
        public WorkerConditionState State;

        /// <summary>当前饥饿值比例。</summary>
        public float HungryRatio;

        /// <summary>当前疲劳值比例。</summary>
        public float TiredRatio;

        /// <summary>移动速度倍率。</summary>
        public float MoveSpeedMultiplier;

        /// <summary>普通任务进度倍率。</summary>
        public float WorkProgressMultiplier;

        /// <summary>
        /// 从 Worker 数据创建状态快照。
        /// </summary>
        /// <param name="worker">目标工人。</param>
        /// <param name="workerData">工人数据。</param>
        /// <returns>状态快照。</returns>
        public static WorkerConditionSnapshot FromWorker(AWorker worker, AWorker.WorkerData workerData)
        {
            if (workerData == null)
            {
                return new WorkerConditionSnapshot
                {
                    WorkerName = worker == null ? "未知工人" : worker.name,
                    WorkerInstanceId = worker == null ? 0 : worker.GetInstanceID(),
                    State = WorkerConditionState.Healthy,
                    HungryRatio = 1.0f,
                    TiredRatio = 1.0f,
                    MoveSpeedMultiplier = 1.0f,
                    WorkProgressMultiplier = 1.0f,
                };
            }

            WorkerConditionState state = WorkerConditionTool.GetState(workerData);
            return new WorkerConditionSnapshot
            {
                WorkerName = worker == null ? "未知工人" : worker.name,
                WorkerInstanceId = worker == null ? 0 : worker.GetInstanceID(),
                State = state,
                HungryRatio = WorkerConditionTool.GetSafeRatio(workerData.CurHungry, workerData.MaxHungry),
                TiredRatio = WorkerConditionTool.GetSafeRatio(workerData.CurTired, workerData.MaxTired),
                MoveSpeedMultiplier = WorkerConditionTool.GetMoveSpeedMultiplier(state),
                WorkProgressMultiplier = WorkerConditionTool.GetTaskProgressMultiplier(
                    state,
                    WorkerTaskType.Build),
            };
        }

        /// <summary>
        /// 生成 HUD 展示行。
        /// </summary>
        /// <returns>带 RichText 颜色的单行状态文本。</returns>
        public string ToDisplayLine()
        {
            return WorkerConditionTool.BuildConditionLine(
                this.WorkerName,
                this.State,
                this.HungryRatio,
                this.TiredRatio,
                this.MoveSpeedMultiplier,
                this.WorkProgressMultiplier);
        }
    }
}
