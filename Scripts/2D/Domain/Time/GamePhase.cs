namespace LAB2D.Domain.Time
{
    /// <summary>
    /// 昼夜相位 — 按一天进度（DayProgress，0=午夜、0.5=正午）划分四段：
    /// Night [0.80, 1.20) → Dawn [0.20, 0.30) → Day [0.30, 0.70) → Dusk [0.70, 0.80)。
    /// 边界常量收敛在 <see cref="DayNightRuleService"/>，消费方一律经 RuleService 判定。
    /// </summary>
    public enum GamePhase
    {
        /// <summary>黎明（晨，天气预报与准备窗口）。</summary>
        Dawn = 0,

        /// <summary>白天（安全经营期）。</summary>
        Day = 1,

        /// <summary>黄昏（预警窗口：敌情/天气/基地薄弱点提示）。</summary>
        Dusk = 2,

        /// <summary>夜晚（危险期，波次来袭窗口）。</summary>
        Night = 3,
    }
}
