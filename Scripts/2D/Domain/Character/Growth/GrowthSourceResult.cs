namespace LAB2D.Domain.Character.Growth
{
    using System.Collections.Generic;

    /// <summary>
    /// 一次属性重算中所有成长源的收集结果。
    /// Sources 传入 AttributeCalculationService.ComputeFinalStats 参与加法管线；
    /// Special 汇总 8 维之外的维度（上限/回蓝/吸血/反伤/修炼速度）。
    /// </summary>
    public sealed class GrowthSourceResult
    {
        /// <summary>各成长源的 8 维加成列表（进入常规加法管线）。</summary>
        public List<BattleStats> Sources { get; } = new List<BattleStats>();

        /// <summary>特殊维度累计（含 Stats，方便消费方一次取用）。</summary>
        public GrowthBonus Special { get; private set; } = GrowthBonus.Zero;

        /// <summary>追加一个成长源贡献。</summary>
        public void Add(GrowthBonus bonus)
        {
            this.Sources.Add(bonus.Stats);
            this.Special += bonus;
        }
    }
}
