namespace LAB2D.Domain.Gameplay
{
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Worker;
    using LAB2D.Enum;
    using LAB2D.Gameplay;
    using LAB2D.Item;
    using LAB2D.Serializable;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 殖民地指挥中心监控的纯算术规则。
    /// 不访问 Unity 引擎、不调用 Singleton.Instance、不引用任何 Manager。
    /// 所有外部依赖通过方法参数或委托传入。
    /// </summary>
    public sealed class ColonyCommandCenterRuleService
    {
        private readonly WorkerConditionRuleService conditionRuleService = new WorkerConditionRuleService();

        public float ClampRefreshInterval(float interval)
        {
            return MathHelper.ClampRefreshInterval(interval);
        }

        /// <summary>
        /// 将 Vector3IntLAB 转换为领域位置对象。
        /// </summary>
        private static GameGridPosition ToGameGridPosition(Vector3IntLAB v)
        {
            return v == null ? default : new GameGridPosition(v.X, v.Y, v.Z);
        }

        /// <summary>
        /// 统计 Worker 数量并收集空闲 Worker 快照。
        /// </summary>
        /// <param name="report">待填充的报告。</param>
        /// <param name="workers">Worker 快照列表。</param>
        /// <returns>空闲 Worker 快照列表。</returns>
        public List<WorkerAgentSnapshot> BuildWorkerCounters(
            WorkerTaskAssignmentReport report,
            IReadOnlyList<WorkerAgentSnapshot> workers)
        {
            List<WorkerAgentSnapshot> idleWorkers = new List<WorkerAgentSnapshot>();
            if (workers == null)
            {
                return idleWorkers;
            }

            report.WorkerCount = workers.Count;
            for (int i = 0; i < workers.Count; i++)
            {
                WorkerAgentSnapshot snapshot = workers[i];
                if (snapshot == null)
                {
                    continue;
                }

                if (conditionRuleService.GetState(snapshot) == WorkerConditionState.Critical)
                {
                    report.CriticalWorkerCount++;
                }

                if (snapshot.IsIdle)
                {
                    report.IdleWorkerCount++;
                    idleWorkers.Add(snapshot);
                }
                else
                {
                    report.BusyWorkerCount++;
                }
            }

            return idleWorkers;
        }

        /// <summary>
        /// 根据任务队列与 Worker 快照构建只读任务分配诊断报告。
        /// </summary>
        /// <param name="priorityTaskGroups">WorkerTaskManager 内部按优先级组织的任务队列。</param>
        /// <param name="workers">Worker 快照列表。</param>
        /// <param name="context">外部依赖上下文（开关检查、可达性、物资查询等）。</param>
        /// <returns>任务分配诊断报告；输入为空时返回可展示的降级报告。</returns>
        public WorkerTaskAssignmentReport BuildAssignmentReport(
            IReadOnlyList<Dictionary<AWorkerTask, bool>> priorityTaskGroups,
            IReadOnlyList<WorkerAgentSnapshot> workers,
            ColonyDiagnosticContext context)
        {
            WorkerTaskAssignmentReport report = new WorkerTaskAssignmentReport();
            Dictionary<WorkerTaskBlockReason, int> reasonCounts = new Dictionary<WorkerTaskBlockReason, int>();

            try
            {
                List<WorkerAgentSnapshot> idleWorkers = BuildWorkerCounters(report, workers);
                if (priorityTaskGroups == null)
                {
                    report.PrimaryBlockReason = WorkerTaskBlockReason.ManagerUnavailable;
                    report.ErrorMessage = "殖民地指挥中心: WorkerTaskManager 未初始化";
                    AddReasonCount(reasonCounts, WorkerTaskBlockReason.ManagerUnavailable);
                    FillReasonSummaries(report, reasonCounts);
                    return report;
                }

                for (int i = 0; i < priorityTaskGroups.Count; i++)
                {
                    Dictionary<AWorkerTask, bool> taskGroup = priorityTaskGroups[i];
                    if (taskGroup == null)
                    {
                        continue;
                    }

                    foreach (KeyValuePair<AWorkerTask, bool> pair in taskGroup)
                    {
                        AWorkerTask task = pair.Key;
                        if (task == null)
                        {
                            continue;
                        }

                        report.TotalTaskCount++;
                        if (pair.Value)
                        {
                            report.RunningTaskCount++;
                            continue;
                        }

                        report.WaitingTaskCount++;
                        WorkerTaskBlockReason reason = ResolveWaitingTaskReason(
                            task, idleWorkers, workers, context);
                        if (reason == WorkerTaskBlockReason.None)
                        {
                            report.MaybeAssignableTaskCount++;
                            continue;
                        }

                        report.BlockedTaskCount++;
                        AddReasonCount(reasonCounts, reason);
                        Vector3IntLAB targetMap = task.TargetMap;
                        report.Details.Add(new WorkerTaskBlockDetail
                        {
                            TaskId = task.TaskId,
                            TaskName = task.Name,
                            TaskType = task.TaskType,
                            TargetText = targetMap == null ? "(无目标)" : targetMap.ToString(),
                            Reason = reason,
                        });
                    }
                }

                FillReasonSummaries(report, reasonCounts);
                report.PrimaryBlockReason = ResolvePrimaryReason(report.ReasonSummaries);
            }
            catch
            {
                report.ErrorMessage = "任务分配诊断失败";
                report.PrimaryBlockReason = WorkerTaskBlockReason.UnknownError;
                report.BlockedTaskCount++;
                AddReasonCount(reasonCounts, WorkerTaskBlockReason.UnknownError);
                FillReasonSummaries(report, reasonCounts);
            }

            return report;
        }

