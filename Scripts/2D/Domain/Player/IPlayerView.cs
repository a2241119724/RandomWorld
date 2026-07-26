namespace LAB2D.Domain.Player
{
    using LAB2D.Domain.Common;

    /// <summary>
    /// 玩家表现层抽象。
    /// 隔离 Animator、Rigidbody2D、SpriteRenderer、Camera 等 Unity 表现组件，
    /// 使 Player.cs 的核心逻辑（输入采集、规则执行、事件发布）不再直接操作 Unity 表现对象。
    ///
    /// Unity 实现位于 <see cref="LAB2D.UnityAdapter.PlayerViewAdapter"/>。
    /// 测试中可替换为桩实现。
    /// </summary>
    public interface IPlayerView
    {
        /// <summary>
        /// 应用移动动画和物理速度。
        /// </summary>
        /// <param name="command">移动输入命令（方向、是否跑步）。</param>
        /// <param name="moveResult">移动计算结果（速度向量）。</param>
        void ApplyMoveAnimation(PlayerMoveCommand command, PlayerMoveResult moveResult);

        /// <summary>
        /// 应用待机动画（停止移动时的 idle 状态）。
        /// </summary>
        void ApplyIdleAnimation();

        /// <summary>
        /// 确保主摄像机和 minimap 摄像机跟随指定位置。
        /// 内部处理去重逻辑——仅在摄像机未绑定时更新。
        /// </summary>
        /// <param name="position">玩家当前位置。</param>
        void EnsureCameraFollow(GameVector2 position);

        /// <summary>
        /// 播放受击红色闪烁效果，0.2 秒后自动恢复原色。
        /// </summary>
        void PlayHitFlash();

        /// <summary>
        /// 每帧更新，用于驱动计时器相关的表现（受击闪烁恢复、边缘特效等）。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间。</param>
        void Tick(float deltaTime);

        /// <summary>
        /// 切换 2D / 2.5D 视角。
        /// </summary>
        /// <param name="is2_5D">true 表示切换到 2.5D 视角。</param>
        void TogglePerspective(bool is2_5D);
    }
}
