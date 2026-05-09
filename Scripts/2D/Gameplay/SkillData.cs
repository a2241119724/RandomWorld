namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 技能运行时数据模型。
    /// 定义单个主动技能的全部属性：类型、效果、冷却、法力消耗、等级等。
    /// 当前为纯代码驱动（非 ScriptableObject），后续可扩展为 SO 配置。
    /// </summary>
    [Serializable]
    public class SkillData
    {
        /// <summary>技能唯一标识ID，如 "skill_whirlwind"</summary>
        public string SkillId;

        /// <summary>技能显示名称</summary>
        public string SkillName;

        /// <summary>技能描述文本</summary>
        public string Description;

        /// <summary>技能释放类型</summary>
        public SkillType SkillType;

        /// <summary>技能效果类型</summary>
        public SkillEffectType EffectType;

        /// <summary>基础法力消耗</summary>
        public int ManaCost;

        /// <summary>基础冷却时间（秒）</summary>
        public float BaseCooldown;

        /// <summary>伤害/效果倍率（基于玩家属性计算）</summary>
        public float EffectMultiplier;

        /// <summary>AOE 半径（仅 SelfAOE 类型使用）</summary>
        public float AoeRadius;

        /// <summary>Buff 持续时间（仅 SelfBuff 类型使用，秒）</summary>
        public float BuffDuration;

        /// <summary>当前技能等级（1-MaxSkillLevel）</summary>
        public int Level;

        /// <summary>技能在HUD中的槽位索引（0-3）</summary>
        public int SlotIndex;

        /// <summary>上次激活时间（Time.time），用于冷却计算</summary>
        public float LastActivateTime;

        /// <summary>Buff效果结束时间（仅 SelfBuff 类型使用，Time.time）</summary>
        public float BuffEndTime;

        /// <summary>
        /// 当前实际冷却时间（基于等级计算）
        /// </summary>
        public float CurrentCooldown
        {
            get
            {
                return SkillTool.CalculateSkillCooldown(this.BaseCooldown, this.Level);
            }
        }

        /// <summary>
        /// 剩余冷却时间（秒），0 表示冷却就绪
        /// </summary>
        public float RemainingCooldown
        {
            get
            {
                if (this.LastActivateTime <= 0f)
                {
                    return 0f;
                }

                float elapsed = Time.time - this.LastActivateTime;
                float remaining = this.CurrentCooldown - elapsed;
                return remaining > 0f ? remaining : 0f;
            }
        }

        /// <summary>
        /// 技能是否处于冷却就绪状态
        /// </summary>
        public bool IsReady
        {
            get
            {
                return this.RemainingCooldown <= 0f;
            }
        }

        /// <summary>
        /// Buff 是否仍处于激活状态（仅 SelfBuff 类型）
        /// </summary>
        public bool IsBuffActive
        {
            get
            {
                return this.EffectType == SkillEffectType.AttackBuff
                       && this.BuffEndTime > 0f
                       && Time.time < this.BuffEndTime;
            }
        }

        /// <summary>
        /// 当前 Buff 攻击力倍率（仅在 Buff 激活时有效）
        /// </summary>
        public float CurrentBuffMultiplier
        {
            get
            {
                return this.IsBuffActive
                    ? SkillTool.CalculateBuffMultiplier(this.EffectMultiplier, this.Level)
                    : 1.0f;
            }
        }

        /// <summary>
        /// 技能是否已达最高等级
        /// </summary>
        public bool IsMaxLevel
        {
            get
            {
                return this.Level >= SkillConstant.MaxSkillLevel;
            }
        }

        /// <summary>
        /// 创建旋风斩技能的默认数据实例
        /// </summary>
        public static SkillData CreateWhirlwind()
        {
            return new SkillData
            {
                SkillId = SkillConstant.SkillWhirlwind,
                SkillName = SkillConstant.DefaultSkillNameWhirlwind,
                Description = SkillConstant.DefaultSkillDescWhirlwind,
                SkillType = SkillType.SelfAOE,
                EffectType = SkillEffectType.PhysicalDamage,
                ManaCost = SkillConstant.WhirlwindManaCost,
                BaseCooldown = SkillConstant.WhirlwindCooldown,
                EffectMultiplier = SkillConstant.WhirlwindDamageMultiplier,
                AoeRadius = SkillConstant.WhirlwindRadius,
                Level = 1,
                SlotIndex = 0,
            };
        }

        /// <summary>
        /// 创建冲刺技能的默认数据实例
        /// </summary>
        public static SkillData CreateDash()
        {
            return new SkillData
            {
                SkillId = SkillConstant.SkillDash,
                SkillName = SkillConstant.DefaultSkillNameDash,
                Description = SkillConstant.DefaultSkillDescDash,
                SkillType = SkillType.Movement,
                EffectType = SkillEffectType.Invincibility,
                ManaCost = SkillConstant.DashManaCost,
                BaseCooldown = SkillConstant.DashCooldown,
                AoeRadius = SkillConstant.DashDistance,
                Level = 1,
                SlotIndex = 1,
            };
        }

        /// <summary>
        /// 创建力量爆发技能的默认数据实例
        /// </summary>
        public static SkillData CreatePowerSurge()
        {
            return new SkillData
            {
                SkillId = SkillConstant.SkillPowerSurge,
                SkillName = SkillConstant.DefaultSkillNamePowerSurge,
                Description = SkillConstant.DefaultSkillDescPowerSurge,
                SkillType = SkillType.SelfBuff,
                EffectType = SkillEffectType.AttackBuff,
                ManaCost = SkillConstant.PowerSurgeManaCost,
                BaseCooldown = SkillConstant.PowerSurgeCooldown,
                EffectMultiplier = SkillConstant.PowerSurgeAtnMultiplier,
                BuffDuration = SkillConstant.PowerSurgeDuration,
                Level = 1,
                SlotIndex = 2,
            };
        }

        /// <summary>
        /// 创建治疗之光技能的默认数据实例
        /// </summary>
        public static SkillData CreateHealingLight()
        {
            return new SkillData
            {
                SkillId = SkillConstant.SkillHealingLight,
                SkillName = SkillConstant.DefaultSkillNameHealingLight,
                Description = SkillConstant.DefaultSkillDescHealingLight,
                SkillType = SkillType.SelfHeal,
                EffectType = SkillEffectType.Heal,
                ManaCost = SkillConstant.HealingLightManaCost,
                BaseCooldown = SkillConstant.HealingLightCooldown,
                EffectMultiplier = SkillConstant.HealingLightHealAmount,
                Level = 1,
                SlotIndex = 3,
            };
        }
    }
}
