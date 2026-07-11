namespace LAB2D
{
    /// <summary>
    /// Marker interface for all game commands.
    /// Commands represent player or system intents that should be processed by domain services.
    /// </summary>
    public interface IGameCommand
    {
    }

    /// <summary>
    /// Player movement intent command.
    /// Created by input adapters and consumed by PlayerMovementPolicy.
    /// </summary>
    public sealed class PlayerMoveCommand : IGameCommand
    {
        public long EntityId;
        public GameVector2 Direction;
        public bool IsRunning;
        public float DeltaTime;
    }

    /// <summary>
    /// Player attack intent command.
    /// </summary>
    public sealed class PlayerAttackCommand : IGameCommand
    {
        public long EntityId;
    }

    /// <summary>
    /// Activate skill intent command.
    /// </summary>
    public sealed class ActivateSkillCommand : IGameCommand
    {
        public long EntityId;
        public int SlotIndex;
    }
}
