namespace LAB2D
{
    using System;

    /// <summary>
    /// 装备
    /// </summary>
    [Serializable]
    public abstract class AEquipment : ABackpackItem
    {
        /// <summary>
        /// 装备类型
        /// </summary>
        public EquipTypeEnum Type;

        /// <summary>
        /// 装备信息
        /// </summary>
        public Character.Attribute Attribute = new ();

        public AEquipment()
        {
            this.Attribute.ATN = this.RankRandom(5.0f, 10.0f);
            this.Attribute.INT = this.RankRandom(5.0f, 10.0f);
            this.Attribute.CRT = this.RankRandom(0.05f, 0.1f);
            this.Attribute.CSD = this.RankRandom(0f, 1.0f);
            this.Attribute.HIT = this.RankRandom(5.0f, 10.0f);
            this.Attribute.RES = this.RankRandom(5.0f, 10.0f);
            this.Attribute.SPD = this.RankRandom(5.0f, 10.0f);
            this.Attribute.DEF = this.RankRandom(5.0f, 10.0f);
        }

        /// <summary>
        /// 装备类型
        /// </summary>
        public enum EquipTypeEnum
        {
            /// <summary>
            /// 头部
            /// </summary>
            Head,

            /// <summary>
            /// 上衣
            /// </summary>
            Body,

            /// <summary>
            /// 裤子
            /// </summary>
            Trouser,

            /// <summary>
            /// 鞋子
            /// </summary>
            Shoes,

            /// <summary>
            /// 武器
            /// </summary>
            Weapon,

            /// <summary>
            /// 盾牌
            /// </summary>
            Shield,

            /// <summary>
            /// 戒指
            /// </summary>
            Ring,

            /// <summary>
            /// 项链
            /// </summary>
            Necklace,

            /// <summary>
            /// 手镯
            /// </summary>
            Bracelet,

            /// <summary>
            /// 腰带
            /// </summary>
            Belt,

            /// <summary>
            /// 耳环
            /// </summary>
            Earring,

            /// <summary>
            /// 翅膀
            /// </summary>
            Wing,

            /// <summary>
            /// 坐骑
            /// </summary>
            Mount,

            /// <summary>
            /// 宠物
            /// </summary>
            Pet,

            /// <summary>
            /// 空
            /// </summary>
            Null,
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            float mult = EquipmentLootTool.GetQualityStatMultiplier(this.Quality);

            return base.ToString() +
                $"槽位: {EquipmentLootTool.GetSlotName(this.Type)}\n" +
                $"品质倍率: x{mult:F1}\n" +
                $"ATN 物理攻击: {this.Attribute.ATN:F1}\n" +
                $"INT 魔法攻击: {this.Attribute.INT:F1}\n" +
                $"DEF 物理防御: {this.Attribute.DEF:F1}\n" +
                $"RES 魔法防御: {this.Attribute.RES:F1}\n" +
                $"CRT 暴击率: {this.Attribute.CRT:P1}\n" +
                $"CSD 暴击伤害: {this.Attribute.CSD:F1}\n" +
                $"SPD 速度回避: {this.Attribute.SPD:F1}\n" +
                $"HIT 命中连击: {this.Attribute.HIT:F1}\n";
        }

        /// <summary>
        /// 生成数越大生成的随机数几率越小
        /// </summary>
        /// <param name="down">下限</param>
        /// <param name="up">上限</param>
        /// <returns>随机数</returns>
        protected float RankRandom(float down, float up)
        {
            if (down > up)
            {
                float t = down;
                down = up;
                up = t;
            }

            float intervalValue = (up - down) / 20;
            float r; // 每次生成随机数进行判断
            for (float t = down + intervalValue; t < up; t += intervalValue)
            {
                r = UnityEngine.Random.Range(down, up);
                if (r < t)
                {
                    return r;
                }
            }

            return 0.0f;
        }
    }

    /// <summary>
    /// 装备对象
    /// </summary>
    public abstract class AEquipmentObject : ABackpackItemObject
    {
    }
}