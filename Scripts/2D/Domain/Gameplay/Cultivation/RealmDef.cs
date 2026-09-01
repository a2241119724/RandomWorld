namespace LAB2D.Domain.Gameplay.Cultivation
{
    using LAB2D.Domain.Character.Growth;

    /// <summary>
    /// 修仙境界定义 — 突破到本境界时获得 <see cref="Bonus"/> 永久加成；
    /// 从本境界突破到下一境界需要 <see cref="QiToNext"/> 灵气（最高境界为 0）。
    /// </summary>
    public sealed class RealmDef
    {
        /// <summary>境界索引（0=凡人，与 GrowthData.RealmIndex 对应）。</summary>
        public int Index;

        /// <summary>境界显示名。</summary>
        public string Name;

        /// <summary>突破到下一境界所需灵气；最高境界为 0（无下一境界）。</summary>
        public float QiToNext;

        /// <summary>突破到本境界时获得的永久属性加成（凡人为中性值）。</summary>
        public GrowthBonus Bonus;
    }
}
