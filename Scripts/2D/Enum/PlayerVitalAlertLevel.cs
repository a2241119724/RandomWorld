namespace LAB2D.Enum
{
    /// <summary>
    /// 玩家生命危险提示等级。
    /// 用于统一表达本地玩家血量安全、受伤、濒危和复活等待状态，可被 Tip、HUD、Editor 菜单和后续任务目标系统复用。
    /// 后续允许追加更细等级，但不得删除或重命名已有值，避免破坏报告签名与 UI 文案。
    /// </summary>
    public enum PlayerVitalAlertLevel
    {
        /// <summary>
        /// 安全：血量比例高于预警阈值，不需要主动打断玩家。
        /// </summary>
        Safe,

        /// <summary>
        /// 受伤：血量已低于预警阈值，建议玩家拉开距离或准备恢复。
        /// </summary>
        Wounded,

        /// <summary>
        /// 濒危：血量已低于危急阈值，需要玩家优先保命。
        /// </summary>
        Critical,

        /// <summary>
        /// 复活等待：玩家死亡惩罚流程正在倒计时。
        /// </summary>
        Respawning,
    }
}
