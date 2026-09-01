namespace LAB2D.Character.Enemy
{
    using LAB2D;
    using LAB2D.UI.Character;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class AEnemy : Character
    {
        protected const float ChangeTarget = 5.0f; // 超过当前时间被攻击会被吸引仇恨（延长专注期避免频繁扭头）
        protected CharacterStatusUI statusBar; // 记录实例化血条
        private RaycastHit2D raycastHit2D; // 射线射中返回的结果

        /// <summary>
        /// 获取角色朝向的方向
        /// </summary>
        public virtual Vector3 Direction { get; set; }

        /// <summary>
        /// 视觉
        /// </summary>
        public GameObject SightRange { get; set; }

        /// <summary>
        /// 攻击觉
        /// </summary>
        public GameObject AttackRange { get; set; }

        /// <summary>
        /// 敌人攻击目标, 扫描到附近的人
        /// </summary>
        [HideInInspector]
        public Character Target { get; set; } // 打击目标

        public override void Awake()
        {
            base.Awake();
            this.AttackLayers = LayerMask.GetMask("Tile", "BuildTile", LayerConstant.PLAYER_LAYER, LayerConstant.WORKER_LAYER);
            this.AttackTags = new List<string>
            {
                "Player",
                "Worker",
            };
            // 根据当前波次难度缩放敌人基础属性（波次越后敌人越强）
            float difficultyScale = 1.0f;
            try
            {
                var waveManager = Core.ServiceLocator.Get<Gameplay.WaveManager>();
                if (waveManager != null)
                {
                    difficultyScale = Mathf.Max(1.0f, waveManager.CurrentDifficultyScale);
                }
            }
            catch { /* WaveManager 未注册时保持基础属性 */ }
            this.basicAttribute = new Attribute(
                5.0f * difficultyScale,
                5.0f * difficultyScale,
                2.0f * difficultyScale,
                2.0f * difficultyScale,
                Mathf.Min(1.0f, 0.05f * difficultyScale),
                1.0f * difficultyScale,
                1.0f * difficultyScale,
                1.0f * difficultyScale);

            this.CharacterDataLAB = new EnemyData();
            this.CharacterDataLAB.Character = this;
        }

        /// <inheritdoc/>
        public override void Start()
        {
            base.Start();
            this.MoveSpeed = 2f;
            EnemyData enemyData = this.CharacterDataLAB as EnemyData;

            // 画视觉,听觉,攻击范围
            // LAB2D.Tool.Tool.DrawSectorSolid(360, enemyData.SoundRange, new Color32(0, 0, 255, 50), this.transform);
            this.AttackRange = LAB2D.Tool.Tool.DrawSectorSolid(10, enemyData.AttackRange, new Color32(255, 0, 0, 50), this.transform);
            this.SightRange = LAB2D.Tool.Tool.DrawSectorSolid(enemyData.SightAngle, enemyData.SightRange, new Color32(0, 255, 0, 50), this.transform);

            this.statusBar = this.transform.Find("Hp").GetComponent<CharacterStatusUI>();
            if (this.statusBar == null)
            {
                AWorkerTask.LogProvider("statusBar Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            // 更新敌人身体状况
            this.statusBar.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
        }

        /// <summary>
        /// 敌人通过视觉和听觉感知周围是否有target.
        /// </summary>
        /// <param name="target">目标.</param>
        /// <returns>周围是否有目标.</returns>
        public bool SenseNearby(Transform target)
        {
            if (target == null)
            {
                AWorkerTask.LogProvider("target is null!!!", LogManager.LogLevelEnum.Error);
                return false;
            }

            EnemyData enemyData = this.CharacterDataLAB as EnemyData;

            // 使用平方距离比较，避免 Vector3.Distance 中的 sqrt 运算
            float sqrDist = (target.position - this.transform.position).sqrMagnitude;

            // 如果玩家与敌人的距离小于敌人的听觉距离(一周)
            // 判断是否听到附近有玩家
            bool isFind = sqrDist < enemyData.SoundRange * enemyData.SoundRange;

            // 如果玩家与敌人的距离小于敌人的视觉距离(前方扇形)
            if (sqrDist < enemyData.SightRange * enemyData.SightRange && !isFind)
            {
                // 计算玩家是否在敌人的视角内
                Vector3 direction = target.position - this.transform.position;
                float degree = Vector3.Angle(direction, this.Direction);
                if (degree < enemyData.SightAngle / 2 && degree > -enemyData.SightAngle / 2)
                {
                    isFind = true;
                }
            }

            if (isFind)
            {
                // 网格级视线遮挡检查：Bresenham 遍历两点之间的网格，
                // 检测建造中/已完成的墙壁和不可通过地形，弥补物理射线无法检测无碰撞体瓦片的缺陷。
                if (!this.HasGridLineOfSight(target.position))
                {
                    isFind = false;
                }
            }

            // 物理射线检测作为补充（检测动态障碍物等）
            if (isFind)
            {
                Vector3 direction = target.position - this.transform.position;
                this.raycastHit2D = Physics2D.Raycast(this.transform.position, direction, enemyData.SightRange, this.AttackLayers); // (源,方向,距离,层级)

                // 如果有碰撞体并且不是目标，是障碍物
                if (this.raycastHit2D.collider != null && this.raycastHit2D.transform != target)
                {
                    isFind = false;
                }
            }

            return isFind;
        }

        /// <inheritdoc/>
        public override void Attack()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 网格级视线检查 — 使用 Bresenham 算法遍历两点之间的网格，
        /// 检测是否有不可通过的 BuildTile 或 TerrainTile 阻挡视线。
        /// 物理射线无法检测建造中（无碰撞体）的墙壁和门洞，此方法弥补该缺陷。
        /// </summary>
        /// <param name="targetWorldPos">目标世界坐标。</param>
        /// <returns>两点之间是否有畅通的网格视线。</returns>
        private bool HasGridLineOfSight(Vector3 targetWorldPos)
        {
            var tileMap = Core.ServiceLocator.Get<TileMap>();
            var buildMap = Core.ServiceLocator.Get<BuildMap>();
            if (tileMap == null)
            {
                return true; // 无 TileMap 时不做网格检测，回退到物理射线
            }

            Vector3Int fromMap = tileMap.WorldPosToMapPos(this.transform.position);
            Vector3Int toMap = tileMap.WorldPosToMapPos(targetWorldPos);

            int x0 = fromMap.x;
            int y0 = fromMap.y;
            int x1 = toMap.x;
            int y1 = toMap.y;

            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int x = x0;
            int y = y0;
            int steps = 0;
            const int maxSteps = 200; // 安全上限，防止无限循环

            while (steps++ < maxSteps)
            {
                // 跳过起点和终点（起点是敌人自身，终点是目标）
                if ((x != x0 || y != y0) && (x != x1 || y != y1))
                {
                    Vector3Int checkPos = new Vector3Int(x, y, 0);

                    // 检查 BuildMap：已完成且不可通过的建筑瓦片阻挡视线
                    if (buildMap != null && !buildMap.IsCanReach(checkPos))
                    {
                        return false;
                    }

                    // 检查 TileMap：不可通过的地形瓦片阻挡视线
                    if (!tileMap.IsCanReach(checkPos))
                    {
                        return false;
                    }
                }

                if (x == x1 && y == y1)
                {
                    break;
                }

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string targetInfo = this.Target != null
                ? $"Target: {this.Target.name}\n"
                : string.Empty;
            return base.ToString()
                + targetInfo
                + $"碰撞计数: {this.collisionBugDetector.ColliderCount}\n"
                + $"听觉范围: {((EnemyData)this.CharacterDataLAB).SoundRange}\n"
                + $"视觉范围: {((EnemyData)this.CharacterDataLAB).SightRange}/{((EnemyData)this.CharacterDataLAB).SightAngle}°\n"
                + $"攻击范围: {((EnemyData)this.CharacterDataLAB).AttackRange}\n"
                + $"子弹速度: {((EnemyData)this.CharacterDataLAB).BulletSpeed}\n";
        }

        /// <summary>
        /// 当前状态机状态的中文描述（ItemInfo 面板显示用）。
        /// 各敌人子类按自己的状态枚举实现。
        /// </summary>
        public virtual string GetStateLabel() => string.Empty;

        /// <inheritdoc/>
        protected override void Death()
        {
            base.Death();
        }

        /// <summary>
        /// 敌人数据
        /// </summary>
        [Serializable]
        public class EnemyData : CharacterData
        {
            /// <summary>
            /// 敌人攻击范围.
            /// </summary>
            public float AttackRange = 4.0f;

            /// <summary>
            /// 听觉距离.
            /// </summary>
            public float SoundRange = 4.0f;

            /// <summary>
            /// 视野距离.
            /// </summary>
            public float SightRange = 10.0f;

            /// <summary>
            /// 视野角度.
            /// </summary>
            public float SightAngle = 60.0f;

            /// <summary>
            /// 发射子弹的速度.
            /// </summary>
            public float BulletSpeed = 50.0f;
        }
    }
}
