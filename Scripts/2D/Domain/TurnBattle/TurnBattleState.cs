namespace LAB2D.Domain.TurnBattle
{
    using System.Collections.Generic;

    /// <summary>
    /// 回合制战斗阶段。
    /// </summary>
    public enum TurnBattlePhase
    {
        /// <summary>等待玩家选择行动。</summary>
        AwaitPlayerAction,

        /// <summary>行动结算与演出播放中（UI 锁输入）。</summary>
        Resolving,

        /// <summary>战斗结束（Result 已定，待写回与关闭面板）。</summary>
        BattleOver,
    }

    /// <summary>
    /// 回合制战斗结果。
    /// </summary>
    public enum TurnBattleResult
    {
        /// <summary>敌方全灭。</summary>
        Victory,

        /// <summary>玩家 HP 归零（立即判负 — 用户决策：Worker 倒下不判负）。</summary>
        Defeat,

        /// <summary>逃跑成功（战斗中伤害保留写回）。</summary>
        Escaped,
    }

    /// <summary>
    /// 回合制战斗状态 — 整场战斗的唯一可变状态，由规则引擎推进、UI 读取演出。
    /// </summary>
    public sealed class TurnBattleState
    {
        /// <summary>全部参战单位（双方混编，按 UnitId 检索）。</summary>
        public List<TurnBattleUnit> Units = new List<TurnBattleUnit>();

        /// <summary>当前回合数（从 1 起）。</summary>
        public int Round = 1;

        /// <summary>战斗阶段。</summary>
        public TurnBattlePhase Phase = TurnBattlePhase.AwaitPlayerAction;

        /// <summary>战斗结果（BattleOver 时有效）。</summary>
        public TurnBattleResult Result;

        /// <summary>逃跑已失败次数（每次失败 +15% 成功率，鼓励果断）。</summary>
        public int FleeAttemptCount;

        /// <summary>玩家单位（每场战斗恰一个，StartBattle 校验）。</summary>
        public TurnBattleUnit PlayerUnit
        {
            get
            {
                foreach (TurnBattleUnit unit in this.Units)
                {
                    if (unit.IsPlayerUnit)
                    {
                        return unit;
                    }
                }

                return null;
            }
        }

        /// <summary>存活我方单位。</summary>
        public List<TurnBattleUnit> AliveAllies
        {
            get { return this.Units.FindAll(u => u.Faction == TurnBattleFaction.Ally && u.CanAct); }
        }

        /// <summary>存活敌方单位。</summary>
        public List<TurnBattleUnit> AliveEnemies
        {
            get { return this.Units.FindAll(u => u.Faction == TurnBattleFaction.Enemy && u.CanAct); }
        }

        /// <summary>按 UnitId 检索（找不到返回 null）。</summary>
        public TurnBattleUnit FindUnit(long unitId)
        {
            foreach (TurnBattleUnit unit in this.Units)
            {
                if (unit.UnitId == unitId)
                {
                    return unit;
                }
            }

            return null;
        }
    }
}
