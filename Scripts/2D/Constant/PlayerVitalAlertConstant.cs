namespace LAB2D
{
    /// <summary>
    /// 玩家生命危险提示常量。
    /// 集中维护 F019 的刷新节奏、Tip 冷却、血量阈值、菜单路径和默认文案；修改后需要验证低血量提示频率和玩家状态 UI 展示是否一致。
    /// </summary>
    public static class PlayerVitalAlertConstant
    {
        /// <summary>
        /// 候选编号，用于报告、日志和节点命名。
        /// </summary>
        public const string CandidateId = "F019";

        /// <summary>
        /// Editor 菜单根路径。
        /// 仅供 Editor 脚本复用，运行时代码不会引用 UnityEditor。
        /// </summary>
        public const string MenuRoot = "工具/玩家生命提示/";

        /// <summary>
        /// 玩家生命监控刷新间隔。
        /// 调低会更实时，但会增加只读血量检查和字符串构建频率。
        /// </summary>
        public const float MonitorRefreshInterval = 0.25f;

        /// <summary>
        /// 低血量 Tip 冷却时间。
        /// 避免玩家长期低血量时反复刷屏；濒危升级提示会优先显示。
        /// </summary>
        public const float TipCooldownSeconds = 8.0f;

        /// <summary>
        /// 血量低于该比例时进入受伤提示。
        /// 只影响提示文案，不改变血量、伤害、死亡惩罚或恢复数值。
        /// </summary>
        public const float WarningRatio = 0.35f;

        /// <summary>
        /// 血量低于该比例时进入濒危提示。
        /// 只影响提示文案，不改变战斗判定。
        /// </summary>
        public const float CriticalRatio = 0.18f;

        /// <summary>
        /// 血量恢复到该比例以上时允许显示恢复提示。
        /// 避免刚脱离低血量阈值就频繁弹出恢复文案。
        /// </summary>
        public const float RecoveryRatio = 0.60f;

        /// <summary>
        /// 玩家尚未初始化时的安全降级文案。
        /// </summary>
        public const string PlayerUnavailableText = "玩家生命提示: 本地玩家未初始化";

        /// <summary>
        /// 玩家名称为空时的默认展示名。
        /// </summary>
        public const string DefaultPlayerName = "玩家";

        /// <summary>
        /// 生命摘要标题。
        /// </summary>
        public const string SummaryTitle = "玩家生命";

        /// <summary>
        /// 建议标签文案。
        /// </summary>
        public const string AdviceLabel = "建议";

        /// <summary>
        /// 玩家数据不可读取时的安全降级文案。
        /// </summary>
        public const string PlayerDataUnavailableText = "玩家生命提示: 玩家数据不可读取";

        /// <summary>
        /// 生命提示扫描异常前缀。
        /// </summary>
        public const string ScanFailedPrefix = "玩家生命提示扫描失败: ";

        /// <summary>
        /// 安全状态建议文案。
        /// </summary>
        public const string SafeAdviceText = "保持当前节奏，继续观察周围敌人。";

        /// <summary>
        /// 受伤状态建议文案。
        /// </summary>
        public const string WoundedAdviceText = "生命偏低，建议拉开距离或准备恢复。";

        /// <summary>
        /// 濒危状态建议文案。
        /// </summary>
        public const string CriticalAdviceText = "生命濒危，优先脱战、走位或使用恢复手段。";

        /// <summary>
        /// 复活等待建议文案。
        /// </summary>
        public const string RespawningAdviceText = "正在等待复活，倒计时结束后会恢复部分生命。";

        /// <summary>
        /// 恢复提示文案。
        /// </summary>
        public const string RecoveredTipText = "生命状态已恢复，继续推进。";

        /// <summary>
        /// 生命提示调试日志前缀。
        /// </summary>
        public const string LogPrefix = "[PlayerVitalAlert]";
    }
}
