namespace LAB2D.Gameplay.TurnBattle
{
    using System.Collections.Generic;
    using LAB2D.Character;
    using LAB2D.Character.Enemy;
    using LAB2D.Character.Worker;
    using UnityEngine;

    /// <summary>
    /// 一场大世界交战 — 连通分量聚合出的 Worker vs Enemy 对峙组。
    /// 玩家按 G 加入后，Workers 自动成为我方阵容、Enemies 成为敌方阵容。
    /// </summary>
    public sealed class BattleEncounter
    {
        /// <summary>交战中的 Worker（加入后为我方阵容）。</summary>
        public readonly List<AWorker> Workers = new List<AWorker>();

        /// <summary>交战中的 Enemy。</summary>
        public readonly List<AEnemy> Enemies = new List<AEnemy>();

        /// <summary>成员世界坐标均值（提示与最近遭遇计算用）。</summary>
        public Vector2 Center
        {
            get
            {
                if (this.Workers.Count == 0 && this.Enemies.Count == 0)
                {
                    return Vector2.zero;
                }

                Vector2 sum = Vector2.zero;
                int count = 0;
                foreach (AWorker worker in this.Workers)
                {
                    if (worker != null)
                    {
                        sum += (Vector2)worker.transform.position;
                        count++;
                    }
                }

                foreach (AEnemy enemy in this.Enemies)
                {
                    if (enemy != null)
                    {
                        sum += (Vector2)enemy.transform.position;
                        count++;
                    }
                }

                return count > 0 ? sum / count : Vector2.zero;
            }
        }

        /// <summary>双方是否都有存活成员（可开战的最低条件）。</summary>
        public bool HasAliveOnBothSides
        {
            get
            {
                bool anyWorker = false;
                foreach (AWorker worker in this.Workers)
                {
                    if (worker != null && worker.CharacterDataLAB != null && worker.CharacterDataLAB.Hp > 0f)
                    {
                        anyWorker = true;
                        break;
                    }
                }

                if (!anyWorker)
                {
                    return false;
                }

                foreach (AEnemy enemy in this.Enemies)
                {
                    if (enemy != null && enemy.CharacterDataLAB != null && enemy.CharacterDataLAB.Hp > 0f)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
