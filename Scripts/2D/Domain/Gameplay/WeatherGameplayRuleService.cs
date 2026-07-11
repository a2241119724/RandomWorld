namespace LAB2D
{
    /// <summary>
    /// Pure arithmetic rules for weather gameplay multipliers.
    /// </summary>
    public sealed class WeatherGameplayRuleService
    {
        public float ApplyMultiplier(float baseValue, float multiplier, float minValue)
        {
            float safeMultiplier = multiplier < 0.0f ? 0.0f : multiplier;
            float value = baseValue * safeMultiplier;
            return value < minValue ? minValue : value;
        }
    }
}
