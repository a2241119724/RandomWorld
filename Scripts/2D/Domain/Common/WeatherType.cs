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
    }
}
