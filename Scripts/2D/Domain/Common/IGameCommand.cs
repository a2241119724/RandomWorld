namespace LAB2D.Domain.Common
{
    /// <summary>
    /// 所有游戏命令的标记接口。
    /// 命令代表玩家或系统的意图，应由领域服务处理。
    /// </summary>
    public interface IGameCommand
    {
    }

    /// <summary>
    /// 玩家移动意图命令。
    /// 由输入适配器创建，由PlayerMovementPolicy消费。
    /// </summary>
    public sealed class PlayerMoveCommand : IGameCommand
    {
        public long EntityId;
        public GameVector2 Direction;
        public bool IsRunning;
        public float DeltaTime;
    }

    /// <summary>
    /// 玩家攻击意图命令。
    /// </summary>
    public sealed class PlayerAttackCommand : IGameCommand
    {
        public long EntityId;
    }

    /// <summary>
    /// 激活技能意图命令。
    /// </summary>
    public sealed class ActivateSkillCommand : IGameCommand
    {
        public long EntityId;
        public int SlotIndex;
    }
}
