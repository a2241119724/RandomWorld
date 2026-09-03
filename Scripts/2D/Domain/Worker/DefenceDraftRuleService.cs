namespace LAB2D.Domain.Worker
{
    /// <summary>
    /// 防守夜 Worker 响应（M2A 包 2.1）— 三种行为分化。
    /// </summary>
    public enum DefenceResponse
    {
        /// <summary>参战：赶往山门核心附近驻守，迎敌靠既有被动反击。</summary>
        Fight,

        /// <summary>躲避：缩回床/家旁待命（胆小/高压/低士气者）。</summary>
        ShelterInBed,

        /// <summary>趁乱：溜到远离核心处游荡（高贪婪+对玩家无感情者）。</summary>
        Loot,
    }

    /// <summary>
    /// 防守夜决策输入快照（纯数据，由调用方从 WorkerData/好感度/成长数据采集）。
    /// 全部 0-100 标量 + 两个布尔标志。
    /// </summary>
    public struct DefenceDraftInput
    {
        /// <summary>人格·心情。</summary>
        public float Mood;

        /// <summary>人格·事业心。</summary>
        public float Ambition;

        /// <summary>人格·勤奋。</summary>
        public float Diligence;

        /// <summary>人格·社交（合群者更愿与大家一同守夜）。</summary>
        public float Sociality;

        /// <summary>贪婪。</summary>
        public float Greed;

        /// <summary>当前压力。</summary>
        public float Stress;

        /// <summary>当前士气。</summary>
        public float Morale;

        /// <summary>对玩家好感。</summary>
        public float FavorWithPlayer;

        /// <summary>已觉醒异能（战斗权重大幅加分——觉醒者优先参战）。</summary>
        public bool HasAwakenedPower;

        /// <summary>境界索引（RealmLibrary，越高越敢战）。</summary>
        public int RealmIndex;
    }

    /// <summary>
    /// 防守夜响应决策纯规则服务 — 人格 4 维 + 贪婪 + 压力/士气 + 对玩家好感 → 三种行为。
    /// 零 Unity 依赖，可单测；确定性地取三类行为分最高者（不加随机，
    /// 玩家可从人格/状态稳定预测某个村民夜里的选择——"胆小的会躲床"是可读的设计）。
    /// </summary>
    public static class DefenceDraftRuleService
    {
        // ---- 参战分 ----
        /// <summary>参战基础分。</summary>
        public const float FightBase = 40f;

        /// <summary>勤奋/社交/士气/好感对参战分的系数（评分项）。</summary>
        public const float FightDiligenceScale = 0.25f;
        public const float FightSocialityScale = 0.05f;
        public const float FightMoraleScale = 0.15f;
        public const float FightFavorScale = 0.2f;

        /// <summary>事业心对参战分的系数（野心大者渴望战功证明自己，权重与好感齐平）。</summary>
        public const float FightAmbitionScale = 0.2f;

        /// <summary>压力对参战分的惩罚系数。</summary>
        public const float FightStressPenalty = 0.25f;

        /// <summary>心情低落对参战分的惩罚系数（按 100-Mood 计）。</summary>
        public const float FightBadMoodPenalty = 0.1f;

        /// <summary>已觉醒异能的参战加分（觉醒者优先参战的红线要求）。</summary>
        public const float FightAwakenedBonus = 40f;

        /// <summary>每级境界的参战加分。</summary>
        public const float FightRealmBonusPerLevel = 10f;

        // ---- 躲避分 ----
        /// <summary>躲避基础分。</summary>
        public const float ShelterBase = 10f;

        /// <summary>心情低落/压力高/士气低/勤奋低对躲避分的系数（按 100-x 计心情与勤奋）。</summary>
        public const float ShelterBadMoodScale = 0.2f;
        public const float ShelterStressScale = 0.25f;
        public const float ShelterLowMoraleScale = 0.15f;
        public const float ShelterLowDiligenceScale = 0.1f;

        // ---- 趁乱分 ----
        /// <summary>趁乱基础分。</summary>
        public const float LootBase = 5f;

        /// <summary>贪婪对趁乱分的系数。</summary>
        public const float LootGreedScale = 0.45f;

        /// <summary>对玩家无感情（100-好感）对趁乱分的系数。</summary>
        public const float LootNoFavorScale = 0.15f;

        /// <summary>压力高对趁乱分的系数。</summary>
        public const float LootStressScale = 0.1f;

        /// <summary>士气低对趁乱分的惩罚系数。</summary>
        public const float LootLowMoralePenalty = 0.05f;

        /// <summary>趁乱行为的贪婪门槛 — 低于此值趁乱分无效（本分守己者不趁乱）。</summary>
        public const float LootGreedMin = 50f;

        /// <summary>
        /// 防守夜响应决策：取参战/躲避/趁乱三类行为分最高者。
        /// </summary>
        public static DefenceResponse Decide(in DefenceDraftInput input)
        {
            float fight = FightBase
                + (input.Diligence * FightDiligenceScale)
                + (input.Sociality * FightSocialityScale)
                + (input.Morale * FightMoraleScale)
                + (input.FavorWithPlayer * FightFavorScale)
                + (input.Ambition * FightAmbitionScale)
                - (input.Stress * FightStressPenalty)
                - ((100f - input.Mood) * FightBadMoodPenalty)
                + (input.HasAwakenedPower ? FightAwakenedBonus : 0f)
                + (input.RealmIndex * FightRealmBonusPerLevel);

            float shelter = ShelterBase
                + ((100f - input.Mood) * ShelterBadMoodScale)
                + (input.Stress * ShelterStressScale)
                + ((100f - input.Morale) * ShelterLowMoraleScale)
                + ((100f - input.Diligence) * ShelterLowDiligenceScale);

            float loot = LootBase
                + (input.Greed * LootGreedScale)
                + ((100f - input.FavorWithPlayer) * LootNoFavorScale)
                + (input.Stress * LootStressScale)
                - ((100f - input.Morale) * LootLowMoralePenalty);

            if (input.Greed < LootGreedMin)
            {
                loot = float.MinValue; // 贪婪未过门槛：本分守己者永不趁乱
            }

            if (fight >= shelter && fight >= loot)
            {
                return DefenceResponse.Fight;
            }

            return shelter >= loot ? DefenceResponse.ShelterInBed : DefenceResponse.Loot;
        }
    }
}
