namespace LAB2D
{
    /// <summary>
    /// Worker 任务等待或无法接取的诊断原因。
    /// 用于统一表达任务开关、饥饿、可达性、补给、仓库和任务绑定等阻塞来源，可被指挥中心 HUD、Tip、Editor 菜单和后续教程目标复用。
    /// 后续允许追加更细原因，但不得删除或重命名已有值，避免破坏报告聚合与 UI 文案。
    /// </summary>
    public enum WorkerTaskBlockReason
    {
        /// <summary>
        /// 无阻塞：任务只是正常等待或即将被 Worker 接取。
        /// </summary>
        None,

        /// <summary>
        /// WorkerTaskManager 或任务队列尚未初始化。
        /// </summary>
        ManagerUnavailable,

        /// <summary>
        /// 当前没有可扫描的 Worker。
        /// </summary>
        NoWorker,

        /// <summary>
        /// 所有 Worker 都已有任务，暂时无人可接新任务。
        /// </summary>
        WorkerBusy,

        /// <summary>
        /// 可用 Worker 关闭了该任务类型的任务开关。
        /// </summary>
        TaskToggleDisabled,

        /// <summary>
        /// 可用 Worker 饥饿值过低，不能接非吃饭任务。
        /// </summary>
        WorkerHungry,

        /// <summary>
        /// 任务目标附近没有可达工作点。
        /// </summary>
        TargetUnreachable,

        /// <summary>
        /// 建造或生产所需材料不足。
        /// </summary>
        MissingMaterial,

        /// <summary>
        /// 仓库没有可放置空间，搬运任务无法接取。
        /// </summary>
        InventoryFull,

        /// <summary>
        /// 吃饭任务目标位置没有可用食物。
        /// </summary>
        FoodUnavailable,

        /// <summary>
        /// 疲劳 Worker 缺少床位或床位绑定。
        /// </summary>
        MissingBed,

        /// <summary>
        /// 任务绑定的指定 Worker 不可用。
        /// </summary>
        BoundWorkerUnavailable,

        /// <summary>
        /// Worker 当前状态尚未满足该任务的专属条件，例如未疲劳、不需要吃饭或任务目标不匹配。
        /// </summary>
        WorkerNotReady,

        /// <summary>
        /// 种植任务缺少可用种子。
        /// </summary>
        SeedUnavailable,

        /// <summary>
        /// 种植任务缺少可种植农田。
        /// </summary>
        FarmlandUnavailable,

        /// <summary>
        /// 任务拥有额外专属条件，当前只读诊断无法安全细分。
        /// </summary>
        TaskSpecificCondition,

        /// <summary>
        /// 诊断过程中出现异常，已降级为安全提示。
        /// </summary>
        UnknownError,
    }
}
