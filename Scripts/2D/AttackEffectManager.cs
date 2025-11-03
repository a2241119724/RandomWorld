namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 攻击效果管理器
    /// </summary>
    public class AttackEffectManager : Singleton<AttackEffectManager>
    {
        private Dictionary<EffectType, List<ParticleSystem>> activeEffects = new ();
        private Dictionary<EffectType, Queue<ParticleSystem>> availableEffects = new ();

        public AttackEffectManager()
        {
            foreach (EffectType type in Enum.GetValues(typeof(EffectType)))
            {
                this.availableEffects.Add(type, new Queue<ParticleSystem>());
                this.activeEffects.Add(type, new List<ParticleSystem>());
            }
        }

        /// <summary>
        /// 攻击特效类型
        /// </summary>
        public enum EffectType
        {
            /// <summary>
            /// 刀光
            /// </summary>
            KnifeLight,

            /// <summary>
            /// 无
            /// </summary>
            None,
        }

        /// <summary>
        /// 获取攻击效果
        /// </summary>
        /// <param name="name">特效名称</param>
        /// <param name="deg">特效方向，与x正半轴夹角的弧度值</param>
        /// <returns>特效</returns>
        public ParticleSystem GetEffect(EffectType name, float deg)
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

            if (!this.availableEffects.ContainsKey(name) || this.availableEffects[name].Count == 0)
            {
                this.availableEffects[name].Enqueue(ResourceManager.Instance.Instantiate(name.ToString() + "Effect").GetComponent<ParticleSystem>());
            }

            ParticleSystem ps = this.availableEffects[name].Dequeue();
            this.activeEffects[name].Add(ps);

            // 设置角度
            ps.transform.rotation = Quaternion.Euler(0, 0, deg * Mathf.Rad2Deg);
            ParticleSystem.MainModule main = ps.main;
            main.startRotation = deg;
            return ps.GetComponent<ParticleSystem>();
        }
    }
}