        /// <summary>
        /// 诊断等待任务的主要阻塞原因。
        /// </summary>
        /// <param name="task">等待任务。</param>
        /// <param name="idleWorkers">当前空闲 Worker 快照。</param>
        /// <param name="allWorkers">全部 Worker 快照。</param>
        /// <param name="context">外部依赖上下文。</param>
        /// <returns>阻塞原因；没有明显阻塞时返回 None。</returns>
        public WorkerTaskBlockReason ResolveWaitingTaskReason(
            AWorkerTask task,
            IReadOnlyList<WorkerAgentSnapshot> idleWorkers,
            IReadOnlyList<WorkerAgentSnapshot> allWorkers,
            ColonyDiagnosticContext context)
        {
            if (task == null)
            {
                return WorkerTaskBlockReason.UnknownError;
            }

            if (allWorkers == null || allWorkers.Count == 0)
            {
                return WorkerTaskBlockReason.NoWorker;
            }

            if (idleWorkers == null || idleWorkers.Count == 0)
            {
                return WorkerTaskBlockReason.WorkerBusy;
            }

            WorkerTaskBlockReason commonReason = ResolveCommonWorkerGate(
                task, idleWorkers, context, out WorkerAgentSnapshot candidateWorker);
            if (commonReason != WorkerTaskBlockReason.None)
            {
                return commonReason;
            }

            return ResolveTaskSpecificReason(task, candidateWorker, allWorkers, context);
        }

        /// <summary>
        /// 诊断任务开关、饥饿和可达性这类公共接取门槛。
        /// </summary>
        /// <param name="task">等待任务。</param>
        /// <param name="idleWorkers">空闲 Worker 快照。</param>
        /// <param name="context">外部依赖上下文。</param>
        /// <param name="candidateWorker">通过公共门槛的候选 Worker 快照。</param>
        /// <returns>公共阻塞原因；存在候选 Worker 时返回 None。</returns>
        public WorkerTaskBlockReason ResolveCommonWorkerGate(
            AWorkerTask task,
            IReadOnlyList<WorkerAgentSnapshot> idleWorkers,
            ColonyDiagnosticContext context,
            out WorkerAgentSnapshot candidateWorker)
        {
            candidateWorker = null;
            bool hasToggleEnabled = false;
            bool hasNotHungryWorker = false;
            bool hasReachableWorker = false;

            for (int i = 0; i < idleWorkers.Count; i++)
            {
                WorkerAgentSnapshot snapshot = idleWorkers[i];
                if (snapshot == null)
                {
                    continue;
                }

                if (!IsTaskToggleEnabled(snapshot.WorkerId, task.TaskType, context))
                {
                    continue;
                }

                hasToggleEnabled = true;
                if (snapshot.CurHungry < AWorker.ThresholdHungry &&
                    task.TaskType != AWorkerTask.WorkerTaskTypeEnum.Eat)
                {
                    continue;
                }

                hasNotHungryWorker = true;
                if (!IsTaskTargetReachable(task, context))
                {
                    continue;
                }

                hasReachableWorker = true;
                candidateWorker = snapshot;
                return WorkerTaskBlockReason.None;
            }

            if (!hasToggleEnabled)
            {
                return WorkerTaskBlockReason.TaskToggleDisabled;
            }

            if (!hasNotHungryWorker)
            {
                return WorkerTaskBlockReason.WorkerHungry;
            }

            if (!hasReachableWorker)
            {
                return WorkerTaskBlockReason.TargetUnreachable;
            }

            return WorkerTaskBlockReason.TaskSpecificCondition;
        }

