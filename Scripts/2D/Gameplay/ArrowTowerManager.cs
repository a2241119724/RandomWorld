namespace LAB2D.Gameplay
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 箭塔管理器 — 已建成箭塔周期索敌并自动射击（M2B）。
    /// 仿 TechManager：Tick 内 2s 节流扫描 BuildMap 找 IsComplete 的 ArrowTower 主格，
    /// 全塔统一开火节奏（无逐塔冷却状态）。伤害走技能直伤公式
    /// （DEF/10 减免 + 下限 1；Onwer=null —— 塔非 Character，
    /// ReduceHp 对 null attacker 安全：attacker?. 传播 + ASeekEnemy override 兼容）；
    /// 弹道复用 Bullet 粒子纯视觉（不设 AttackTags/Onwer/Damage，粒子碰撞只 Stop，无伤害无 NRE）。
    /// 塔本体数据在 BuildMap 存档，本类无持久状态，不入 ASingletonSaveData。
    /// </summary>
    public class ArrowTowerManager : Singleton<ArrowTowerManager>, ITickable
    {
        /// <summary>已建成箭塔重扫间隔（秒）。</summary>
        private const float BuildingScanInterval = 2f;

        /// <summary>全塔统一开火间隔（秒）。</summary>
        private const float FireInterval = 1.5f;

        /// <summary>索敌半径（世界单位）。</summary>
        private const float AttackRange = 7f;

        /// <summary>每箭基础伤害（技能公式 DEF 减免前）。</summary>
        private const float BaseDamage = 8f;

        /// <summary>弹道视觉速度。</summary>
        private const float BulletSpeed = 25f;

        internal static Func<BuildMap> BuildMapProvider { get; set; }
            = () => ServiceLocator.TryGet(out BuildMap bm) ? bm : null;

        private float buildingScanTimer;
        private float fireTimer;
        private int lastLoggedCount = -1;
        private readonly List<Vector3Int> towerCells = new List<Vector3Int>();

        /// <summary>已建成箭塔数量（缓存值，Tick 节流刷新）。</summary>
        public int ArrowTowerCount => this.towerCells.Count;

        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            this.buildingScanTimer += deltaTime;
            if (this.buildingScanTimer >= BuildingScanInterval)
            {
                this.buildingScanTimer = 0f;
                this.RescanTowers();
            }

            if (this.towerCells.Count == 0)
            {
                return;
            }

            this.fireTimer += deltaTime;
            if (this.fireTimer < FireInterval)
            {
                return;
            }

            this.fireTimer = 0f;
            TileMap tileMap = ServiceLocator.Get<TileMap>();
            if (tileMap == null)
            {
                return;
            }

            foreach (Vector3Int cell in this.towerCells)
            {
                this.TryFire(tileMap, cell);
            }
        }

        /// <summary>
        /// 立即重扫已建成箭塔主格（建造完成回调/Tick 节流共用）。
        /// 1×1 建筑无副格，IsComplete 即可射击。
        /// </summary>
        public void RescanTowers()
        {
            this.towerCells.Clear();
            BuildMap buildMap = BuildMapProvider();
            if (buildMap?.BuildMapDataLAB?.PosMap == null)
            {
                return;
            }

            foreach (KeyValuePair<Vector3IntLAB, BuildMap.BuildTileData> kv in buildMap.BuildMapDataLAB.PosMap)
            {
                BuildMap.BuildTileData tile = kv.Value;
                if (tile != null && tile.IsComplete && tile.Name == nameof(ArrowTower))
                {
                    this.towerCells.Add(new Vector3Int(kv.Key.X, kv.Key.Y, 0));
                }
            }

            if (this.towerCells.Count != this.lastLoggedCount)
            {
                this.lastLoggedCount = this.towerCells.Count;
                AWorkerTask.LogProvider(
                    $"[TowerDiag] 箭塔数量变化：{this.towerCells.Count}",
                    LogManager.LogLevelEnum.Debug);
            }
        }

        private void TryFire(TileMap tileMap, Vector3Int cell)
        {
            Vector3 towerPos = tileMap.MapPosToWorldPos(cell);

            // 网格最近邻查询（零分配，O(局部敌人数)，替代全列表扫描+取最近）；
            // Hp 复查为快照防御最后关口（网格是本帧重建时刻的快照，对齐原实时判活语义）
            AEnemy target = SkillTool.GetNearestEnemyInRadius(towerPos, AttackRange, out float nearestSqr);
            if (target == null || target.CharacterDataLAB == null || target.CharacterDataLAB.Hp <= 0)
            {
                return;
            }

            // 直伤公式与 SkillManager 一致：DEF/10 减免、下限 1；Onwer=null（塔非 Character）
            float finalDamage = BaseDamage;
            float defReduction = finalDamage * target.CharacterDataLAB.DEF / 10f;
            finalDamage -= defReduction;
            finalDamage = Math.Max(1f, finalDamage);
            target.ReduceHp(finalDamage, null, false);

            // 弹道纯视觉：不设 AttackTags/Onwer/Damage → OnParticleCollision 只 Stop，无碰撞伤害
            Vector3 offset = target.transform.position - towerPos;
            ParticleSystem ps = Core.GameServices.AttackEffectProvider(
                AttackEffectManager.EffectTypeEnum.Bullet, Mathf.Atan2(offset.y, offset.x));
            ps.transform.position = towerPos;
            AttackEffect ae = ps.GetComponent<AttackEffect>();
            if (ae != null)
            {
                ae.Speed = BulletSpeed;
            }

            ps.Play();

            AWorkerTask.LogProvider(
                $"[TowerDiag] 箭塔({cell.x},{cell.y}) 射击 {target.name} 距离{Mathf.Sqrt(nearestSqr):F1} 伤害{finalDamage:F1}",
                LogManager.LogLevelEnum.Trace);
        }
    }
}
