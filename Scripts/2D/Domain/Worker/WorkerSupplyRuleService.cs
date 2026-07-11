namespace LAB2D
{
    /// <summary>
    /// Pure arithmetic rules for worker supply needs.
    /// </summary>
    public sealed class WorkerSupplyRuleService
    {
        public float GetRecoverNeed(float current, float max)
        {
            float need = max - current;
            return need < 0.0f ? 0.0f : need;
        }
    }
}