        /// <summary>
        /// 诊断不同任务类型的专属阻塞原因。
        /// </summary>
        /// <param name="task">等待任务。</param>
        /// <param name="workerSnapshot">通过公共门槛的候选 Worker 快照。</param>
        /// <param name="allWorkers">全部 Worker 快照。</param>
        /// <param name="context">外部依赖上下文。</param>
        /// <returns>专属阻塞原因；没有明显阻塞时返回 None。</returns>
        public WorkerTaskBlockReason ResolveTaskSpecificReason(
            AWorkerTask task,
            WorkerAgentSnapshot workerSnapshot,
            IReadOnlyList<WorkerAgentSnapshot> allWorkers,
            ColonyDiagnosticContext context)
        {
            try
            {
                if (task is WorkerBuildTask)
                {
                    return ResolveBuildTaskReason(task, workerSnapshot, context);
                }

                if (task is WorkerCarryTask)
                {
                    return ResolveCarryTaskReason(task, workerSnapshot, context);
                }

                if (task is WorkerHungryTask)
                {
                    return ResolveHungryTaskReason(task, workerSnapshot, context);
                }

                if (task is WorkerSleepTask)
                {
                    return ResolveBoundWorkerTaskReason(task, "worker", true, allWorkers, context);
                }

                if (task is WorkerPlantTask)
                {
                    return ResolvePlantTaskReason(workerSnapshot, context);
                }

                if (task is WorkerWearTask || task is WorkerExerciseTask)
                {
                    return ResolveBoundWorkerTaskReason(task, "worker", false, allWorkers, context);
                }

                return WorkerTaskBlockReason.None;
            }
            catch
            {
                return WorkerTaskBlockReason.UnknownError;
            }
        }

        /// <summary>
        /// 诊断建造任务材料是否足够。
        /// </summary>
        /// <param name="task">建造任务。</param>
        /// <param name="workerSnapshot">候选 Worker 快照。</param>
        /// <param name="context">外部依赖上下文。</param>
        /// <returns>材料不足或无阻塞。</returns>
        public WorkerTaskBlockReason ResolveBuildTaskReason(
            AWorkerTask task,
            WorkerAgentSnapshot workerSnapshot,
            ColonyDiagnosticContext context)
        {
            if (workerSnapshot == null)
            {
                return WorkerTaskBlockReason.WorkerBusy;
            }

            Dictionary<int, ResourceInfo> needs = context.GetBuildNeeds?.Invoke(task);
            if (needs == null || needs.Count == 0)
            {
                return WorkerTaskBlockReason.None;
            }

            if (context.CanFulfillMaterials != null &&
                context.CanFulfillMaterials(workerSnapshot.WorkerId, needs))
            {
                return WorkerTaskBlockReason.None;
            }

            return WorkerTaskBlockReason.MissingMaterial;
        }

