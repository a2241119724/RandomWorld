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
            // 惰性检测：倒序原地清理（原 ToArray 快照 + List.Remove 是每次攻击一次数组分配 + O(n²) 移动）
            List<ParticleSystem> active = this.activeEffects[name];
            for (int i = active.Count - 1; i >= 0; i--)
            {
                ParticleSystem candidate = active[i];
                if (candidate == null)
                {
                    // 已销毁（场景重载等）：只移出 active，不入队复用（避免 Dequeue 出 null 延迟 NRE）
                    active.RemoveAt(i);
                }
                else if (!candidate.IsAlive())
                {
                    this.availableEffects[name].Enqueue(candidate);
                    active.RemoveAt(i);
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
