namespace LAB2D.Domain.Worker
{
    /// <summary>
    /// 工人任务疲劳和进度的纯算术。
    /// </summary>
    public sealed class WorkerTaskProgressService
    {
        public float ApplyTiredCost(float currentTired, float deltaTime, float tiredCostPerSecond)
        {
            float nextTired = currentTired - (deltaTime * tiredCostPerSecond);
            return nextTired < 0.0f ? 0.0f : nextTired;
        }

        public WorkerTaskProgressResult AdvanceProgress(
            float currentProgress,
            float maxProgress,
            float deltaTime,
            float progressMultiplier)
        {
            float nextProgress = currentProgress + (deltaTime * progressMultiplier);
            bool completed = nextProgress > maxProgress;
            return new WorkerTaskProgressResult(completed ? 0.0f : nextProgress, completed);
        }

        public float GetProgressRatio(float currentProgress, float maxProgress)
        {
            if (maxProgress <= 0.0f)
            {
                return 1.0f;
            }

            return currentProgress / maxProgress;
        }
    }

    public readonly struct WorkerTaskProgressResult
    {
        public WorkerTaskProgressResult(float currentProgress, bool completed)
        {
            this.CurrentProgress = currentProgress;
            this.Completed = completed;
        }

        public float CurrentProgress { get; }

        public bool Completed { get; }
    }
}
