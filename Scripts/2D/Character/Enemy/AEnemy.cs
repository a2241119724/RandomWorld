namespace LAB2D.Character.Enemy
{
    using LAB2D;
    using LAB2D.UI.Character;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class AEnemy : Character
    {
        protected const float ChangeTarget = 3.0f; // 超过当前时间被攻击会被吸引仇恨
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
            this.AttackLayers = LayerMask.GetMask("Tile", LayerConstant.PLAYER_LAYER, LayerConstant.WORKER_LAYER);
            this.AttackTags = new List<string>
            {
                "Player",
                "Worker",
            };
            this.basicAttribute = new Attribute(5.0f, 5.0f, 2.0f, 2.0f, 0.05f, 1.0f, 1.0f, 1.0f);
            this.CharacterDataLAB = new EnemyData();
            this.CharacterDataLAB.Character = this;
        }

        /// <inheritdoc/>
        public override void Start()
        {
            base.Start();
            this.MoveSpeed = 5f;
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
                // 判断玩家和敌人之间是否存在遮挡物
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
