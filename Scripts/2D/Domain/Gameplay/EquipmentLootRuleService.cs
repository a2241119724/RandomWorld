namespace LAB2D
{
    /// <summary>
    /// Pure arithmetic rules for equipment loot rarity scaling.
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
