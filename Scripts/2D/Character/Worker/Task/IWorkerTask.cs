namespace LAB2D
{
    /// <summary>
    /// Worker任务基
    /// </summary>
    public interface IWorkerTask
    {
        /// <summary>
        /// 执行任务
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <returns>是否成功</returns>
        bool Execute(Worker worker);

        /// <summary>
        /// 完成任务
        /// </summary>
        /// <param name="worker">Worer</param>
        void Finish(Worker worker);

        /// <summary>
        /// 是否满足前提条件（Build需要材料，Carry需要Inventory）
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <returns>是否</returns>
        bool IsCanWork(Worker worker);
    }
}