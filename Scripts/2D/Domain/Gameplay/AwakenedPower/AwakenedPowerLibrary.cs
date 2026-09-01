namespace LAB2D.Domain.Gameplay.AwakenedPower
{
    using System.Collections.Generic;
    using LAB2D.Domain.Character;
    using LAB2D.Domain.Character.Growth;

    /// <summary>
    /// 异能静态库 — 本版 2 条：念力（拉怪）与火球（INT 基单体爆发）。
    /// 觉醒事件由 AwakenedPowerManager 在玩家/Worker 受击时按概率触发。
    /// </summary>
    public static class AwakenedPowerLibrary
    {
        /// <summary>念力 — 将周围敌人拉向自己；Worker 觉醒被动 +INT 5（精神系对应）。</summary>
        public static readonly AwakenedPowerDef Telekinesis = new AwakenedPowerDef
        {
            Id = "power_telekinesis",
            Name = "念力",
            Description = "觉醒的念力将周围敌人拉向你",
            SkillId = "skill_telekinesis",
            WorkerPassiveBonus = new GrowthBonus(new BattleStats(0f, 5f, 0f, 0f, 0f, 0f, 0f, 0f)),
        };

        /// <summary>火球 — 以精神力引燃的爆发；Worker 觉醒被动 +ATN 5（攻击系对应）。</summary>
        public static readonly AwakenedPowerDef FireBall = new AwakenedPowerDef
        {
            Id = "power_fireball",
            Name = "火球",
            Description = "凝聚精神力掷出炽热火球",
            SkillId = "skill_fireball",
            WorkerPassiveBonus = new GrowthBonus(new BattleStats(5f, 0f, 0f, 0f, 0f, 0f, 0f, 0f)),
        };

        /// <summary>全部异能（觉醒池）。</summary>
        public static readonly IReadOnlyList<AwakenedPowerDef> All = new List<AwakenedPowerDef>
        {
            Telekinesis,
            FireBall,
        };

        /// <summary>
        /// 按 Id 查询异能，未找到返回 null。
        /// </summary>
        public static AwakenedPowerDef Get(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (AwakenedPowerDef def in All)
            {
                if (def.Id == id)
                {
                    return def;
                }
            }

            return null;
        }
    }
}
