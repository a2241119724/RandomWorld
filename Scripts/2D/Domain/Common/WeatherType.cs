namespace LAB2D.Domain.Common
{
    /// <summary>
    /// 天气类型 — 纯领域枚举，不依赖 Unity 或 Manager 层。
    /// </summary>
    public enum WeatherType
    {
        /// <summary>晴天</summary>
        Clear = 0,
        /// <summary>雨天</summary>
        Rain = 1,
        /// <summary>雪天</summary>
        Snow = 2,
        /// <summary>灵雨（事件天气）：灵气恢复大幅提升，其余无影响</summary>
        SpiritRain = 3,
        /// <summary>血月（事件天气）：当晚妖兽强化（波次数量/难度），白天与常规无差</summary>
        BloodMoon = 4,
    }
}
