namespace LAB2D.Domain.Player
{
    /// <summary>
    /// 玩家专属速度规则的纯移动算术。
    /// </summary>
    public sealed class PlayerMovementPolicy
    {
        public float ClampRunSpeedMultiplier(float multiplier)
        {
            return multiplier < 1.0f ? 1.0f : multiplier;
        }

        public float ApplyRunMultiplier(float baseSpeed, bool isRunning, float runSpeedMultiplier)
        {
            return isRunning
                ? baseSpeed * this.ClampRunSpeedMultiplier(runSpeedMultiplier)
                : baseSpeed;
        }
    }
}
