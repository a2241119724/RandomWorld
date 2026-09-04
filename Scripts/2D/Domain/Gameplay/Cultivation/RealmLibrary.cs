namespace LAB2D.Domain.Gameplay.Cultivation
{
    using System.Collections.Generic;
    using LAB2D.Domain.Character;
    using LAB2D.Domain.Character.Growth;

    /// <summary>
    /// 境界静态数据 — 本版实现凡人/练气/筑基/金丹/元婴/化神六境。
    /// 突破加成为 GrowthBonus（Stats 进常规管线，MaxHpFlat 进生命上限），累进存入
    /// GrowthData.PermanentRealmBonus，由成长收集服务并入属性重算。
    /// </summary>
    public static class RealmLibrary
    {
        /// <summary>凡人 — 初始境界。</summary>
        public static readonly RealmDef Mortal = new RealmDef
        {
            Index = 0,
            Name = "凡人",
            QiToNext = 100f,
            Bonus = GrowthBonus.Zero,
        };

        /// <summary>练气 — 突破奖励：物攻+4、物防+2。</summary>
        public static readonly RealmDef QiRefining = new RealmDef
        {
            Index = 1,
            Name = "练气",
            QiToNext = 400f,
            Bonus = new GrowthBonus(new BattleStats(4f, 0f, 2f, 0f, 0f, 0f, 0f, 0f)),
        };

        /// <summary>筑基 — 突破奖励：物攻+8、物防+4、魔防+4、生命上限+50。</summary>
        public static readonly RealmDef Foundation = new RealmDef
        {
            Index = 2,
            Name = "筑基",
            QiToNext = 1200f,
            Bonus = new GrowthBonus(
                new BattleStats(8f, 0f, 4f, 4f, 0f, 0f, 0f, 0f),
                maxHpFlat: 50f),
        };

        /// <summary>金丹 — 突破奖励：全维+10、生命上限+150。</summary>
        public static readonly RealmDef GoldenCore = new RealmDef
        {
            Index = 3,
            Name = "金丹",
            QiToNext = 3600f,
            Bonus = new GrowthBonus(
                new BattleStats(10f, 10f, 10f, 10f, 0f, 0f, 0f, 0f),
                maxHpFlat: 150f),
        };

        /// <summary>元婴 — 突破奖励：全维+15、生命上限+400。</summary>
        public static readonly RealmDef NascentSoul = new RealmDef
        {
            Index = 4,
            Name = "元婴",
            QiToNext = 10800f,
            Bonus = new GrowthBonus(
                new BattleStats(15f, 15f, 15f, 15f, 0f, 0f, 0f, 0f),
                maxHpFlat: 400f),
        };

        /// <summary>化神 — 当前最高境界，突破奖励：全维+25、生命上限+800。</summary>
        public static readonly RealmDef SpiritTransform = new RealmDef
        {
            Index = 5,
            Name = "化神",
            QiToNext = 0f,
            Bonus = new GrowthBonus(
                new BattleStats(25f, 25f, 25f, 25f, 0f, 0f, 0f, 0f),
                maxHpFlat: 800f),
        };

        /// <summary>全部境界（按索引升序）。</summary>
        public static readonly IReadOnlyList<RealmDef> All = new[]
        {
            Mortal, QiRefining, Foundation, GoldenCore, NascentSoul, SpiritTransform,
        };

        /// <summary>按索引取境界定义（越界回落凡人）。</summary>
        public static RealmDef Get(int index)
        {
            if (index < 0 || index >= All.Count)
            {
                return Mortal;
            }

            return All[index];
        }

        /// <summary>是否已达最高境界。</summary>
        public static bool IsMax(int index)
        {
            return index >= SpiritTransform.Index;
        }
    }
}
