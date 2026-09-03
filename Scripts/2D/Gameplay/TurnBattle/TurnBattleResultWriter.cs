namespace LAB2D.Gameplay.TurnBattle
{
    using System.Collections.Generic;
    using LAB2D.Character;
    using LAB2D.Character.Enemy;
    using LAB2D.Character.Player;
    using LAB2D.Character.Worker;
    using LAB2D.Domain.TurnBattle;
    using UnityEngine;

    /// <summary>
    /// 战斗结果写回器 — 结束时把快照一次性写回大世界（面板关闭前调用，timeScale=0 下纯 C# 链安全）。
    ///
    /// 写回规则（用户决策 2026-09-03）：
    /// - 存活单位：Hp/Mp = 快照值，Player 走 RefreshUI，Worker/Enemy 刷头顶血条；
    /// - Worker 倒下 = 重伤退场：Hp 写 1，不触发 WorkerDeadState 永久死亡管线；
    /// - 玩家倒下 = 判负：绕无敌帧走 Player.Death 管线（Hp 钳 1 + 经验惩罚 + 复活倒计时）；
    /// - Enemy 倒下：LastAttacker=玩家归因经验，ReduceHp 走标准 Dead 管线（经验/掉落/统计）；
    /// - 逃跑成功：存活照常写回 + 玩家 2s 无敌帧（防恢复后被仍仇恨的 Enemy 秒咬）。
    /// </summary>
    public static class TurnBattleResultWriter
    {
        /// <summary>逃跑成功后的玩家无敌帧时长（秒）。</summary>
        public const float FleeInvincibilitySeconds = 2f;

        public static void WriteBack(
            TurnBattleState state,
            IReadOnlyDictionary<long, Character> worldUnits,
            Player player,
            AEnemy defeatKiller)
        {
            foreach (TurnBattleUnit unit in state.Units)
            {
                if (!worldUnits.TryGetValue(unit.UnitId, out Character character) || character == null)
                {
                    continue;
                }

                if (unit.IsPlayerUnit)
                {
                    WriteBackPlayer(unit, player, state.Result, defeatKiller);
                }
                else if (character is AEnemy enemy)
                {
                    WriteBackEnemy(unit, enemy, player);
                }
                else if (character is AWorker worker)
                {
                    WriteBackWorker(unit, worker);
                }
            }
        }

        private static void WriteBackPlayer(TurnBattleUnit unit, Player player, TurnBattleResult result, AEnemy defeatKiller)
        {
            if (player == null)
            {
                return;
            }

            if (unit.IsDown)
            {
                // 判负：绕无敌帧走标准死亡管线（内部 Hp 钳 1 + 惩罚 + 复活倒计时）
                player.DeathByTurnBattle(defeatKiller);
                return;
            }

            player.CharacterDataLAB.Hp = Mathf.Clamp(unit.Hp, 0f, player.CharacterDataLAB.MaxHp);
            player.CharacterDataLAB.Mp = Mathf.Clamp(unit.Mp, 0, player.CharacterDataLAB.MaxMp);
            player.RefreshUI();

            if (result == TurnBattleResult.Escaped)
            {
                player.GrantInvincibility(FleeInvincibilitySeconds);
            }
        }

        private static void WriteBackEnemy(TurnBattleUnit unit, AEnemy enemy, Player player)
        {
            if (unit.IsDown)
            {
                if (enemy == null || enemy.CharacterDataLAB == null || enemy.CharacterDataLAB.Hp <= 0f)
                {
                    return;
                }

                // 经验/掉落归因玩家，走标准 Dead 管线（延迟掉落由 timeScale 恢复后自然执行）
                enemy.LastAttacker = player;
                enemy.ReduceHp(enemy.CharacterDataLAB.Hp + 1f, player);
                return;
            }

            enemy.CharacterDataLAB.Hp = Mathf.Clamp(unit.Hp, 0f, enemy.CharacterDataLAB.MaxHp);
            enemy.CharacterDataLAB.Mp = Mathf.Clamp(unit.Mp, 0, enemy.CharacterDataLAB.MaxMp);
            enemy.RefreshStatusBar();
        }

        private static void WriteBackWorker(TurnBattleUnit unit, AWorker worker)
        {
            if (worker == null || worker.CharacterDataLAB == null)
            {
                return;
            }

            if (unit.IsDown)
            {
                // 重伤退场：1 HP 写回，不触发永久死亡管线
                worker.CharacterDataLAB.Hp = 1f;
                worker.RefreshStatusBar();
                return;
            }

            worker.CharacterDataLAB.Hp = Mathf.Clamp(unit.Hp, 0f, worker.CharacterDataLAB.MaxHp);
            worker.CharacterDataLAB.Mp = Mathf.Clamp(unit.Mp, 0, worker.CharacterDataLAB.MaxMp);
            worker.RefreshStatusBar();
        }
    }
}
