namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// A006 殖民地运营指挥中心公共工具。
    /// 负责只读诊断 Worker 任务阻塞、聚合运营报告和生成展示文案；不新增任务、不取消任务、不预留资源、不调整 Worker 优先级、不访问存档、Photon 或 AssetBundle。
    /// </summary>
    public static class ColonyCommandCenterTool
    {
        /// <summary>
        /// 根据任务队列与 Worker 列表构建只读任务分配诊断报告。
        /// </summary>
        /// <param name="priorityTaskGroups">WorkerTaskManager 内部按优先级组织的任务队列。</param>
        /// <param name="workers">当前 Worker 列表。</param>
        /// <returns>任务分配诊断报告；输入为空时返回可展示的降级报告。</returns>
        public static WorkerTaskAssignmentReport BuildAssignmentReport(
            IReadOnlyList<Dictionary<AWorkerTask, bool>> priorityTaskGroups,
            IReadOnlyList<AWorker> workers)
        {
            WorkerTaskAssignmentReport report = new WorkerTaskAssignmentReport();
            Dictionary<WorkerTaskBlockReason, int> reasonCounts = new Dictionary<WorkerTaskBlockReason, int>();

            try
            {
                List<AWorker> idleWorkers = BuildWorkerCounters(report, workers);
                if (priorityTaskGroups == null)
                {
                    report.PrimaryBlockReason = WorkerTaskBlockReason.ManagerUnavailable;
                    report.ErrorMessage = ColonyCommandCenterConstant.ManagerUnavailableText;
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
                        WorkerTaskBlockReason reason = ResolveWaitingTaskReason(task, idleWorkers, workers);
                        if (reason == WorkerTaskBlockReason.None)
                        {
                            report.MaybeAssignableTaskCount++;
                            continue;
                        }

                        report.BlockedTaskCount++;
                        AddReasonCount(reasonCounts, reason);
                        report.Details.Add(new WorkerTaskBlockDetail
                        {
                            TaskId = task.TaskId,
                            TaskName = task.Name,
                            TaskType = task.TaskType,
                            TargetText = task.TargetMap == null ? "(无目标)" : task.TargetMap.ToString(),
                            Reason = reason,
                        });
                    }
                }

                FillReasonSummaries(report, reasonCounts);
                report.PrimaryBlockReason = ResolvePrimaryReason(report.ReasonSummaries);
            }
            catch (Exception exception)
            {
                report.ErrorMessage = "任务分配诊断失败: " + exception.Message;
                report.PrimaryBlockReason = WorkerTaskBlockReason.UnknownError;
                report.BlockedTaskCount++;
                AddReasonCount(reasonCounts, WorkerTaskBlockReason.UnknownError);
                FillReasonSummaries(report, reasonCounts);
            }

            return report;
        }

        /// <summary>
        /// 根据任务、补给、拥堵和 Worker 数据构建殖民地指挥报告。
        /// </summary>
        /// <param name="queueSnapshot">任务队列快照。</param>
        /// <param name="assignmentReport">任务分配诊断报告。</param>
        /// <param name="supplyReport">补给缺口报告。</param>
        /// <param name="congestionReport">任务拥堵报告。</param>
        /// <returns>殖民地指挥报告。</returns>
        public static ColonyCommandCenterReport BuildCommandReport(
            WorkerTaskQueueSnapshot queueSnapshot,
            WorkerTaskAssignmentReport assignmentReport,
            WorkerSupplyReport supplyReport,
            WorkerTaskCongestionReport congestionReport,
            float updatedTime)
        {
            ColonyCommandCenterReport report = new ColonyCommandCenterReport
            {
                QueueSnapshot = queueSnapshot,
                AssignmentReport = assignmentReport,
                SupplyReport = supplyReport,
                CongestionReport = congestionReport,
                UpdatedTime = updatedTime,
            };

            report.AlertLevel = ResolveAlertLevel(assignmentReport, supplyReport, congestionReport, queueSnapshot);
            report.FocusText = BuildFocusText(report);
            report.AdviceText = BuildCommandAdvice(report);
            return report;
        }

        /// <summary>
        /// 获取警戒等级中文名。
        /// </summary>
        /// <param name="level">警戒等级。</param>
        /// <returns>适合 UI 和日志展示的中文名。</returns>
        public static string GetAlertLevelName(ColonyCommandAlertLevel level)
        {
            switch (level)
            {
                case ColonyCommandAlertLevel.Notice:
                    return "关注";
                case ColonyCommandAlertLevel.Warning:
                    return "警告";
                case ColonyCommandAlertLevel.Critical:
                    return "危急";
                default:
                    return "稳定";
            }
        }

        /// <summary>
        /// 获取警戒等级 RichText 颜色。
        /// </summary>
        /// <param name="level">警戒等级。</param>
        /// <returns>HTML 颜色字符串。</returns>
        public static string GetAlertLevelRichColor(ColonyCommandAlertLevel level)
        {
            switch (level)
            {
                case ColonyCommandAlertLevel.Notice:
                    return PixelUITheme.RichSky;
                case ColonyCommandAlertLevel.Warning:
                    return PixelUITheme.RichGold;
                case ColonyCommandAlertLevel.Critical:
                    return PixelUITheme.RichCoral;
                default:
                    return PixelUITheme.RichMint;
            }
        }

        /// <summary>
        /// 获取任务阻塞原因中文名。
        /// </summary>
        /// <param name="reason">阻塞原因。</param>
        /// <returns>适合 HUD、Tip 和日志展示的中文名。</returns>
        public static string GetBlockReasonName(WorkerTaskBlockReason reason)
        {
            switch (reason)
            {
                case WorkerTaskBlockReason.ManagerUnavailable:
                    return "任务管理器未初始化";
                case WorkerTaskBlockReason.NoWorker:
                    return "没有工人";
                case WorkerTaskBlockReason.WorkerBusy:
                    return "工人全忙";
                case WorkerTaskBlockReason.TaskToggleDisabled:
                    return "任务开关关闭";
                case WorkerTaskBlockReason.WorkerHungry:
                    return "工人饥饿";
                case WorkerTaskBlockReason.TargetUnreachable:
                    return "目标不可达";
                case WorkerTaskBlockReason.MissingMaterial:
                    return "材料不足";
                case WorkerTaskBlockReason.InventoryFull:
                    return "仓库已满";
                case WorkerTaskBlockReason.FoodUnavailable:
                    return "食物不可用";
                case WorkerTaskBlockReason.MissingBed:
                    return "缺少床位";
                case WorkerTaskBlockReason.BoundWorkerUnavailable:
                    return "绑定工人不可用";
                case WorkerTaskBlockReason.WorkerNotReady:
                    return "工人未满足条件";
                case WorkerTaskBlockReason.SeedUnavailable:
                    return "缺少种子";
                case WorkerTaskBlockReason.FarmlandUnavailable:
                    return "缺少可种植农田";
                case WorkerTaskBlockReason.TaskSpecificCondition:
                    return "任务专属条件未满足";
                case WorkerTaskBlockReason.UnknownError:
                    return "诊断异常";
                default:
                    return "暂无阻塞";
            }
        }

        /// <summary>
        /// 获取任务阻塞原因 RichText 颜色。
        /// </summary>
        /// <param name="reason">阻塞原因。</param>
        /// <returns>HTML 颜色字符串。</returns>
        public static string GetBlockReasonRichColor(WorkerTaskBlockReason reason)
        {
            switch (reason)
            {
                case WorkerTaskBlockReason.None:
                    return PixelUITheme.RichMint;
                case WorkerTaskBlockReason.WorkerBusy:
                case WorkerTaskBlockReason.TaskToggleDisabled:
                case WorkerTaskBlockReason.WorkerNotReady:
                    return PixelUITheme.RichSky;
                case WorkerTaskBlockReason.WorkerHungry:
                case WorkerTaskBlockReason.FoodUnavailable:
                case WorkerTaskBlockReason.MissingMaterial:
                case WorkerTaskBlockReason.SeedUnavailable:
                    return PixelUITheme.RichGold;
                case WorkerTaskBlockReason.MissingBed:
                case WorkerTaskBlockReason.TargetUnreachable:
                case WorkerTaskBlockReason.FarmlandUnavailable:
                    return PixelUITheme.RichLavender;
                default:
                    return PixelUITheme.RichCoral;
            }
        }

        /// <summary>
        /// 构建指挥中心纯文本报告。
        /// </summary>
        /// <param name="report">指挥报告。</param>
        /// <returns>适合 Editor 弹窗和日志的纯文本。</returns>
        public static string BuildPlainText(ColonyCommandCenterReport report)
        {
            if (report == null)
            {
                return ColonyCommandCenterConstant.EmptyText;
            }

            StringBuilder builder = new StringBuilder(1024);
            builder.Append("殖民地指挥中心: ")
                .Append(GetAlertLevelName(report.AlertLevel))
                .Append(" - ")
                .Append(report.FocusText)
                .AppendLine();
            builder.Append("建议: ").Append(report.AdviceText).AppendLine();

            if (report.AssignmentReport != null)
            {
                builder.AppendLine(report.AssignmentReport.ToPlainText());
            }

            if (report.SupplyReport != null)
            {
                builder.AppendLine(report.SupplyReport.ToSummaryText());
            }

            if (report.CongestionReport != null)
            {
                builder.AppendLine(report.CongestionReport.ToSummaryText());
            }

            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// 统计 Worker 数量并收集空闲 Worker。
        /// </summary>
        /// <param name="report">待填充的报告。</param>
        /// <param name="workers">Worker 列表。</param>
        /// <returns>空闲 Worker 列表。</returns>
        private static List<AWorker> BuildWorkerCounters(
            WorkerTaskAssignmentReport report,
            IReadOnlyList<AWorker> workers)
        {
            List<AWorker> idleWorkers = new List<AWorker>();
            if (workers == null)
            {
                return idleWorkers;
            }

            report.WorkerCount = workers.Count;
            for (int i = 0; i < workers.Count; i++)
            {
                AWorker worker = workers[i];
                if (!WorkerConditionTool.TryGetWorkerData(worker, out AWorker.WorkerData workerData))
                {
                    continue;
                }

                if (WorkerConditionTool.GetState(workerData) == WorkerConditionState.Critical)
                {
                    report.CriticalWorkerCount++;
                }

                if (workerData.Task == null)
                {
                    report.IdleWorkerCount++;
                    idleWorkers.Add(worker);
                }
                else
                {
                    report.BusyWorkerCount++;
                }
            }

            return idleWorkers;
        }

        /// <summary>
        /// 诊断等待任务的主要阻塞原因。
        /// </summary>
        /// <param name="task">等待任务。</param>
        /// <param name="idleWorkers">当前空闲 Worker。</param>
        /// <param name="allWorkers">全部 Worker。</param>
        /// <returns>阻塞原因；没有明显阻塞时返回 None。</returns>
        private static WorkerTaskBlockReason ResolveWaitingTaskReason(
            AWorkerTask task,
            IReadOnlyList<AWorker> idleWorkers,
            IReadOnlyList<AWorker> allWorkers)
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

            WorkerTaskBlockReason commonReason = ResolveCommonWorkerGate(task, idleWorkers, out AWorker candidateWorker);
            if (commonReason != WorkerTaskBlockReason.None)
            {
                return commonReason;
            }

            WorkerTaskBlockReason specificReason = ResolveTaskSpecificReason(task, candidateWorker);
            return specificReason;
        }

        /// <summary>
        /// 诊断任务开关、饥饿和可达性这类公共接取门槛。
        /// </summary>
        /// <param name="task">等待任务。</param>
        /// <param name="idleWorkers">空闲 Worker。</param>
        /// <param name="candidateWorker">通过公共门槛的候选 Worker。</param>
        /// <returns>公共阻塞原因；存在候选 Worker 时返回 None。</returns>
        private static WorkerTaskBlockReason ResolveCommonWorkerGate(
            AWorkerTask task,
            IReadOnlyList<AWorker> idleWorkers,
            out AWorker candidateWorker)
        {
            candidateWorker = null;
            bool hasToggleEnabled = false;
            bool hasNotHungryWorker = false;
            bool hasReachableWorker = false;

            for (int i = 0; i < idleWorkers.Count; i++)
            {
                AWorker worker = idleWorkers[i];
                if (!WorkerConditionTool.TryGetWorkerData(worker, out AWorker.WorkerData workerData))
                {
                    continue;
                }

                if (!IsTaskToggleEnabled(workerData, task.TaskType))
                {
                    continue;
                }

                hasToggleEnabled = true;
                if (workerData.CurHungry < AWorker.ThresholdHungry &&
                    task.TaskType != AWorkerTask.WorkerTaskTypeEnum.Eat)
                {
                    continue;
                }

                hasNotHungryWorker = true;
                if (!IsTaskTargetReachable(task))
                {
                    continue;
                }

                hasReachableWorker = true;
                candidateWorker = worker;
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
        /// <param name="worker">通过公共门槛的候选 Worker。</param>
        /// <returns>专属阻塞原因；没有明显阻塞时返回 None。</returns>
        private static WorkerTaskBlockReason ResolveTaskSpecificReason(AWorkerTask task, AWorker worker)
        {
            try
            {
                if (task is WorkerBuildTask)
                {
                    return ResolveBuildTaskReason(task, worker);
                }

                if (task is WorkerCarryTask)
                {
                    return ResolveCarryTaskReason(task, worker);
                }

                if (task is WorkerHungryTask)
                {
                    return ResolveHungryTaskReason(task, worker);
                }

                if (task is WorkerSleepTask)
                {
                    return ResolveBoundWorkerTaskReason(task, "worker", true);
                }

                if (task is WorkerPlantTask)
                {
                    return ResolvePlantTaskReason(worker);
                }

                if (task is WorkerWearTask || task is WorkerExerciseTask)
                {
                    return ResolveBoundWorkerTaskReason(task, "worker", false);
                }

                return WorkerTaskBlockReason.None;
            }
            catch (Exception)
            {
                return WorkerTaskBlockReason.UnknownError;
            }
        }

        /// <summary>
        /// 诊断建造任务材料是否足够。
        /// </summary>
        /// <param name="task">建造任务。</param>
        /// <param name="worker">候选 Worker。</param>
        /// <returns>材料不足或无阻塞。</returns>
        private static WorkerTaskBlockReason ResolveBuildTaskReason(AWorkerTask task, AWorker worker)
        {
            if (worker == null)
            {
                return WorkerTaskBlockReason.WorkerBusy;
            }

            if (!TryGetFieldValue(task, "needs", out Dictionary<int, ResourceInfo> needs) ||
                needs == null ||
                needs.Count == 0)
            {
                return WorkerTaskBlockReason.None;
            }

            if (worker.IsEnough(needs))
            {
                return WorkerTaskBlockReason.None;
            }

            Dictionary<int, ResourceInfo> remaining = worker.GetRemaining(needs);
            return InventoryManager.Instance != null &&
                InventoryManager.Instance.IsEnoughAndPreTake(worker, remaining)
                ? WorkerTaskBlockReason.None
                : WorkerTaskBlockReason.MissingMaterial;
        }

        /// <summary>
        /// 诊断搬运任务是否有仓库容量。
        /// </summary>
        /// <param name="task">搬运任务。</param>
        /// <param name="worker">候选 Worker。</param>
        /// <returns>仓库已满或无阻塞。</returns>
        private static WorkerTaskBlockReason ResolveCarryTaskReason(AWorkerTask task, AWorker worker)
        {
            if (worker == null)
            {
                return WorkerTaskBlockReason.WorkerBusy;
            }

            if (!TryGetFieldValue(task, "resourceInfo", out ResourceInfo resourceInfo) || resourceInfo == null)
            {
                return WorkerTaskBlockReason.None;
            }

            return InventoryManager.Instance != null &&
                InventoryManager.Instance.IsEnoughAndPrePlace(worker, resourceInfo)
                ? WorkerTaskBlockReason.None
                : WorkerTaskBlockReason.InventoryFull;
        }

        /// <summary>
        /// 诊断吃饭任务目标位置是否仍有可用食物。
        /// </summary>
        /// <param name="task">吃饭任务。</param>
        /// <param name="worker">候选 Worker。</param>
        /// <returns>食物不可用、工人不饿或无阻塞。</returns>
        private static WorkerTaskBlockReason ResolveHungryTaskReason(AWorkerTask task, AWorker worker)
        {
            if (worker == null || !WorkerConditionTool.TryGetWorkerData(worker, out AWorker.WorkerData workerData))
            {
                return WorkerTaskBlockReason.WorkerBusy;
            }

            if (workerData.CurHungry > AWorker.ThresholdHungry)
            {
                return WorkerTaskBlockReason.WorkerNotReady;
            }

            ResourceInfo resourceInfo = InventoryManager.Instance == null || task.TargetMap == null
                ? null
                : InventoryManager.Instance.GetResourceByPos(Vector3IntLAB.ToVector3Int(task.TargetMap));
            if (resourceInfo == null || resourceInfo.Count <= 0)
            {
                return WorkerTaskBlockReason.FoodUnavailable;
            }

            return ItemDataManager.Instance.IdToType(resourceInfo.Id) == AItem.ItemTypeEnum.Food
                ? WorkerTaskBlockReason.None
                : WorkerTaskBlockReason.FoodUnavailable;
        }

        /// <summary>
        /// 诊断绑定 Worker 的任务，例如睡觉、穿戴和锻炼。
        /// </summary>
        /// <param name="task">绑定 Worker 的任务。</param>
        /// <param name="fieldName">保存绑定 Worker 的私有字段名。</param>
        /// <param name="requiresBed">是否要求绑定 Worker 有床位。</param>
        /// <returns>绑定 Worker 不可用、缺床、状态未满足或无阻塞。</returns>
        private static WorkerTaskBlockReason ResolveBoundWorkerTaskReason(
            AWorkerTask task,
            string fieldName,
            bool requiresBed)
        {
            if (!TryGetFieldValue(task, fieldName, out AWorker boundWorker) || boundWorker == null)
            {
                return WorkerTaskBlockReason.BoundWorkerUnavailable;
            }

            if (!WorkerConditionTool.TryGetWorkerData(boundWorker, out AWorker.WorkerData workerData))
            {
                return WorkerTaskBlockReason.BoundWorkerUnavailable;
            }

            if (workerData.Task != null)
            {
                return WorkerTaskBlockReason.WorkerBusy;
            }

            if (!IsTaskToggleEnabled(workerData, task.TaskType))
            {
                return WorkerTaskBlockReason.TaskToggleDisabled;
            }

            if (requiresBed)
            {
                if (boundWorker.BedItem == null)
                {
                    return WorkerTaskBlockReason.MissingBed;
                }

                if (workerData.CurTired >= AWorker.ThresholdTired)
                {
                    return WorkerTaskBlockReason.WorkerNotReady;
                }
            }

            return WorkerTaskBlockReason.None;
        }

        /// <summary>
        /// 诊断种植任务是否有种子和可种植农田。
        /// </summary>
        /// <param name="worker">候选 Worker。</param>
        /// <returns>缺种子、缺农田或无阻塞。</returns>
        private static WorkerTaskBlockReason ResolvePlantTaskReason(AWorker worker)
        {
            if (worker == null)
            {
                return WorkerTaskBlockReason.WorkerBusy;
            }

            if (InventoryManager.Instance == null ||
                InventoryManager.Instance.TypeToResource == null ||
                !InventoryManager.Instance.TypeToResource.TryGetValue(AItem.ItemTypeEnum.Seed, out Dictionary<Vector3Int, ResourceInfo> seeds) ||
                !HasPositiveResource(seeds))
            {
                return WorkerTaskBlockReason.SeedUnavailable;
            }

            return FarmlandManager.Instance != null &&
                FarmlandManager.Instance.IsEnoughAndPrePlant(worker, null) != default
                ? WorkerTaskBlockReason.None
                : WorkerTaskBlockReason.FarmlandUnavailable;
        }

        /// <summary>
        /// 判断任务目标附近是否存在可达工作点。
        /// </summary>
        /// <param name="task">待诊断任务。</param>
        /// <returns>可达时返回 true。</returns>
        private static bool IsTaskTargetReachable(AWorkerTask task)
        {
            if (task == null || task.TaskType == AWorkerTask.WorkerTaskTypeEnum.Exercise)
            {
                return true;
            }

            if (task.AvailableNeighborPos == null || task.AvailableNeighborPos.Count == 0 || task.TargetMap == null)
            {
                return true;
            }

            if (BuildMap.Instance == null)
            {
                return true;
            }

            for (int i = 0; i < task.AvailableNeighborPos.Count; i++)
            {
                Vector3IntLAB neighbor = task.AvailableNeighborPos[i];
                if (neighbor == null)
                {
                    continue;
                }

                Vector3Int target = Vector3IntLAB.ToVector3Int(neighbor + task.TargetMap);
                if (BuildMap.Instance.IsCanReach(target))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断 Worker 的任务开关是否允许目标任务类型。
        /// </summary>
        /// <param name="workerData">Worker 数据。</param>
        /// <param name="taskType">任务类型。</param>
        /// <returns>开关存在且开启时返回 true。</returns>
        private static bool IsTaskToggleEnabled(AWorker.WorkerData workerData, AWorkerTask.WorkerTaskTypeEnum taskType)
        {
            int index = (int)taskType;
            return workerData != null &&
                workerData.TaskToggle != null &&
                index >= 0 &&
                index < workerData.TaskToggle.Length &&
                workerData.TaskToggle[index];
        }

        /// <summary>
        /// 判断资源字典中是否存在正数资源。
        /// </summary>
        /// <param name="resources">资源字典。</param>
        /// <returns>存在 Count 大于 0 的资源时返回 true。</returns>
        private static bool HasPositiveResource(Dictionary<Vector3Int, ResourceInfo> resources)
        {
            if (resources == null)
            {
                return false;
            }

            foreach (KeyValuePair<Vector3Int, ResourceInfo> pair in resources)
            {
                if (pair.Value != null && pair.Value.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 安全读取任务私有字段。
        /// 仅用于诊断已有任务对象内部数据，不修改字段内容。
        /// </summary>
        /// <typeparam name="T">字段目标类型。</typeparam>
        /// <param name="target">目标对象。</param>
        /// <param name="fieldName">字段名。</param>
        /// <param name="value">读取到的字段值。</param>
        /// <returns>读取成功且类型匹配时返回 true。</returns>
        private static bool TryGetFieldValue<T>(object target, string fieldName, out T value)
        {
            value = default;
            if (target == null || string.IsNullOrEmpty(fieldName))
            {
                return false;
            }

            Type type = target.GetType();
            while (type != null && type != typeof(object))
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    object rawValue = field.GetValue(target);
                    if (rawValue is T typedValue)
                    {
                        value = typedValue;
                        return true;
                    }

                    return false;
                }

                type = type.BaseType;
            }

            return false;
        }

        /// <summary>
        /// 追加阻塞原因计数。
        /// </summary>
        /// <param name="reasonCounts">计数字典。</param>
        /// <param name="reason">阻塞原因。</param>
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
        /// <param name="report">目标报告。</param>
        /// <param name="reasonCounts">计数字典。</param>
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

        /// <summary>
        /// 从阻塞统计中获取主要阻塞原因。
        /// </summary>
        /// <param name="summaries">阻塞统计列表。</param>
        /// <returns>主要阻塞原因。</returns>
        private static WorkerTaskBlockReason ResolvePrimaryReason(List<WorkerTaskBlockReasonSummary> summaries)
        {
            return summaries != null && summaries.Count > 0
                ? summaries[0].Reason
                : WorkerTaskBlockReason.None;
        }

        /// <summary>
        /// 根据各子报告计算整体警戒等级。
        /// </summary>
        /// <param name="assignment">任务分配报告。</param>
        /// <param name="supply">补给报告。</param>
        /// <param name="congestion">拥堵报告。</param>
        /// <param name="queue">任务队列快照。</param>
        /// <returns>整体警戒等级。</returns>
        private static ColonyCommandAlertLevel ResolveAlertLevel(
            WorkerTaskAssignmentReport assignment,
            WorkerSupplyReport supply,
            WorkerTaskCongestionReport congestion,
            WorkerTaskQueueSnapshot queue)
        {
            if (assignment != null &&
                (assignment.CriticalWorkerCount > 0 ||
                assignment.BlockedTaskCount >= ColonyCommandCenterConstant.CriticalBlockedTaskThreshold))
            {
                return ColonyCommandAlertLevel.Critical;
            }

            if (congestion != null && congestion.Level == WorkerTaskCongestionLevel.Critical)
            {
                return ColonyCommandAlertLevel.Critical;
            }

            if (supply != null && supply.CriticalWorkerCount > 0)
            {
                return ColonyCommandAlertLevel.Critical;
            }

            if (assignment != null &&
                assignment.BlockedTaskCount >= ColonyCommandCenterConstant.WarningBlockedTaskThreshold)
            {
                return ColonyCommandAlertLevel.Warning;
            }

            if (supply != null && supply.HasIssue)
            {
                return ColonyCommandAlertLevel.Warning;
            }

            if (congestion != null &&
                (congestion.Level == WorkerTaskCongestionLevel.Congested ||
                congestion.Level == WorkerTaskCongestionLevel.Busy))
            {
                return ColonyCommandAlertLevel.Warning;
            }

            if (queue != null && queue.WaitingTaskCount > 0)
            {
                return ColonyCommandAlertLevel.Notice;
            }

            return ColonyCommandAlertLevel.Stable;
        }

        /// <summary>
        /// 构建指挥中心主问题摘要。
        /// </summary>
        /// <param name="report">指挥报告。</param>
        /// <returns>主问题摘要。</returns>
        private static string BuildFocusText(ColonyCommandCenterReport report)
        {
            WorkerTaskAssignmentReport assignment = report.AssignmentReport;
            WorkerSupplyReport supply = report.SupplyReport;
            WorkerTaskCongestionReport congestion = report.CongestionReport;

            if (assignment != null && assignment.CriticalWorkerCount > 0)
            {
                return $"有 {assignment.CriticalWorkerCount} 名工人接近停工。";
            }

            if (supply != null && supply.CriticalWorkerCount > 0)
            {
                return $"补给链出现危急工人 {supply.CriticalWorkerCount} 名。";
            }

            if (assignment != null && assignment.BlockedTaskCount > 0)
            {
                return $"有 {assignment.BlockedTaskCount} 个等待任务被诊断为阻塞，主要原因是 {GetBlockReasonName(assignment.PrimaryBlockReason)}。";
            }

            if (supply != null && supply.HasIssue)
            {
                return $"补给存在问题：{WorkerSupplyTool.GetIssueName(supply.PrimaryIssue)}。";
            }

            if (congestion != null &&
                (congestion.Level == WorkerTaskCongestionLevel.Busy ||
                congestion.Level == WorkerTaskCongestionLevel.Congested ||
                congestion.Level == WorkerTaskCongestionLevel.Critical))
            {
                return $"任务队列处于 {WorkerTaskCongestionTool.GetLevelName(congestion.Level)}。";
            }

            if (report.QueueSnapshot != null && report.QueueSnapshot.WaitingTaskCount > 0)
            {
                return $"还有 {report.QueueSnapshot.WaitingTaskCount} 个任务等待接取。";
            }

            return "殖民地运行平稳。";
        }

        /// <summary>
        /// 构建面向玩家的行动建议。
        /// </summary>
        /// <param name="report">指挥报告。</param>
        /// <returns>行动建议文案。</returns>
        private static string BuildCommandAdvice(ColonyCommandCenterReport report)
        {
            WorkerTaskAssignmentReport assignment = report.AssignmentReport;
            WorkerSupplyReport supply = report.SupplyReport;
            WorkerTaskCongestionReport congestion = report.CongestionReport;

            if (assignment != null && assignment.PrimaryBlockReason != WorkerTaskBlockReason.None)
            {
                return BuildAdviceByBlockReason(assignment.PrimaryBlockReason);
            }

            if (supply != null && supply.HasIssue)
            {
                switch (supply.PrimaryIssue)
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

            if (congestion != null && !string.IsNullOrEmpty(congestion.AdviceText))
            {
                return congestion.AdviceText;
            }

            return "保持当前节奏，继续观察任务队列和补给。";
        }

        /// <summary>
        /// 根据主要阻塞原因构建玩家建议。
        /// </summary>
        /// <param name="reason">主要阻塞原因。</param>
        /// <returns>建议文案。</returns>
        private static string BuildAdviceByBlockReason(WorkerTaskBlockReason reason)
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
    }
}
