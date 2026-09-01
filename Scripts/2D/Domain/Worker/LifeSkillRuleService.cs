namespace LAB2D.Domain.Worker
{
    using LAB2D.Constant;
    using LAB2D.Enum;
    using System.Collections.Generic;

    /// <summary>
    /// 生活技能规则服务 — 经验/等级/效率倍率的纯函数计算。
    /// 经验存储在 WorkerData.LifeSkillXp（key=LifeSkillType），
    /// 本服务只做数值换算，不持有状态，可独立单元测试。
    /// </summary>
    public static class LifeSkillRuleService
    {
        /// <summary>单次任务经验值（按技能类型）。</summary>
        public static float XpPerTask(LifeSkillType skill)
        {
            return skill switch
            {
                LifeSkillType.Felling => LifeSkillConstant.XpPerFelling,
                LifeSkillType.Mining => LifeSkillConstant.XpPerMining,
                LifeSkillType.Farming => LifeSkillConstant.XpPerFarming,
                _ => 0f,
            };
        }

        /// <summary>
        /// 当前等级（0-3）：按累计经验跨过阈值升级。
        /// </summary>
        public static int LevelOf(float xp)
        {
            if (xp >= LifeSkillConstant.XpToLevel3)
            {
                return 3;
            }

            if (xp >= LifeSkillConstant.XpToLevel2)
            {
                return 2;
            }

            if (xp >= LifeSkillConstant.XpToLevel1)
            {
                return 1;
            }

            return 0;
        }

        /// <summary>指定等级的效率倍率（任务进度速度）。</summary>
        public static float GetMultiplier(int level)
        {
            return level switch
            {
                1 => LifeSkillConstant.MultiplierLevel1,
                2 => LifeSkillConstant.MultiplierLevel2,
                3 => LifeSkillConstant.MultiplierLevel3,
                _ => LifeSkillConstant.MultiplierLevel0,
            };
        }

        /// <summary>按累计经验直接取效率倍率。</summary>
        public static float GetMultiplier(float xp)
        {
            return GetMultiplier(LevelOf(xp));
        }

        /// <summary>
        /// 升到下一级所需的总经验；已满级返回 -1。
        /// </summary>
        public static float XpToNextLevel(float xp)
        {
            if (xp >= LifeSkillConstant.XpToLevel3)
            {
                return -1f;
            }

            if (xp >= LifeSkillConstant.XpToLevel2)
            {
                return LifeSkillConstant.XpToLevel3;
            }

            if (xp >= LifeSkillConstant.XpToLevel1)
            {
                return LifeSkillConstant.XpToLevel2;
            }

            return LifeSkillConstant.XpToLevel1;
        }

        /// <summary>技能中文名。</summary>
        public static string GetName(LifeSkillType skill)
        {
            return skill switch
            {
                LifeSkillType.Felling => LifeSkillConstant.FellingName,
                LifeSkillType.Mining => LifeSkillConstant.MiningName,
                LifeSkillType.Farming => LifeSkillConstant.FarmingName,
                _ => skill.ToString(),
            };
        }

        /// <summary>全部生活技能（UI 展示顺序）。</summary>
        public static IReadOnlyList<LifeSkillType> AllSkills { get; } = new List<LifeSkillType>
        {
            LifeSkillType.Felling,
            LifeSkillType.Mining,
            LifeSkillType.Farming,
        };
    }
}