        /// <summary>
        /// 诊断搬运任务是否有仓库容量。
        /// </summary>
        /// <param name="task">搬运任务。</param>
        /// <param name="workerSnapshot">候选 Worker 快照。</param>
        /// <param name="context">外部依赖上下文。</param>
        /// <returns>仓库已满或无阻塞。</returns>
        public WorkerTaskBlockReason ResolveCarryTaskReason(
            AWorkerTask task,
            WorkerAgentSnapshot workerSnapshot,
            ColonyDiagnosticContext context)
        {
            if (workerSnapshot == null)
            {
                return WorkerTaskBlockReason.WorkerBusy;
            }

            ResourceInfo resourceInfo = context.GetCarryResourceInfo?.Invoke(task);
            if (resourceInfo == null)
            {
                return WorkerTaskBlockReason.None;
            }

            if (context.CanPlaceInInventory != null &&
                context.CanPlaceInInventory(workerSnapshot.WorkerId, resourceInfo))
            {
                return WorkerTaskBlockReason.None;
            }

            return WorkerTaskBlockReason.InventoryFull;
        }

        /// <summary>
        /// 诊断吃饭任务目标位置是否仍有可用食物。
        /// </summary>
        /// <param name="task">吃饭任务。</param>
        /// <param name="workerSnapshot">候选 Worker 快照。</param>
        /// <param name="context">外部依赖上下文。</param>
        /// <returns>食物不可用、工人不饿或无阻塞。</returns>
        public WorkerTaskBlockReason ResolveHungryTaskReason(
            AWorkerTask task,
            WorkerAgentSnapshot workerSnapshot,
            ColonyDiagnosticContext context)
        {
            if (workerSnapshot == null)
            {
                return WorkerTaskBlockReason.WorkerBusy;
            }

            if (workerSnapshot.CurHungry > AWorker.ThresholdHungry)
            {
                return WorkerTaskBlockReason.WorkerNotReady;
            }

            if (context.IsFoodAtPosition == null || task.TargetMap == null)
            {
                return WorkerTaskBlockReason.FoodUnavailable;
            }

            return context.IsFoodAtPosition(ToGameGridPosition(task.TargetMap))
                ? WorkerTaskBlockReason.None
                : WorkerTaskBlockReason.FoodUnavailable;
        }

        /// <summary>
        /// 诊断绑定 Worker 的任务，例如睡觉、穿戴和锻炼。
        /// </summary>
        /// <param name="task">绑定 Worker 的任务。</param>
        /// <param name="fieldName">保存绑定 Worker 的私有字段名。</param>
        /// <param name="requiresBed">是否要求绑定 Worker 有床位。</param>
        /// <param name="allWorkers">全部 Worker 快照。</param>
        /// <param name="context">外部依赖上下文。</param>
        /// <returns>绑定 Worker 不可用、缺床、状态未满足或无阻塞。</returns>
        public WorkerTaskBlockReason ResolveBoundWorkerTaskReason(
            AWorkerTask task,
            string fieldName,
            bool requiresBed,
            IReadOnlyList<WorkerAgentSnapshot> allWorkers,
            ColonyDiagnosticContext context)
        {
            long boundWorkerId = context.GetBoundWorkerId?.Invoke(task, fieldName) ?? 0L;
            if (boundWorkerId == 0L)
            {
                return WorkerTaskBlockReason.BoundWorkerUnavailable;
            }

            WorkerAgentSnapshot boundSnapshot = FindWorkerById(allWorkers, boundWorkerId);
            if (boundSnapshot == null)
            {
                return WorkerTaskBlockReason.BoundWorkerUnavailable;
            }

            if (!boundSnapshot.IsIdle)
            {
                return WorkerTaskBlockReason.WorkerBusy;
            }

            if (!IsTaskToggleEnabled(boundWorkerId, task.TaskType, context))
            {
                return WorkerTaskBlockReason.TaskToggleDisabled;
            }

            if (requiresBed)
            {
                if (context.HasBed == null || !context.HasBed(boundWorkerId))
                {
                    return WorkerTaskBlockReason.MissingBed;
                }

                if (boundSnapshot.CurTired >= AWorker.ThresholdTired)
                {
                    return WorkerTaskBlockReason.WorkerNotReady;
                }
            }

            return WorkerTaskBlockReason.None;
        }

