namespace LAB2D.Enum
{
    /// <summary>
    /// 工人补给缺口类型。
    /// 用于统一表达食物、床位、饥饿、疲劳和临界停工等补给提示状态，可被 HUD、Tip、统计和后续任务目标复用。
    /// 后续允许追加更细的补给问题类型，但不得删除或重命名已有值，避免破坏提示和统计逻辑。
    /// </summary>
    public enum WorkerSupplyIssueType
    {
        /// <summary>
        /// 无补给缺口，当前不需要向玩家提示补给问题。
        /// </summary>
        None,

        /// <summary>
        /// 食物不足，仓库食物恢复量无法覆盖当前低饥饿工人的补给缺口。
        /// </summary>
        FoodShortage,

        /// <summary>
        /// 床位不足或未绑定，疲劳工人无法通过睡眠稳定恢复。
        /// </summary>
        BedShortage,

        /// <summary>
        /// 工人处于饥饿风险，但当前食物总量未必不足。
        /// </summary>
        HungryWorker,

        /// <summary>
        /// 工人处于疲劳风险，但当前床位总量未必不足。
        /// </summary>
        TiredWorker,

        /// <summary>
        /// 工人已接近停工临界状态，需要优先处理补给或休息。
        /// </summary>
        CriticalWorker,
    }
}
