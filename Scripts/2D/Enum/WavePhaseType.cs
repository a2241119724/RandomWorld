namespace LAB2D.Enum
{
    /// <summary>
    /// 波次体验阶段类型。
    /// 用于 HUD、奖励面板和运行时管理器表达当前波次系统处于普通战斗、Boss、奖励选择或休息等阶段。
    /// 后续允许追加新阶段，但不得重命名已有值，避免破坏 UI 和报告工具的状态判断。
    /// </summary>
    public enum WavePhaseType
    {
        /// <summary>波次尚未启动或系统空闲。</summary>
        Idle,

        /// <summary>普通波次战斗中。</summary>
        NormalWave,

        /// <summary>Boss 波或精英波战斗中。</summary>
        BossWave,

        /// <summary>波间奖励等待玩家选择。</summary>
        RewardSelection,

        /// <summary>波间休息倒计时中。</summary>
        Resting,

        /// <summary>有限波次全部完成。</summary>
        Completed,
    }
}
