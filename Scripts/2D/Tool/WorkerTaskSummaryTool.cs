namespace LAB2D.Tool
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Gameplay;
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// 工人任务队列统计与文案工具。
    /// 只做只读聚合、中文名称转换和 HUD 文案生成，不新增任务、不改变优先级、不访问 Scene、Prefab、存档、Photon 或 AssetBundle。
    /// </summary>
    public static class WorkerTaskSummaryTool
    {
        /// <summary>
        /// 根据 WorkerTaskManager 的任务队列构建只读快照。
        /// </summary>
        /// <param name="priorityTaskGroups">按优先级组织的任务字典；字典值为 true 表示任务进行中。</param>
        /// <returns>任务队列快照；输入为空时返回空快照。</returns>
        public static WorkerTaskQueueSnapshot BuildSnapshot(
            IReadOnlyList<Dictionary<AWorkerTask, bool>> priorityTaskGroups)
        {
            Dictionary<AWorkerTask.WorkerTaskTypeEnum, int> totalByType =
                CreateEmptyTaskTypeCountMap();
            Dictionary<AWorkerTask.WorkerTaskTypeEnum, int> runningByType =
                CreateEmptyTaskTypeCountMap();

            int totalTaskCount = 0;
            int runningTaskCount = 0;
            if (priorityTaskGroups != null)
            {
                foreach (Dictionary<AWorkerTask, bool> taskGroup in priorityTaskGroups)
                {
                    if (taskGroup == null)
                    {
                        continue;
                    }

                    foreach (KeyValuePair<AWorkerTask, bool> taskPair in taskGroup)
                    {
                        AWorkerTask task = taskPair.Key;
                        if (task == null)
                        {
                            continue;
                        }

                        AWorkerTask.WorkerTaskTypeEnum taskType = task.TaskType;
                        EnsureTaskType(totalByType, taskType);
                        EnsureTaskType(runningByType, taskType);

                        totalByType[taskType]++;
                        totalTaskCount++;
                        if (taskPair.Value)
                        {
                            runningByType[taskType]++;
                            runningTaskCount++;
                        }
                    }
                }
            }

            List<WorkerTaskTypeSummary> summaries = new ();
            foreach (AWorkerTask.WorkerTaskTypeEnum taskType in Enum.GetValues(typeof(AWorkerTask.WorkerTaskTypeEnum)))
            {
                int totalCount = totalByType.ContainsKey(taskType) ? totalByType[taskType] : 0;
                int runningCount = runningByType.ContainsKey(taskType) ? runningByType[taskType] : 0;
                summaries.Add(new WorkerTaskTypeSummary(taskType, totalCount, runningCount));
            }

            return new WorkerTaskQueueSnapshot(totalTaskCount, runningTaskCount, summaries);
        }

        /// <summary>
        /// 生成适合 HUD 展示的 RichText 文案。
        /// </summary>
        /// <param name="snapshot">任务队列快照。</param>
        /// <returns>HUD 文案；快照为空或无任务时返回默认文案。</returns>
        public static string BuildHudText(WorkerTaskQueueSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return WorkerTaskHudConstant.ManagerUnavailableText;
            }

            if (!snapshot.HasTask)
            {
                return WorkerTaskHudConstant.NoTaskText;
            }

            StringBuilder sb = new ();
            sb.Append("<color=").Append(PixelUITheme.RichSky).Append(">任务队列</color> ");
            sb.Append("总数 ").Append(snapshot.TotalTaskCount);
            sb.Append(" | 等待 ").Append(snapshot.WaitingTaskCount);
            sb.Append(" | 进行中 ").Append(snapshot.RunningTaskCount);
            sb.Append(" | 压力 <color=").Append(GetPressureRichColor(snapshot.WaitingTaskCount)).Append(">");
            sb.Append(GetPressureLabel(snapshot.WaitingTaskCount)).Append("</color>");
            sb.AppendLine();

            int lineCount = 0;
            foreach (WorkerTaskTypeSummary summary in snapshot.TaskTypeSummaries)
            {
                if (!summary.HasTask)
                {
                    continue;
                }

                sb.AppendLine(BuildHudTaskTypeLine(summary));
                lineCount++;
                if (lineCount >= WorkerTaskHudConstant.MaxHudTaskTypeLines)
                {
                    break;
                }
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// 生成适合 Editor 弹窗或日志展示的纯文本摘要。
        /// </summary>
        /// <param name="snapshot">任务队列快照。</param>
        /// <returns>不包含 RichText 标签的摘要。</returns>
        public static string BuildPlainText(WorkerTaskQueueSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return WorkerTaskHudConstant.ManagerUnavailableText;
            }

            if (!snapshot.HasTask)
            {
                return WorkerTaskHudConstant.NoTaskText;
            }

            StringBuilder sb = new ();
            sb.AppendFormat(
                "任务队列: 总数 {0} | 等待 {1} | 进行中 {2} | 压力 {3}",
                snapshot.TotalTaskCount,
                snapshot.WaitingTaskCount,
                snapshot.RunningTaskCount,
                GetPressureLabel(snapshot.WaitingTaskCount));
            sb.AppendLine();

            foreach (WorkerTaskTypeSummary summary in snapshot.TaskTypeSummaries)
            {
                if (!summary.HasTask)
                {
                    continue;
                }

                sb.AppendFormat(
                    "{0}: 总数 {1} | 等待 {2} | 进行中 {3}",
                    GetTaskDisplayName(summary.TaskType),
                    summary.TotalCount,
                    summary.WaitingCount,
                    summary.RunningCount);
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// 获取任务类型中文显示名。
        /// </summary>
        /// <param name="taskType">任务类型。</param>
        /// <returns>适合 HUD 和日志展示的中文名。</returns>
        public static string GetTaskDisplayName(AWorkerTask.WorkerTaskTypeEnum taskType)
        {
            switch (taskType)
            {
                case AWorkerTask.WorkerTaskTypeEnum.Build:
                    return "建造";
                case AWorkerTask.WorkerTaskTypeEnum.Carry:
                    return "搬运";
                case AWorkerTask.WorkerTaskTypeEnum.Gather:
                    return "采集";
                case AWorkerTask.WorkerTaskTypeEnum.Eat:
                    return "吃饭";
                case AWorkerTask.WorkerTaskTypeEnum.Exercise:
                    return "锻炼";
                case AWorkerTask.WorkerTaskTypeEnum.Wear:
                    return "穿戴";
                case AWorkerTask.WorkerTaskTypeEnum.Sleep:
                    return "睡觉";
                case AWorkerTask.WorkerTaskTypeEnum.Plant:
                    return "种植";
                default:
                    return taskType.ToString();
            }
        }

        /// <summary>
        /// 获取任务队列压力标签。
        /// </summary>
        /// <param name="waitingTaskCount">等待中任务数量。</param>
        /// <returns>压力中文标签。</returns>
        public static string GetPressureLabel(int waitingTaskCount)
        {
            if (waitingTaskCount >= WorkerTaskHudConstant.HighWaitingTaskThreshold)
            {
                return "拥堵";
            }

            if (waitingTaskCount >= WorkerTaskHudConstant.MediumWaitingTaskThreshold)
            {
                return "繁忙";
            }

            return "平稳";
        }

        /// <summary>
        /// 生成单个任务类型的 HUD 行。
        /// </summary>
        /// <param name="summary">任务类型统计。</param>
        /// <returns>适合 HUD 展示的一行 RichText 文案。</returns>
        private static string BuildHudTaskTypeLine(WorkerTaskTypeSummary summary)
        {
            string color = summary.WaitingCount > 0 ? PixelUITheme.RichGold : PixelUITheme.RichMint;
            return $"<color={color}>{GetTaskDisplayName(summary.TaskType)}</color> " +
                $"总 {summary.TotalCount} | 等待 {summary.WaitingCount} | 进行 {summary.RunningCount}";
        }

        /// <summary>
        /// 获取任务压力 RichText 颜色。
        /// </summary>
        /// <param name="waitingTaskCount">等待中任务数量。</param>
        /// <returns>HTML 颜色字符串。</returns>
        private static string GetPressureRichColor(int waitingTaskCount)
        {
            if (waitingTaskCount >= WorkerTaskHudConstant.HighWaitingTaskThreshold)
            {
                return PixelUITheme.RichCoral;
            }

            if (waitingTaskCount >= WorkerTaskHudConstant.MediumWaitingTaskThreshold)
            {
                return PixelUITheme.RichGold;
            }

            return PixelUITheme.RichMint;
        }

        /// <summary>
        /// 创建包含所有已知任务类型的计数字典。
        /// </summary>
        /// <returns>计数字典。</returns>
        private static Dictionary<AWorkerTask.WorkerTaskTypeEnum, int> CreateEmptyTaskTypeCountMap()
        {
            Dictionary<AWorkerTask.WorkerTaskTypeEnum, int> result = new ();
            foreach (AWorkerTask.WorkerTaskTypeEnum taskType in Enum.GetValues(typeof(AWorkerTask.WorkerTaskTypeEnum)))
            {
                result[taskType] = 0;
            }

            return result;
        }

        /// <summary>
        /// 确保扩展任务类型不会因字典缺项导致统计失败。
        /// </summary>
        /// <param name="counts">计数字典。</param>
        /// <param name="taskType">任务类型。</param>
        private static void EnsureTaskType(
            Dictionary<AWorkerTask.WorkerTaskTypeEnum, int> counts,
            AWorkerTask.WorkerTaskTypeEnum taskType)
        {
            if (!counts.ContainsKey(taskType))
            {
                counts[taskType] = 0;
            }
        }
    }
}
