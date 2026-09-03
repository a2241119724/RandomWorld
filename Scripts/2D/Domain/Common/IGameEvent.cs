namespace LAB2D.Domain.Common
{
    using System.Collections.Generic;
    using LAB2D.Domain.TurnBattle;

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
    /// WorldPosX / WorldPosY 用于世界坐标定位，展示层通过这两个字段放置 UI。
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

    /// <summary>
    /// 回合制战斗开始时触发（玩家按 G 加入大世界 Worker vs Enemy 交战）。
    /// </summary>
    public sealed class TurnBattleStartedEvent : IGameEvent
    {
        /// <summary>参战单位 Id 列表（含玩家/Worker/Enemy）。</summary>
        public List<long> UnitIds = new List<long>();
    }

    /// <summary>
    /// 回合制战斗结束时触发 — 成就"战斗"类别与会话统计消费。
    /// </summary>
    public sealed class TurnBattleEndedEvent : IGameEvent
    {
        public TurnBattleResult Result;

        /// <summary>战斗持续回合数。</summary>
        public int Round;

        /// <summary>被击败的敌方单位 Id 列表（经验/掉落归因参考）。</summary>
        public List<long> DefeatedEnemyUnitIds = new List<long>();
    }
}
