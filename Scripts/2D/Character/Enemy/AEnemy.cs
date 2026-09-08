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
        /// 感知遮挡层（只算静态墙/建筑）：物理射线判定"目标被挡"用。
        /// 不含 Player/Worker 层——角色不挡视线，否则被围一圈 Worker 即被致盲
        /// （丢目标回 Seek → 寻路被围卡死不动，2026-09-07 用户报告"Enemy 卡住"根因）。
        /// 动态遮挡语义由网格 LOS 覆盖（SkillTool.HasLineOfSight 挡墙/地形）。
        /// 惰性初始化：LayerMask.GetMask 禁止在静态字段初始化器中调用
        /// （实例化触发 .cctor 即抛 UnityException，见 bug-fixes.md 2026-09-07）。
        /// </summary>
        private static LayerMask senseOcclusionLayers;
        private static bool senseOcclusionLayersReady;

        private static LayerMask SenseOcclusionLayers
        {
            get
            {
                if (!senseOcclusionLayersReady)
                {
                    senseOcclusionLayers = LayerMask.GetMask("Tile", "BuildTile");
                    senseOcclusionLayersReady = true;
                }

                return senseOcclusionLayers;
            }
        }

        /// <summary>
        /// 刷新头顶血条 — 回合制战斗结束写回 Hp 后调用
        /// （正常路径 ReduceHp 内部刷新，快照写回绕过了这些管线）。
        /// </summary>
        public void RefreshStatusBar()
        {
            this.statusBar?.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
        }

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
            // 攻击范围视觉与实际攻击门槛同源：min(AttackRange, 武器实际射程 AttackReach)
            //（SeekEnemy 拿剑 →1.5）——原固定 AttackRange=4，实际门槛降到 1.5 后扇形
            // 仍显示 4 格 = "视觉范围不管用了"（2026-09-08 用户报告）。无武器敌人
            //（CommonEnemy）回退 AttackRange，与其攻击判定 dist≤AttackRange 一致。
            float attackVisualRange = Mathf.Min(
                enemyData.AttackRange,
                this.CharacterDataLAB.Weapon?.AttackReach ?? enemyData.AttackRange);
            this.AttackRange = LAB2D.Tool.Tool.DrawSectorSolid(10, attackVisualRange, new Color32(255, 0, 0, 50), this.transform);
            this.SightRange = LAB2D.Tool.Tool.DrawSectorSolid(enemyData.SightAngle, enemyData.SightRange, new Color32(0, 255, 0, 50), this.transform);

            // Hp 位于 prefab 内置的 HeadUI 子树下（FindHeadChild 含旧层级兜底）
            Transform hpTrans = this.FindHeadChild("Hp");
            this.statusBar = hpTrans != null ? hpTrans.GetComponent<CharacterStatusUI>() : null;
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
                if (!SkillTool.HasLineOfSight(this.transform.position, target.position))
                {
                    isFind = false;
                }
            }

            // 物理射线检测作为补充（检测动态障碍物等）——只查静态遮挡层：
            // 若含 Player/Worker 层，被一圈 Worker 围住即判定目标不可见 → 致盲卡死
            if (isFind)
            {
                Vector3 direction = target.position - this.transform.position;
                this.raycastHit2D = Physics2D.Raycast(this.transform.position, direction, enemyData.SightRange, SenseOcclusionLayers); // (源,方向,距离,层级)

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
            // 死亡溶解：MPB 写起始时刻，Custom/Sprite-Lit-Flash 内 _Time 自驱 0.45s 颗粒吞噬 + 灼烧亮边；
            // 各子类 Dead 状态 DeadTime=0.5s 后销毁对象，与溶解时长自然衔接（IsAliveEnemy 按 Hp 判活，溶解期间不计入）
            ApplyDissolve(this.spriteRenderer);
        }

        /// <summary>
        /// 敌人数据
        /// </summary>
        [Serializable]
        public class EnemyData : CharacterData
        {
            /// <summary>
            /// 是否 Boss 波实体（生成时由 WaveBossRewardManager 按 IsBossEnemySpawn 判定写入；
            /// 旧档缺省 false = 普通敌人，零迁移）。击杀统计/成就据此区分 Boss 计数.
            /// </summary>
            public bool IsBoss;

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

            /// <summary>
            /// 敌人种类 Id（WaveEnemyKind）。入档防读档换种：prefab 按此 Id 选择。
            /// 旧档反序列化缺省 0=Common。
            /// </summary>
            public int EnemyKindId;
        }
    }
}
