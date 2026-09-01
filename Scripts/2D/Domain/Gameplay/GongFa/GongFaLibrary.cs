namespace LAB2D.Domain.Gameplay.GongFa
{
    using System.Collections.Generic;
    using LAB2D.Domain.Character;
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Gameplay.Cultivation;

    /// <summary>
    /// 武学功法静态库 — 本版 5 条：3 内功（长春功/玄阳功/玄天诀）+ 2 外功（横扫千军/破空斩）。
    /// 数值调整直接影响游戏平衡，请配合 Play Mode 验证。
    /// </summary>
    public static class GongFaLibrary
    {
        /// <summary>长春功 — 木系入门内功（练气可修）。</summary>
        public static readonly GongFaDef ChangChun = new GongFaDef
        {
            Id = "gongfa_changchun",
            Name = "长春功",
            Description = "木系入门心法，滋养气血神识",
            Element = Element.Wood,
            IsNeiGong = true,
            Bonus = new GrowthBonus(new BattleStats(5f, 8f, 0f, 0f, 0f, 0f, 0f, 0f), mpRegenPerSec: 2f),
            MpRegenPerSec = 2f,
            RequiredRealmIndex = 1,
        };

        /// <summary>玄阳功 — 火系攻击内功（筑基可修）。</summary>
        public static readonly GongFaDef XuanYang = new GongFaDef
        {
            Id = "gongfa_xuanyang",
            Name = "玄阳功",
            Description = "火系霸道心法，大幅提升攻击与暴击",
            Element = Element.Fire,
            IsNeiGong = true,
            Bonus = new GrowthBonus(new BattleStats(12f, 0f, 0f, 0f, 0.03f, 0f, 0f, 0f)),
            MpRegenPerSec = 0f,
            RequiredRealmIndex = 2,
        };

        /// <summary>玄天诀 — 金系顶阶内功（金丹可修，全维强化）。</summary>
        public static readonly GongFaDef XuanTian = new GongFaDef
        {
            Id = "gongfa_xuantian",
            Name = "玄天诀",
            Description = "金系无上心法，全方位强化且灵力自生",
            Element = Element.Metal,
            IsNeiGong = true,
            Bonus = new GrowthBonus(new BattleStats(6f, 6f, 6f, 6f, 0f, 0f, 0f, 0f), mpRegenPerSec: 3f),
            MpRegenPerSec = 3f,
            RequiredRealmIndex = 3,
        };

        /// <summary>横扫千军 — 外功范围招式（凡人可学）。</summary>
        public static readonly GongFaDef HengSao = new GongFaDef
        {
            Id = "gongfa_hengsao",
            Name = "横扫千军",
            Description = "挥舞兵器横扫周围敌人",
            Element = Element.Earth,
            IsNeiGong = false,
            RequiredRealmIndex = 0,
            SkillId = "skill_sweep_all",
            SkillOrder = 0,
        };

        /// <summary>破空斩 — 外功单体招式（凡人可学）。</summary>
        public static readonly GongFaDef PoKong = new GongFaDef
        {
            Id = "gongfa_pokong",
            Name = "破空斩",
            Description = "对最近的敌人造成高额单体伤害",
            Element = Element.Metal,
            IsNeiGong = false,
            RequiredRealmIndex = 0,
            SkillId = "skill_sky_split",
            SkillOrder = 1,
        };

        /// <summary>全部功法（学习列表展示顺序）。</summary>
        public static readonly IReadOnlyList<GongFaDef> All = new List<GongFaDef>
        {
            ChangChun,
            XuanYang,
            XuanTian,
            HengSao,
            PoKong,
        };

        /// <summary>
        /// 按 Id 查询功法，未找到返回 null。
        /// </summary>
        public static GongFaDef Get(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (GongFaDef def in All)
            {
                if (def.Id == id)
                {
                    return def;
                }
            }

            return null;
        }

        /// <summary>
        /// 取玩家已学的外功招式，保持学习顺序（LearnedGongFaIds 列表序），
        /// 用于读档重建技能注册时槽位与学习时一致。
        /// </summary>
        public static List<GongFaDef> GetLearnedExternalSkills(IReadOnlyList<string> learnedIds)
        {
            List<GongFaDef> result = new List<GongFaDef>();
            if (learnedIds == null)
            {
                return result;
            }

            foreach (string id in learnedIds)
            {
                GongFaDef def = Get(id);
                if (def != null && !def.IsNeiGong)
                {
                    result.Add(def);
                }
            }

            return result;
        }
    }
}
