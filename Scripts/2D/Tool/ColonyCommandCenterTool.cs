namespace LAB2D.Tool
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay;
    using LAB2D.Domain.Worker;
    using LAB2D.Enum;
    using LAB2D.Gameplay;
    using LAB2D.Item;
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
        private static readonly ColonyCommandCenterRuleService RuleService = new ColonyCommandCenterRuleService();

        /// <summary>
        /// 根据任务队列与 Worker 列表构建只读任务分配诊断报告。
        /// 内部委托 RuleService 执行纯规则计算，本方法负责从游戏对象提取数据并创建外部依赖上下文。
        /// </summary>
        /// <param name="priorityTaskGroups">WorkerTaskManager 内部按优先级组织的任务队列。</param>
        /// <param name="workers">当前 Worker 列表。</param>
        /// <returns>任务分配诊断报告；输入为空时返回可展示的降级报告。</returns>
        public static WorkerTaskAssignmentReport BuildAssignmentReport(
            IReadOnlyList<Dictionary<AWorkerTask, bool>> priorityTaskGroups,
            IReadOnlyList<AWorker> workers)
        {
            List<WorkerAgentSnapshot> snapshots = new List<WorkerAgentSnapshot>();
            Dictionary<long, AWorker> workerMap = new Dictionary<long, AWorker>();

            if (workers != null)
            {
                for (int i = 0; i < workers.Count; i++)
                {
                    AWorker worker = workers[i];
                    if (worker == null)
                    {
                        continue;
                    }

                    long workerId = worker.GetInstanceID();
                    workerMap[workerId] = worker;

                    if (WorkerConditionTool.TryGetWorkerData(worker, out AWorker.WorkerData workerData))
                    {
                        snapshots.Add(new WorkerAgentSnapshot(
                            workerId: workerId,
                            position: default,
                            isIdle: workerData.Task == null,
                            isPaused: worker.IsDialoguePaused,
                            curHungry: workerData.CurHungry,
                            maxHungry: workerData.MaxHungry,
                            curTired: workerData.CurTired,
                            maxTired: workerData.MaxTired));
                    }
                }
            }

            ColonyDiagnosticContext context = BuildDiagnosticContext(workerMap);

            // 将 Dictionary<AWorkerTask, bool> 转换为 Domain 接口类型
            List<Dictionary<IWorkerTaskInfo, bool>> convertedGroups = null;
            if (priorityTaskGroups != null)
            {
                convertedGroups = new List<Dictionary<IWorkerTaskInfo, bool>>();
                for (int i = 0; i < priorityTaskGroups.Count; i++)
                {
                    Dictionary<AWorkerTask, bool> group = priorityTaskGroups[i];
                    if (group == null)
                    {
                        continue;
                    }

                    Dictionary<IWorkerTaskInfo, bool> converted = new Dictionary<IWorkerTaskInfo, bool>();
                    foreach (KeyValuePair<AWorkerTask, bool> pair in group)
                    {
                        converted[pair.Key] = pair.Value;
                    }

                    convertedGroups.Add(converted);
                }
            }

            return RuleService.BuildAssignmentReport(convertedGroups, snapshots, context);
        }

        /// <summary>
        /// 根据任务、补给、拥堵和 Worker 数据构建殖民地指挥报告。
        /// </summary>
        /// <param name="queueSnapshot">任务队列快照。</param>
        /// <param name="assignmentReport">任务分配诊断报告。</param>
        /// <param name="supplyReport">补给缺口报告。</param>
        /// <param name="congestionReport">任务拥堵报告。</param>
        /// <param name="updatedTime">报告生成时间。</param>
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

            report.AlertLevel = RuleService.ResolveAlertLevel(
                assignmentReport, supplyReport, congestionReport, queueSnapshot);
            report.FocusText = BuildFocusText(report);
            report.AdviceText = RuleService.BuildCommandAdvice(
                assignmentReport, supplyReport, congestionReport);
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
        /// 安全读取任务私有字段。
        /// 仅用于诊断已有任务对象内部数据，不修改字段内容。
        /// </summary>
        /// <typeparam name="T">字段目标类型。</typeparam>
        /// <param name="target">目标对象。</param>
        /// <param name="fieldName">字段名。</param>
        /// <param name="value">读取到的字段值。</param>
        /// <returns>读取成功且类型匹配时返回 true。</returns>
        public static bool TryGetFieldValue<T>(object target, string fieldName, out T value)
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
                return string.Format("有 {0} 名工人接近停工。", assignment.CriticalWorkerCount);
            }

            if (supply != null && supply.CriticalWorkerCount > 0)
            {
                return string.Format("补给链出现危急工人 {0} 名。", supply.CriticalWorkerCount);
            }

            if (assignment != null && assignment.BlockedTaskCount > 0)
            {
                return string.Format(
                    "有 {0} 个等待任务被诊断为阻塞，主要原因是 {1}。",
                    assignment.BlockedTaskCount,
                    GetBlockReasonName(assignment.PrimaryBlockReason));
            }

            if (supply != null && supply.HasIssue)
            {
                return string.Format("补给存在问题：{0}。", WorkerSupplyTool.GetIssueName(supply.PrimaryIssue));
            }

            if (congestion != null &&
                (congestion.Level == WorkerTaskCongestionLevel.Busy ||
                congestion.Level == WorkerTaskCongestionLevel.Congested ||
                congestion.Level == WorkerTaskCongestionLevel.Critical))
            {
                return string.Format("任务队列处于 {0}。", WorkerTaskCongestionTool.GetLevelName(congestion.Level));
            }

            if (report.QueueSnapshot != null && report.QueueSnapshot.WaitingTaskCount > 0)
            {
                return string.Format("还有 {0} 个任务等待接取。", report.QueueSnapshot.WaitingTaskCount);
            }

            return "殖民地运行平稳。";
        }

        /// <summary>
        /// 构建殖民地指挥中心外部依赖上下文。
        /// </summary>
        /// <param name="workerMap">Worker ID 到 AWorker 实例的映射。</param>
        /// <returns>诊断上下文。</returns>
        private static ColonyDiagnosticContext BuildDiagnosticContext(
            Dictionary<long, AWorker> workerMap)
        {
            ColonyDiagnosticContext context = new ColonyDiagnosticContext();

            // 地图可达性查询 — 通过 ServiceLocator 解析 Domain 接口
            context.MapQuery = ServiceLocator.Get<IMapWalkabilityQuery>();

            // 任务开关检查
            context.IsTaskToggleEnabled = (workerId, taskType) =>
            {
                if (workerMap.TryGetValue(workerId, out AWorker worker) &&
                    WorkerConditionTool.TryGetWorkerData(worker, out AWorker.WorkerData workerData))
                {
                    return workerData.TaskToggle != null &&
                        workerData.TaskToggle.TryGetValue(taskType, out bool enabled) &&
                        enabled;
                }

                return false;
            };

            // 获取建造任务所需材料字典
            context.GetBuildNeeds = task =>
            {
                TryGetFieldValue(task, "needs", out Dictionary<int, ResourceInfo> needs);
                return needs;
            };

            // 检查能否满足材料需求（Worker 背包 + 仓库）
            context.CanFulfillMaterials = (workerId, needs) =>
            {
                if (needs == null || needs.Count == 0)
                {
                    return true;
                }

                if (workerMap.TryGetValue(workerId, out AWorker worker))
                {
                    if (worker.IsEnough(needs))
                    {
                        return true;
                    }

                    Dictionary<int, ResourceInfo> remaining = worker.GetRemaining(needs);
                    return ServiceLocator.TryGet(out InventoryManager inv) &&
                        inv.IsEnoughAndPreTake(worker, remaining);
                }

                return false;
            };

            // 获取搬运任务资源信息
            context.GetCarryResourceInfo = task =>
            {
                TryGetFieldValue(task, "resourceInfo", out ResourceInfo resourceInfo);
                return resourceInfo;
            };

            // 检查仓库是否有放置空间
            context.CanPlaceInInventory = (workerId, resourceInfo) =>
            {
                if (resourceInfo == null)
                {
                    return true;
                }

                if (ServiceLocator.TryGet(out InventoryManager inventoryMgr))
                {
                    if (workerMap.TryGetValue(workerId, out AWorker worker))
                    {
                        return inventoryMgr.IsEnoughAndPrePlace(worker, resourceInfo);
                    }

                    return inventoryMgr.IsEnoughAndPrePlace(null, resourceInfo);
                }

                return false;
            };

            // 检查指定位置是否有食物
            context.IsFoodAtPosition = pos =>
            {
                if (!ServiceLocator.TryGet(out InventoryManager inv))
                {
                    return false;
                }

                Vector3Int unityPos = new Vector3Int(pos.X, pos.Y, pos.Z);
                ResourceInfo ri = inv.GetResourceByPos(unityPos);
                if (ri == null || ri.Count <= 0)
                {
                    return false;
                }

                return ServiceLocator.TryGet(out ItemDataManager itemMgr) &&
                    itemMgr.IdToType(ri.Id) == AItem.ItemTypeEnum.Food;
            };

            // 检查是否可种植（有种子且有空闲农田）
            context.CanPlant = workerId =>
            {
                if (!ServiceLocator.TryGet(out InventoryManager inv) ||
                    inv.TypeToResource == null)
                {
                    return false;
                }

                if (!inv.TypeToResource.TryGetValue(
                    AItem.ItemTypeEnum.Seed,
                    out Dictionary<Vector3Int, ResourceInfo> seeds) ||
                    !HasPositiveResource(seeds))
                {
                    return false;
                }

                if (workerMap.TryGetValue(workerId, out AWorker worker))
                {
                    return ServiceLocator.TryGet(out FarmlandManager fm) &&
                        fm.IsEnoughAndPrePlant(worker, null) != default;
                }

                return false;
            };

            // 检查 Worker 是否有床位
            context.HasBed = workerId =>
            {
                if (workerMap.TryGetValue(workerId, out AWorker worker))
                {
                    return worker.BedItem != null;
                }

                return false;
            };

            // 获取任务绑定的 Worker ID
            context.GetBoundWorkerId = (task, fieldName) =>
            {
                if (TryGetFieldValue(task, fieldName, out AWorker boundWorker) && boundWorker != null)
                {
                    return (long)boundWorker.GetInstanceID();
                }

                return 0L;
            };

            // 将 AWorkerTask 的位置字段转换为 Domain GameGridPosition
            context.GetTaskTargetPosition = task =>
            {
                AWorkerTask workerTask = task as AWorkerTask;
                if (workerTask == null || workerTask.TargetMap == null)
                {
                    return default;
                }

                Vector3IntLAB v = workerTask.TargetMap;
                return new GameGridPosition(v.X, v.Y, v.Z);
            };

            // 将 AWorkerTask 的邻居位置列表转换为 Domain GameGridPosition 列表
            context.GetTaskNeighborPositions = task =>
            {
                List<GameGridPosition> result = new List<GameGridPosition>();
                AWorkerTask workerTask = task as AWorkerTask;
                if (workerTask != null && workerTask.AvailableNeighborPos != null)
                {
                    for (int i = 0; i < workerTask.AvailableNeighborPos.Count; i++)
                    {
                        Vector3IntLAB v = workerTask.AvailableNeighborPos[i];
                        if (v != null)
                        {
                            result.Add(new GameGridPosition(v.X, v.Y, v.Z));
                        }
                    }
                }

                return result;
            };

            return context;
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
        /// 生成 WorkerTaskAssignmentReport 的纯文本摘要（扩展方法，表现层）。
        /// </summary>
        public static string ToPlainText(this WorkerTaskAssignmentReport report)
        {
            if (report == null || !string.IsNullOrEmpty(report.ErrorMessage))
            {
                return report?.ErrorMessage ?? string.Empty;
            }

            StringBuilder builder = new StringBuilder(512);
            builder.AppendFormat(
                "人力: 总 {0} | 空闲 {1} | 忙碌 {2} | 临界 {3}",
                report.WorkerCount,
                report.IdleWorkerCount,
                report.BusyWorkerCount,
                report.CriticalWorkerCount);
            builder.AppendLine();
            builder.AppendFormat(
                "任务: 总 {0} | 等待 {1} | 进行中 {2} | 阻塞 {3}",
                report.TotalTaskCount,
                report.WaitingTaskCount,
                report.RunningTaskCount,
                report.BlockedTaskCount);
            builder.AppendLine();

            if (report.PrimaryBlockReason != WorkerTaskBlockReason.None)
            {
                builder.Append("主要阻塞: ")
                    .Append(GetBlockReasonName(report.PrimaryBlockReason))
                    .AppendLine();
            }

            for (int i = 0; i < Math.Min(report.ReasonSummaries.Count, 5); i++)
            {
                builder.Append("- ")
                    .Append(GetBlockReasonName(report.ReasonSummaries[i].Reason))
                    .Append(": ")
                    .Append(report.ReasonSummaries[i].Count)
                    .AppendLine();
            }

            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// 生成 WorkerTaskBlockDetail 的 HUD 展示行（扩展方法，表现层）。
        /// </summary>
        public static string ToDisplayLine(this WorkerTaskBlockDetail detail)
        {
            if (detail == null)
            {
                return string.Empty;
            }

            string color = GetBlockReasonRichColor(detail.Reason);
            string taskName = string.IsNullOrEmpty(detail.TaskName)
                ? WorkerTaskSummaryTool.GetTaskDisplayName(detail.TaskType)
                : detail.TaskName;
            return $"<color={color}>{WorkerTaskSummaryTool.GetTaskDisplayName(detail.TaskType)}</color> " +
                $"#{detail.TaskId} {taskName} {detail.TargetText}: " +
                $"{GetBlockReasonName(detail.Reason)}";
        }

    }
}
