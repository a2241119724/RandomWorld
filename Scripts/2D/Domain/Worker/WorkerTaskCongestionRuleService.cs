namespace LAB2D.Domain.Worker
{
    using LAB2D.Domain.Common;
    using LAB2D.Enum;
    /// <summary>
    /// 工人任务拥堵监控的纯算术规则。
    /// </summary>
    public sealed class WorkerTaskCongestionRuleService
    {
        /// <summary>繁忙等待任务阈值（等待任务数 >= 该值判定为繁忙）。</summary>
        public const int BusyWaitingTaskThreshold = 4;

        /// <summary>拥堵等待任务阈值（等待任务数 >= 该值判定为拥堵）。</summary>
        public const int CongestedWaitingTaskThreshold = 10;

        /// <summary>严重拥堵等待任务阈值（等待任务数 >= 该值判定为严重拥堵）。</summary>
        public const int CriticalWaitingTaskThreshold = 18;

        /// <summary>主积压任务类型的最少等待数量阈值。</summary>
        public const int DominantTaskWaitingThreshold = 3;

        /// <summary>主积压任务类型占等待任务的最小比例。</summary>
        public const float DominantTaskWaitingRatio = 0.5f;

        public float ClampRefreshInterval(float interval)
        {
            return MathHelper.ClampRefreshInterval(interval);
        }

        /// <summary>
        /// 根据等待任务数获取拥堵等级。
        /// </summary>
        /// <param name="waitingTaskCount">等待中的任务数量。</param>
        /// <returns>拥堵等级。</returns>
        public WorkerTaskCongestionLevel GetCongestionLevel(int waitingTaskCount)
        {
            if (waitingTaskCount <= 0)
            {
                return WorkerTaskCongestionLevel.None;
            }

            if (waitingTaskCount >= CriticalWaitingTaskThreshold)
            {
                return WorkerTaskCongestionLevel.Critical;
            }

            if (waitingTaskCount >= CongestedWaitingTaskThreshold)
            {
                return WorkerTaskCongestionLevel.Congested;
            }

            if (waitingTaskCount >= BusyWaitingTaskThreshold)
            {
                return WorkerTaskCongestionLevel.Busy;
            }

            return WorkerTaskCongestionLevel.Smooth;
        }

        /// <summary>
        /// 判断是否存在占据主导地位的任务积压类型。
        /// </summary>
        /// <param name="primaryWaitingCount">主要积压任务类型的等待数量。</param>
        /// <param name="totalWaitingCount">总等待任务数。</param>
        /// <returns>主类型等待数量和占比都达到阈值时返回 true。</returns>
        public bool HasDominantTaskType(int primaryWaitingCount, int totalWaitingCount)
        {
            if (primaryWaitingCount <= 0 || totalWaitingCount <= 0)
            {
                return false;
            }

            float ratio = (float)primaryWaitingCount / totalWaitingCount;
            return primaryWaitingCount >= DominantTaskWaitingThreshold &&
                ratio >= DominantTaskWaitingRatio;
        }
    }
}
