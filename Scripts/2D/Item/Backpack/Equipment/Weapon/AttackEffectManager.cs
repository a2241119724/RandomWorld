namespace LAB2D.Item.Backpack.Equipment.Weapon
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 攻击效果管理器
    /// </summary>
    public class AttackEffectManager : Singleton<AttackEffectManager>
    {
        private Dictionary<EffectTypeEnum, List<ParticleSystem>> activeEffects = new ();
        private Dictionary<EffectTypeEnum, Queue<ParticleSystem>> availableEffects = new ();

        public AttackEffectManager()
        {
            foreach (EffectTypeEnum type in Enum.GetValues(typeof(EffectTypeEnum)))
            {
                this.availableEffects.Add(type, new Queue<ParticleSystem>());
                this.activeEffects.Add(type, new List<ParticleSystem>());
            }
        }

        /// <summary>
        /// 攻击特效类型
        /// </summary>
        public enum EffectTypeEnum
        {
            /// <summary>
            /// 刀光
            /// </summary>
            KnifeLight,

            /// <summary>
            /// 子弹
            /// </summary>
            Bullet,

            /// <summary>
            /// 跟踪子弹
            /// </summary>
            TraceBullet,
        }

        /// <summary>
        /// 获取攻击效果
        /// </summary>
        /// <param name="name">特效名称</param>
        /// <param name="rad">特效方向，与x正半轴夹角的弧度值</param>
        /// <returns>特效</returns>
        public ParticleSystem GetEffect(EffectTypeEnum name, float rad)
        {
            // 惰性检测
            foreach (var particleSystem in this.activeEffects[name].ToArray())
            {
                if (!particleSystem.IsAlive())
                {
                    this.availableEffects[name].Enqueue(particleSystem);
                    this.activeEffects[name].Remove(particleSystem);
                }
            }

            // 如果没有可用的粒子系统, 则创建一个
            if (!this.availableEffects.ContainsKey(name) || this.availableEffects[name].Count == 0)
            {
                this.availableEffects[name].Enqueue(ServiceLocator.Get<ResourceManager>().Instantiate(name.ToString() + "Effect").GetComponent<ParticleSystem>());
            }

            ParticleSystem ps = this.availableEffects[name].Dequeue();
            this.activeEffects[name].Add(ps);

            // 设置角度
            ps.transform.rotation = Quaternion.Euler(0, 0, rad * MathHelper.Rad2Deg);
            ParticleSystem.MainModule main = ps.main;
            main.startRotation = rad;
            return ps.GetComponent<ParticleSystem>();
        }
    }
}
