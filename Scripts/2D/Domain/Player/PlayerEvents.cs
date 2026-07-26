namespace LAB2D.Domain.Player
{
    using LAB2D.Domain.Common;

    /// <summary>
    /// 玩家攻击请求事件。
    /// Player.cs 检测到攻击输入后发布，ForegroundPanel/PlayerAttackHandler 订阅后执行武器攻击。
    /// </summary>
    public sealed class PlayerAttackRequestedEvent : IGameEvent
    {
        public long EntityId;
    }

    /// <summary>
    /// 玩家主动技能激活事件。
    /// Player.cs 检测到技能热键后发布，SkillManager 订阅后激活对应技能。
    /// </summary>
    public sealed class PlayerSkillActivatedEvent : IGameEvent
    {
        public long EntityId;
        public int SlotIndex;
    }
}
