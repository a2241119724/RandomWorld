namespace LAB2D.Domain.Item
{
    using System;

    /// <summary>
    /// 装备词条类型。
    /// 数值词条（FlatAtn/FlatInt/MaxHp）进属性加法管线；
    /// 特殊词条（Lifesteal/Reflect）为比例值，由战斗事件处理器消费。
    /// </summary>
    public enum EquipmentAffixType
    {
        /// <summary>物理攻击 +N。</summary>
        FlatAtn,

        /// <summary>魔法攻击 +N。</summary>
        FlatInt,

        /// <summary>生命上限 +N。</summary>
        MaxHp,

        /// <summary>吸血 N%（0.05 = 5%）。</summary>
        Lifesteal,

        /// <summary>反伤 N%（0.1 = 10%）。</summary>
        Reflect,
    }

    /// <summary>
    /// 装备词条条目 — 随 AEquipment 实例序列化（挂背包存档）。
    /// </summary>
    [Serializable]
    public class EquipmentAffix
    {
        /// <summary>词条类型。</summary>
        public EquipmentAffixType Type;

        /// <summary>词条数值（含义随 Type：平坦值或比例）。</summary>
        public float Value;

        public EquipmentAffix()
        {
        }

        public EquipmentAffix(EquipmentAffixType type, float value)
        {
            this.Type = type;
            this.Value = value;
        }
    }
}
