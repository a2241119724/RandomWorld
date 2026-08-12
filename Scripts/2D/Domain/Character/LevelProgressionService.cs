namespace LAB2D.Domain.Character
{
    /// <summary>
    /// 纯角色等级升级规则。
    /// </summary>
    public sealed class LevelProgressionService
    {
        public LevelProgressionResult AddExperience(
            int currentExperience,
            int maxExperience,
            int currentLevel,
            int gainedExperience)
        {
            int safeMaxExperience = maxExperience <= 0 ? 1 : maxExperience;
            int newExperience = currentExperience + gainedExperience;
            int newLevel = currentLevel;
            int newMaxExperience = safeMaxExperience;
            bool leveledUp = false;

            if (newExperience / safeMaxExperience >= 1)
            {
                newLevel++;
                newExperience %= safeMaxExperience;
                // 线性增长曲线：每级所需经验 = 4 + 当前等级 * 3
                // Lv1→2:4, Lv2→3:7, Lv3→4:10, ..., 到10级仅需约175经验
                newMaxExperience = 4 + newLevel * 3;
                leveledUp = true;
            }

            return new LevelProgressionResult(newExperience, newMaxExperience, newLevel, leveledUp);
        }
    }

    public readonly struct LevelProgressionResult
    {
        public LevelProgressionResult(
            int currentExperience,
            int maxExperience,
            int level,
            bool leveledUp)
        {
            this.CurrentExperience = currentExperience;
            this.MaxExperience = maxExperience;
            this.Level = level;
            this.LeveledUp = leveledUp;
        }

        public int CurrentExperience { get; }

        public int MaxExperience { get; }

        public int Level { get; }

        public bool LeveledUp { get; }
    }
}
