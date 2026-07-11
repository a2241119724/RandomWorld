namespace LAB2D
{
    /// <summary>
    /// Pure character level progression rules.
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
                newMaxExperience = safeMaxExperience * 2;
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
