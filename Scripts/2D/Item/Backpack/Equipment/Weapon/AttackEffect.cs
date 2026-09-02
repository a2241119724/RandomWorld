namespace LAB2D.Item.Backpack.Equipment.Weapon
{
    using LAB2D;
    using Character = LAB2D.Character.Character;
    using System.Collections.Generic;
    using UnityEngine;
    using static UnityEngine.ParticleSystem;

    /// <summary>
    /// 所有武器的攻击特挂载
    /// </summary>
    public class AttackEffect : MonoBehaviour
    {
        protected ParticleSystem ps;

        /// <summary>
        /// 是否暴击
        /// </summary>
        public bool IsCRT { get; set; } = false;

        /// <summary>
        /// 特效速度
        /// </summary>
        public float Speed
        {
            set
            {
                VelocityOverLifetimeModule volm = this.ps.velocityOverLifetime;
                volm.x = value;
            }
        }

        /// <summary>
        /// 攻击的层级
        /// </summary>
        public LayerMask AttackLayers
        {
            set
            {
                CollisionModule cm = this.ps.collision;
                cm.collidesWith = value;
            }
        }

        /// <summary>
        /// 攻击的标签
        /// </summary>
        public List<string> AttackTags { get; set; }

        /// <summary>
        /// 攻击的伤害
        /// </summary>
        public float Damage { get; set; }

        /// <summary>
        /// 攻击的拥有者
        /// </summary>
        public Character Onwer { get; set; }

        public void Awake()
        {
            this.ps = this.GetComponent<ParticleSystem>();
        }

        private void OnParticleCollision(GameObject other)
        {
            if (this.AttackTags != null && this.AttackTags.Count > 0 && this.AttackTags.Contains(other.tag))
            {
                // 重置攻击者攻击状态
                this.Onwer.ResetState();
                Character c = other.GetComponent<Character>();
                if (c is ACommonEnemy commonEnemy)
                {
                    commonEnemy.Target = this.Onwer;
                }

                c.ReduceHp(this.Damage, this.Onwer, this.IsCRT);
            }
            else if (this.Onwer is AEnemy && other.GetComponent<Character>() == null)
            {
                // 妖兽啃墙：敌人子弹命中非角色碰撞体（建筑/地形层）→ 对命中格造成建筑伤害。
                // 玩家/Worker 子弹不拆建筑（防误拆自家）；敌人互射（tag 不匹配但有 Character）也不算。
                this.DamageBuildingAt(other);
            }

            // 子弹碰到任何碰撞体（墙壁或角色）后停止特效
            this.ps.Stop();
        }

        /// <summary>
        /// 对子弹命中点所在的建筑格造成伤害（M1.3 建筑伤害通路入口）。
        /// </summary>
        private void DamageBuildingAt(GameObject other)
        {
            if (this.ps.GetCollisionEvents(other, this.collisionEvents) <= 0)
            {
                return;
            }

            var buildMap = Core.ServiceLocator.Get<BuildMap>();
            if (buildMap == null)
            {
                return;
            }

            Vector3Int hitCell = AWorkerTask.TileMapWorldToMapProvider(this.collisionEvents[0].intersection);
            buildMap.DamageBuilding(hitCell, this.Damage, this.Onwer);
        }

        /// <summary>粒子碰撞事件缓存（GetCollisionEvents 要求复用列表，避免每次分配）。</summary>
        private readonly List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
    }
}
