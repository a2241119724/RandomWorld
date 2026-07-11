namespace LAB2D.Domain.Common
{
    using LAB2D;
    /// <summary>
    /// 所有游戏事件的标记接口。
    /// 事件是领域服务的输出，由展示层/Unity适配器消费。
    /// </summary>
    public interface IGameEvent
    {
    }

    /// <summary>
    /// 玩家移动时触发。
    /// 由PlayerViewAdapter消费，用于Rigidbody2D/Transform/Animator更新。
    /// </summary>
    public sealed class PlayerMovedEvent : IGameEvent
    {
        public long EntityId;
        public GameVector2 Position;
        public GameVector2 Direction;
        public bool IsRunning;
    }

    /// <summary>
    /// 角色受到伤害时触发。
    /// 由DamageUI、FloatingTextManager、SpriteRenderer闪烁效果消费。
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
    /// 库存状态变更时触发。
    /// 由ItemMap、ItemInfoUI、WorkerTaskManager桥接消费。
    /// </summary>
    public sealed class InventoryChangedEvent : IGameEvent
    {
        public GameGridPosition Position;
        public int ItemId;
        public int Count;
    }

    /// <summary>
    /// 波次状态变更时触发。
    /// 由WaveHUD、WaveEventFeedback消费。
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
    /// Worker任务被分配或完成时触发。
    /// 由WorkerTaskHUD、ColonyCommandCenterHUD消费。
    /// </summary>
    /// <summary>
    /// Worker任务被分配或完成时触发。
    /// 使用int类型以避免通过AWorkerTask.WorkerTaskTypeEnum产生对UnityEngine的传递依赖。
    /// 在适配器层转换为AWorkerTask.WorkerTaskTypeEnum。
    /// </summary>
    public sealed class WorkerTaskEvent : IGameEvent
    {
        public long TaskId;
        public int WorkerInstanceId;
        public int TaskType;
        public bool IsCompleted;
    }
}
