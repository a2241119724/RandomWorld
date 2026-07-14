namespace LAB2D.Domain.Common
{
    /// <summary>
    /// Worker 任务信息接口 — 为 Domain 层暴露任务的基本属性，
    /// 消除对 Character 层 AWorkerTask 具体类型的依赖。
    /// </summary>
    public interface IWorkerTaskInfo
    {
        /// <summary>任务类型。</summary>
        LAB2D.Enum.WorkerTaskType TaskType { get; }

        /// <summary>任务唯一 ID。</summary>
        long TaskId { get; }

        /// <summary>任务名称。</summary>
        string Name { get; }
    }
}
