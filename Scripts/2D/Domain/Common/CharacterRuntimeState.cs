namespace LAB2D.Domain.Common
{
    /// <summary>
    /// 角色运行时状态 — 纯 C# 数据模型，不依赖 UnityEngine。
    /// 角色生命值、蓝量、等级、经验值、受击时间等可变状态。
    /// 所有修改通过 Domain Service 返回值进行，本类不包含修改方法。
    /// </summary>
    public sealed class CharacterRuntimeState
    {
        public readonly float Hp;
        public readonly float MaxHp;
        public readonly int Mp;
        public readonly int MaxMp;
        public readonly int Level;
        public readonly int CurExperience;
        public readonly int MaxExperience;
        public readonly float LastDamageTime;
        public readonly bool IsRespawning;

        public CharacterRuntimeState(
            float hp,
            float maxHp,
            int mp = 100,
            int maxMp = 100,
            int level = 1,
            int curExperience = 0,
            int maxExperience = 4,
            float lastDamageTime = -99f,
            bool isRespawning = false)
        {
            this.Hp = hp;
            this.MaxHp = maxHp;
            this.Mp = mp;
            this.MaxMp = maxMp;
            this.Level = level;
            this.CurExperience = curExperience;
            this.MaxExperience = maxExperience;
            this.LastDamageTime = lastDamageTime;
            this.IsRespawning = isRespawning;
        }

        public bool IsDead
        {
            get { return this.Hp <= 0f; }
        }

        public bool IsAlive
        {
            get { return !this.IsDead; }
        }

        public CharacterRuntimeState WithHp(float newHp)
        {
            return new CharacterRuntimeState(
                newHp, this.MaxHp, this.Mp, this.MaxMp,
                this.Level, this.CurExperience, this.MaxExperience,
                this.LastDamageTime, this.IsRespawning);
        }

        public CharacterRuntimeState WithHpAndDamageTime(float newHp, float damageTime)
        {
            return new CharacterRuntimeState(
                newHp, this.MaxHp, this.Mp, this.MaxMp,
                this.Level, this.CurExperience, this.MaxExperience,
                damageTime, this.IsRespawning);
        }

        public CharacterRuntimeState WithExperience(int newCurExperience, int newMaxExperience, int newLevel)
        {
            return new CharacterRuntimeState(
                this.Hp, this.MaxHp, this.Mp, this.MaxMp,
                newLevel, newCurExperience, newMaxExperience,
                this.LastDamageTime, this.IsRespawning);
        }

        public CharacterRuntimeState WithRespawning(bool isRespawning)
        {
            return new CharacterRuntimeState(
                this.Hp, this.MaxHp, this.Mp, this.MaxMp,
                this.Level, this.CurExperience, this.MaxExperience,
                this.LastDamageTime, isRespawning);
        }

        public static CharacterRuntimeState FromCharacterData(
            float hp,
            float maxHp,
            int mp,
            int maxMp,
            int level,
            int curExperience,
            int maxExperience,
            float lastDamageTime = -99f,
            bool isRespawning = false)
        {
            return new CharacterRuntimeState(
                hp, maxHp, mp, maxMp,
                level, curExperience, maxExperience,
                lastDamageTime, isRespawning);
        }
    }
}
