namespace LAB2D.Domain.Player
{
    using LAB2D.Domain.Common;

    /// <summary>
    /// 玩家移动计算结果（纯数据）。
    /// 由 PlayerMovementService 产出，Player.Move() 消费后驱动 Rigidbody2D/Animator。
    /// </summary>
    public readonly struct PlayerMoveResult
    {
        public readonly GameVector2 Direction;
        public readonly float MoveSpeed;

        public PlayerMoveResult(GameVector2 direction, float moveSpeed)
        {
            this.Direction = direction;
            this.MoveSpeed = moveSpeed;
        }

        /// <summary>
        /// 在世界坐标中的速度向量 = Direction * MoveSpeed。
        /// </summary>
        public GameVector2 Velocity
        {
            get { return new GameVector2(this.Direction.X * this.MoveSpeed, this.Direction.Y * this.MoveSpeed); }
        }
    }

    /// <summary>
    /// 玩家移动速度纯计算服务。
    /// 将天气、波次奖励、跑步倍率等多层速度修正合并为单一 MoveSpeed 输出。
    /// 不依赖 UnityEngine，输入通过参数传入。
    /// </summary>
    public sealed class PlayerMovementService
    {
        private readonly PlayerMovementPolicy movementPolicy;

        public PlayerMovementService()
        {
            this.movementPolicy = new PlayerMovementPolicy();
        }

        /// <summary>
        /// 计算本次移动的最终速度和方向。
        /// </summary>
        /// <param name="baseMoveSpeed">角色基础移动速度</param>
        /// <param name="runSpeedMultiplier">跑步倍率（由 PlayerMovementPolicy 钳制后应用）</param>
        /// <param name="isRunning">是否按下跑步键</param>
        /// <param name="weatherMoveSpeedMultiplier">天气玩法的移动速度倍率</param>
        /// <param name="waveRewardMoveSpeedMultiplier">波次奖励的移动速度倍率</param>
        /// <param name="rawDirection">原始输入方向</param>
        /// <returns>移动计算结果</returns>
        public PlayerMoveResult CalculateMovement(
            float baseMoveSpeed,
            float runSpeedMultiplier,
            bool isRunning,
            float weatherMoveSpeedMultiplier,
            float waveRewardMoveSpeedMultiplier,
            GameVector2 rawDirection)
        {
            float speed = baseMoveSpeed * weatherMoveSpeedMultiplier * waveRewardMoveSpeedMultiplier;
            speed = this.movementPolicy.ApplyRunMultiplier(speed, isRunning, runSpeedMultiplier);
            return new PlayerMoveResult(rawDirection, speed);
        }
    }
}
