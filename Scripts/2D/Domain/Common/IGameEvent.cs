namespace LAB2D.Domain.Common
{
    /// <summary>
    /// 所有游戏事件的标记接口。
    /// 事件是领域服务的输出，由展示层/Unity适配器消费。
    ///
    /// 注意：EventBus 是备用机制。当前大部分系统间通信通过 C# event 或 ServiceLocator 完成。
    /// 新增事件类型前请确认确实需要发布-订阅解耦，避免增加死代码。
    /// </summary>
    public interface IGameEvent
    {
    }

    /// <summary>
    /// 角色受到伤害时触发。
    /// 由 CharacterDamageUIPresenter 消费以生成伤害浮动文字和受击特效。
    /// </summary>
    public sealed class CharacterDamagedEvent : IGameEvent
    {
        public long TargetId;
        public long AttackerId;
        public float Damage;
        public bool IsCritical;
        public bool IsCombo;
        public float RemainingHp;
        public float WorldPosX;
        public float WorldPosY;
    }
}
