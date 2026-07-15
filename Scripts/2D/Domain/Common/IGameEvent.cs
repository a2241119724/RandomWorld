namespace LAB2D.Domain.Common
{
    using UnityEngine;

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
    /// TargetTransform 用于 UI 父化，确保 DamageUI 跟随角色位置。
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
        public Transform TargetTransform;
    }

    /// <summary>
    /// 玩家状态变化时触发。
    /// 由 PlayerStatusUI 消费以更新 HUD 显示。
    /// </summary>
    public sealed class PlayerStatusChangedEvent : IGameEvent
    {
        public float Hp;
        public float MaxHp;
        public int Mp;
        public int MaxMp;
        public int Level;
        public int CurExperience;
        public int MaxExperience;
    }
}
