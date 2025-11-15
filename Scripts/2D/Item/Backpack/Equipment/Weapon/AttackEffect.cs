namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;
    using static UnityEngine.ParticleSystem;

    /// <summary>
    /// 所有武器的攻击特挂载
    /// </summary>
    public class AttackEffect : MonoBehaviour
    {
        private ParticleSystem ps;

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
                Character c = other.GetComponent<Character>();
                if (c is ACommonEnemy)
                {
                    ACommonEnemy e = c as ACommonEnemy;
                    e.Target = this.Onwer;
                }

                other.GetComponent<Character>().ReduceHp(this.Damage, this.IsCRT);
            }
        }
    }
}
