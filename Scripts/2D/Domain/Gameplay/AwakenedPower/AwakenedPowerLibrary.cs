namespace LAB2D.Domain.Gameplay.AwakenedPower
{
    using System.Collections.Generic;
    using LAB2D.Domain.Character;
    using LAB2D.Domain.Character.Growth;

    /// <summary>
    /// 异能静态库 — 本版 6 条（包 5 功法异能池扩充）：念力/火球/剑气风暴/金光遁/真元爆发/回春术，
    /// 觉醒池 6 选 2（MaxAwakenedCount）。觉醒事件由 AwakenedPowerManager 在玩家/Worker 受击时按概率触发。
    /// SkillId 复用 SkillManager 既有主动技能（sweep_all/sky_split 归外功招式语义，不入异能池）。
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

        /// <summary>剑气风暴 — 旋风斩 AOE；Worker 觉醒被动 +ATN 5（近战攻击系对应）。</summary>
        public static readonly AwakenedPowerDef SwordStorm = new AwakenedPowerDef
        {
            Id = "power_sword_storm",
            Name = "剑气风暴",
            Description = "觉醒剑意，周身剑气如风暴旋转",
            SkillId = "skill_whirlwind",
            WorkerPassiveBonus = new GrowthBonus(new BattleStats(5f, 0f, 0f, 0f, 0f, 0f, 0f, 0f)),
        };

        /// <summary>金光遁 — 瞬身位移；Worker 觉醒被动 +SPD 5（身法系对应）。</summary>
        public static readonly AwakenedPowerDef GoldenEscape = new AwakenedPowerDef
        {
            Id = "power_golden_escape",
            Name = "金光遁",
            Description = "危难之际身化金光，瞬息脱离险地",
            SkillId = "skill_dash",
            WorkerPassiveBonus = new GrowthBonus(new BattleStats(0f, 0f, 0f, 0f, 0f, 0f, 5f, 0f)),
        };

        /// <summary>真元爆发 — 力量涌动增益；Worker 觉醒被动 +RES 5（真元护体对应）。</summary>
        public static readonly AwakenedPowerDef YuanBurst = new AwakenedPowerDef
        {
            Id = "power_yuan_burst",
            Name = "真元爆发",
            Description = "真元在经脉中奔涌，力量喷薄欲出",
            SkillId = "skill_power_surge",
            WorkerPassiveBonus = new GrowthBonus(new BattleStats(0f, 0f, 0f, 5f, 0f, 0f, 0f, 0f)),
        };

        /// <summary>回春术 — 治疗之光自愈；Worker 觉醒被动 +MaxHp 50（生机系，走非 Stats 通道）。</summary>
        public static readonly AwakenedPowerDef Rejuvenation = new AwakenedPowerDef
        {
            Id = "power_rejuvenation",
            Name = "回春术",
            Description = "觉醒生机之力，伤势以肉眼可见的速度愈合",
            SkillId = "skill_healing_light",
            WorkerPassiveBonus = new GrowthBonus(default, maxHpFlat: 50f),
        };

        /// <summary>全部异能（觉醒池）。</summary>
        public static readonly IReadOnlyList<AwakenedPowerDef> All = new List<AwakenedPowerDef>
        {
            Telekinesis,
            FireBall,
            SwordStorm,
            GoldenEscape,
            YuanBurst,
            Rejuvenation,
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
