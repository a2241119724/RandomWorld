namespace LAB2D
{
    /// <summary>
    /// 装备掉落稀有度缩放的纯算术规则。
    /// </summary>
    public sealed class EquipmentLootRuleService
    {
        public float GetRarityWeightBonus(int waveNumber, float bonusPerWave, float maxBonus)
        {
            float bonus = waveNumber * bonusPerWave;
            if (bonus < 0.0f)
            {
                return 0.0f;
            }

            return bonus > maxBonus ? maxBonus : bonus;
        }
    }
}