        /// <summary>
        /// 诊断种植任务是否有种子和可种植农田。
        /// </summary>
        /// <param name="workerSnapshot">候选 Worker 快照。</param>
        /// <param name="context">外部依赖上下文。</param>
        /// <returns>缺种子、缺农田或无阻塞。</returns>
        public WorkerTaskBlockReason ResolvePlantTaskReason(
            WorkerAgentSnapshot workerSnapshot,
            ColonyDiagnosticContext context)
        {
            if (workerSnapshot == null)
            {
                return WorkerTaskBlockReason.WorkerBusy;
            }

            if (context.CanPlant == null || !context.CanPlant(workerSnapshot.WorkerId))
            {
                return WorkerTaskBlockReason.SeedUnavailable;
            }

            return WorkerTaskBlockReason.None;
        }

        /// <summary>
        /// 判断任务目标附近是否存在可达工作点。
        /// </summary>
        /// <param name="task">待诊断任务。</param>
        /// <param name="context">外部依赖上下文。</param>
        /// <returns>可达时返回 true。</returns>
        public bool IsTaskTargetReachable(AWorkerTask task, ColonyDiagnosticContext context)
        {
            if (task == null || task.TaskType == AWorkerTask.WorkerTaskTypeEnum.Exercise)
            {
                return true;
            }

            if (task.AvailableNeighborPos == null ||
                task.AvailableNeighborPos.Count == 0 ||
                task.TargetMap == null)
            {
                return true;
            }

            if (context.MapQuery == null)
            {
                return true;
            }

            GameGridPosition targetBase = ToGameGridPosition(task.TargetMap);
            for (int i = 0; i < task.AvailableNeighborPos.Count; i++)
            {
                Vector3IntLAB neighbor = task.AvailableNeighborPos[i];
                if (neighbor == null)
                {
                    continue;
                }

                GameGridPosition neighborPos = ToGameGridPosition(neighbor);
                GameGridPosition target = new GameGridPosition(
                    targetBase.X + neighborPos.X,
                    targetBase.Y + neighborPos.Y,
                    targetBase.Z + neighborPos.Z);
                if (context.MapQuery.IsCanReach(target))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断指定 Worker 的任务开关是否允许目标任务类型。
        /// </summary>
        /// <param name="workerId">Worker ID。</param>
        /// <param name="taskType">任务类型。</param>
        /// <param name="context">外部依赖上下文。</param>
        /// <returns>开关存在且开启时返回 true。</returns>
        public bool IsTaskToggleEnabled(
            long workerId,
            AWorkerTask.WorkerTaskTypeEnum taskType,
            ColonyDiagnosticContext context)
        {
            return context.IsTaskToggleEnabled != null &&
                context.IsTaskToggleEnabled(workerId, taskType);
        }

        /// <summary>
        /// 根据各子报告计算整体警戒等级。
        /// </summary>
        /// <param name="assignmentReport">任务分配报告。</param>
        /// <param name="supplyReport">补给报告。</param>
        /// <param name="congestionReport">拥堵报告。</param>
        /// <param name="queueSnapshot">任务队列快照。</param>
        /// <returns>整体警戒等级。</returns>
        public ColonyCommandAlertLevel ResolveAlertLevel(
            WorkerTaskAssignmentReport assignmentReport,
            WorkerSupplyReport supplyReport,
            WorkerTaskCongestionReport congestionReport,
            WorkerTaskQueueSnapshot queueSnapshot)
        {
            if (assignmentReport != null &&
                (assignmentReport.CriticalWorkerCount > 0 ||
                assignmentReport.BlockedTaskCount >= 6))
            {
                return ColonyCommandAlertLevel.Critical;
            }

            if (congestionReport != null &&
                congestionReport.Level == WorkerTaskCongestionLevel.Critical)
            {
                return ColonyCommandAlertLevel.Critical;
            }

            if (supplyReport != null && supplyReport.CriticalWorkerCount > 0)
            {
                return ColonyCommandAlertLevel.Critical;
            }

            if (assignmentReport != null &&
                assignmentReport.BlockedTaskCount >= 2)
            {
                return ColonyCommandAlertLevel.Warning;
            }

            if (supplyReport != null && supplyReport.HasIssue)
            {
                return ColonyCommandAlertLevel.Warning;
            }

            if (congestionReport != null &&
                (congestionReport.Level == WorkerTaskCongestionLevel.Congested ||
                congestionReport.Level == WorkerTaskCongestionLevel.Busy))
            {
                return ColonyCommandAlertLevel.Warning;
            }

            if (queueSnapshot != null && queueSnapshot.WaitingTaskCount > 0)
            {
                return ColonyCommandAlertLevel.Notice;
            }

            return ColonyCommandAlertLevel.Stable;
        }

        /// <summary>
        /// 构建面向玩家的行动建议。
        /// </summary>
        /// <param name="assignmentReport">分配报告（可为空）。</param>
        /// <param name="supplyReport">补给报告（可为空）。</param>
        /// <param name="congestionReport">拥堵报告（可为空）。</param>
        /// <returns>行动建议文案。</returns>
        public string BuildCommandAdvice(
            WorkerTaskAssignmentReport assignmentReport,
            WorkerSupplyReport supplyReport,
            WorkerTaskCongestionReport congestionReport)
        {
            if (assignmentReport != null &&
                assignmentReport.PrimaryBlockReason != WorkerTaskBlockReason.None)
            {
                return BuildAdviceByBlockReason(assignmentReport.PrimaryBlockReason);
            }

            if (supplyReport != null && supplyReport.HasIssue)
            {
                switch (supplyReport.PrimaryIssue)
                {
                    case WorkerSupplyIssueType.FoodShortage:
                    case WorkerSupplyIssueType.HungryWorker:
                        return "优先补充食物或搬运食物到仓库，避免饥饿继续拖慢任务链。";
                    case WorkerSupplyIssueType.BedShortage:
                    case WorkerSupplyIssueType.TiredWorker:
                        return "优先补床或确认床位绑定，让疲劳工人恢复效率。";
                    case WorkerSupplyIssueType.CriticalWorker:
                        return "暂停扩张任务，先处理临界工人的食物和休息。";
                }
            }

            if (congestionReport != null && !string.IsNullOrEmpty(congestionReport.AdviceText))
            {
                return congestionReport.AdviceText;
            }

            return "保持当前节奏，继续观察任务队列和补给。";
        }

        /// <summary>
        /// 从阻塞统计中获取主要阻塞原因。
        /// </summary>
        /// <param name="summaries">阻塞统计列表。</param>
        /// <returns>主要阻塞原因。</returns>
        public WorkerTaskBlockReason ResolvePrimaryReason(
            List<WorkerTaskBlockReasonSummary> summaries)
        {
            return summaries != null && summaries.Count > 0
                ? summaries[0].Reason
                : WorkerTaskBlockReason.None;
        }

        /// <summary>
        /// 根据主要阻塞原因构建玩家建议。
        /// </summary>
        /// <param name="reason">主要阻塞原因。</param>
        /// <returns>建议文案。</returns>
        public string BuildAdviceByBlockReason(WorkerTaskBlockReason reason)
        {
            switch (reason)
            {
                case WorkerTaskBlockReason.NoWorker:
                    return "创建或解救更多工人，再继续扩张任务。";
                case WorkerTaskBlockReason.WorkerBusy:
                    return "等待当前任务完成，或暂缓新增建造与采集指令。";
                case WorkerTaskBlockReason.TaskToggleDisabled:
                    return "打开对应 Worker 的任务开关，或换一个允许该任务类型的工人。";
                case WorkerTaskBlockReason.WorkerHungry:
                    return "先补充食物并让工人吃饭，再推进非吃饭任务。";
                case WorkerTaskBlockReason.TargetUnreachable:
                    return "检查建筑、墙体和地形阻挡，为目标点留出可达工作位。";
                case WorkerTaskBlockReason.MissingMaterial:
                    return "补齐建造材料，或先安排采集和搬运任务。";
                case WorkerTaskBlockReason.InventoryFull:
                    return "扩建仓库或清理库存，再继续搬运掉落物。";
                case WorkerTaskBlockReason.FoodUnavailable:
                    return "确认食物仍在仓库且未被预取完，必要时重新搬运食物。";
                case WorkerTaskBlockReason.MissingBed:
                    return "建造床并绑定给疲劳工人。";
                case WorkerTaskBlockReason.SeedUnavailable:
                    return "补充可用种子后再安排种植。";
                case WorkerTaskBlockReason.FarmlandUnavailable:
                    return "建造或清理可种植农田。";
                case WorkerTaskBlockReason.BoundWorkerUnavailable:
                    return "等待绑定工人空闲，或重新下达给目标工人的专属任务。";
                default:
                    return "查看任务详情，优先处理最集中的阻塞来源。";
            }
        }

        /// <summary>
        /// 从 Worker 快照列表按 ID 查找指定 Worker。
        /// </summary>
        /// <param name="workers">Worker 快照列表。</param>
        /// <param name="workerId">Worker ID。</param>
        /// <returns>匹配的 Worker 快照，未找到返回 null。</returns>
        private static WorkerAgentSnapshot FindWorkerById(
            IReadOnlyList<WorkerAgentSnapshot> workers,
            long workerId)
        {
            for (int i = 0; i < workers.Count; i++)
            {
                if (workers[i] != null && workers[i].WorkerId == workerId)
                {
                    return workers[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 追加阻塞原因计数。
        /// </summary>
        private static void AddReasonCount(
            Dictionary<WorkerTaskBlockReason, int> reasonCounts,
            WorkerTaskBlockReason reason)
        {
            if (reason == WorkerTaskBlockReason.None)
            {
                return;
            }

            if (!reasonCounts.ContainsKey(reason))
            {
                reasonCounts[reason] = 0;
            }

            reasonCounts[reason]++;
        }

        /// <summary>
        /// 将阻塞原因计数字典写入报告并按数量排序。
        /// </summary>
        private static void FillReasonSummaries(
            WorkerTaskAssignmentReport report,
            Dictionary<WorkerTaskBlockReason, int> reasonCounts)
        {
            report.ReasonSummaries.Clear();
            foreach (KeyValuePair<WorkerTaskBlockReason, int> pair in reasonCounts)
            {
                report.ReasonSummaries.Add(new WorkerTaskBlockReasonSummary
                {
                    Reason = pair.Key,
                    Count = pair.Value,
                });
            }

            report.ReasonSummaries.Sort((left, right) => right.Count.CompareTo(left.Count));
        }
    }

    /// <summary>
    /// 殖民地指挥中心诊断外部依赖上下文。
    /// 由 Tool 层创建并注入，封装所有非纯规则的外部访问（任务开关、地图可达性、
    /// 物资库存查询等），使 RuleService 保持纯 C# 无外部依赖。
    /// </summary>
    public class ColonyDiagnosticContext
    {
        /// <summary>地图可达性查询。</summary>
        public IMapWalkabilityQuery MapQuery;

        /// <summary>任务开关检查委托。参数：workerId, taskType。返回：是否开启。</summary>
        public System.Func<long, AWorkerTask.WorkerTaskTypeEnum, bool> IsTaskToggleEnabled;

        /// <summary>获取建造任务所需材料字典的委托。</summary>
        public System.Func<AWorkerTask, Dictionary<int, ResourceInfo>> GetBuildNeeds;

        /// <summary>检查能否满足材料需求的委托。参数：workerId, needs。返回：材料是否足够。</summary>
        public System.Func<long, Dictionary<int, ResourceInfo>, bool> CanFulfillMaterials;

        /// <summary>获取搬运任务资源信息的委托。</summary>
        public System.Func<AWorkerTask, ResourceInfo> GetCarryResourceInfo;

        /// <summary>检查仓库是否有空间的委托。参数：workerId, resourceInfo。返回：是否可放置。</summary>
        public System.Func<long, ResourceInfo, bool> CanPlaceInInventory;

        /// <summary>检查指定位置是否有食物的委托。参数：gridPosition。返回：是否有食物。</summary>
        public System.Func<GameGridPosition, bool> IsFoodAtPosition;

        /// <summary>检查是否可种植的委托。参数：workerId。返回：种子和农田是否可用。</summary>
        public System.Func<long, bool> CanPlant;

        /// <summary>检查 Worker 是否有床位的委托。参数：workerId。返回：是否有床。</summary>
        public System.Func<long, bool> HasBed;

        /// <summary>获取任务绑定的 Worker ID 的委托。参数：task, fieldName。返回：绑定 Worker 的 ID，0 表示无。</summary>
        public System.Func<AWorkerTask, string, long> GetBoundWorkerId;
    }
}
