namespace LAB2D
{
    /// <summary>
    /// Marker interface for all game events.
    /// Events are the output of domain services that are consumed by presentation/Unity adapters.
    /// </summary>
    public interface IGameEvent
    {
    }

    /// <summary>
    /// Fired when a player moves.
    /// Consumed by PlayerViewAdapter for Rigidbody2D/Transform/Animator updates.
    /// </summary>
    public sealed class PlayerMovedEvent : IGameEvent
    {
        public long EntityId;
        public GameVector2 Position;
        public GameVector2 Direction;
        public bool IsRunning;
    }

    /// <summary>
    /// Fired when a character takes damage.
    /// Consumed by DamageUI, FloatingTextManager, SpriteRenderer flash effects.
    /// </summary>
    public sealed class CharacterDamagedEvent : IGameEvent
    {
        public long TargetId;
        public long AttackerId;
        public float Damage;
        public bool IsCritical;
        public float RemainingHp;
    }

    /// <summary>
    /// Fired when inventory state changes.
    /// Consumed by ItemMap, ItemInfoUI, WorkerTaskManager bridge.
    /// </summary>
    public sealed class InventoryChangedEvent : IGameEvent
    {
        public GameGridPosition Position;
        public int ItemId;
        public int Count;
    }

    /// <summary>
    /// Fired when a wave state changes.
    /// Consumed by WaveHUD, WaveEventFeedback.
    /// </summary>
    public sealed class WaveStateChangedEvent : IGameEvent
    {
        public int CurrentWaveIndex;
        public int TotalWavesCompleted;
        public bool IsWaveActive;
        public bool IsResting;
        public float DifficultyScale;
    }

    /// <summary>
    /// Fired when a worker task is assigned or completed.
    /// Consumed by WorkerTaskHUD, ColonyCommandCenterHUD.
    /// </summary>
    /// <summary>
    /// Fired when a worker task is assigned or completed.
    /// Uses int to avoid transitive UnityEngine dependency through AWorkerTask.WorkerTaskTypeEnum.
    /// Cast to AWorkerTask.WorkerTaskTypeEnum at the adapter layer.
    /// </summary>
    public sealed class WorkerTaskEvent : IGameEvent
    {
        public long TaskId;
        public int WorkerInstanceId;
        public int TaskType;
        public bool IsCompleted;
    }
}
