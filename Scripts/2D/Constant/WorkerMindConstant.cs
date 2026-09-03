namespace LAB2D.Constant
{
    /// <summary>
    /// Worker 心智层常量 — 命令接受/拒绝/强制、记忆、信念、执念、人生事件、人格漂移、关系。
    /// 对齐 FavorabilityConstant/WorkerConditionConstant 的集中魔法值风格。
    /// </summary>
    public static class WorkerMindConstant
    {
        // ---- 命令接受 / 拒绝 / 强制 ----
        /// <summary>拒绝/拖延玩家命令后的冷却时长（秒）。冷却内不接同源玩家悬赏。</summary>
        public const float RefusalDelayCooldownSeconds = 40f;

        /// <summary>玩家强制命令的放行窗口（秒）。窗口内权威门/体验门放行该 Worker 接玩家悬赏。</summary>
        public const float ForceWindowSeconds = 60f;

        /// <summary>强制命令对怨恨的基础惩罚。</summary>
        public const float ForceResentmentPenalty = 8f;

        /// <summary>强制命令且正处于拒绝冷却中时的额外怨恨惩罚。</summary>
        public const float ForceResentmentPenaltyDuringCooldown = 3f;

        /// <summary>强制命令连击间隔（秒）：防止玩家无限点按强刷怨恨。</summary>
        public const float ForceCommandCooldownSeconds = 5f;

        /// <summary>强制命令对 TrustInPlayer 的惩罚。</summary>
        public const float ForceTrustPenalty = 5f;

        /// <summary>强制命令对玩家好感度的惩罚（FavorabilityManager.ModifyWithPlayer）。</summary>
        public const float ForceFavorabilityPenalty = 5f;

        /// <summary>接受玩家命令后的微小感恩增益（单次）。</summary>
        public const float AcceptGratitudeGain = 0.5f;

        /// <summary>接受玩家命令后的怨恨缓和（单次）。</summary>
        public const float AcceptResentmentRecover = 2f;

        /// <summary>被玩家救危的感恩提升（EVT_PLAYER_HELP）。</summary>
        public const float HelpGratitudeGain = 6f;

        /// <summary>被玩家救危的信任提升。</summary>
        public const float HelpTrustGain = 3f;

        /// <summary>怨恨/感恩随时间的自然消退间隔（秒）。</summary>
        public const float ResentmentDecayIntervalSeconds = 120f;

        /// <summary>每个消退间隔怨恨的减少量。</summary>
        public const float ResentmentDecayPerInterval = 1f;

        /// <summary>每个消退间隔感恩的减少量。</summary>
        public const float GratitudeDecayPerInterval = 1f;

        // ---- 事件记忆 ----
        /// <summary>事件记忆上限，超限丢弃最旧。</summary>
        public const int MemoryCap = 24;

        /// <summary>记忆残留权重逐日衰减速率。</summary>
        public const float MemoryForgetRatePerDay = 0.06f;

        /// <summary>记忆权重低于该值剔除（遗忘）。</summary>
        public const float MemoryWeightPrune = 0.12f;

        // ---- 愿望 / 执念 ----
        /// <summary>执念热情逐日消退速率。</summary>
        public const float DreamPassionDecayPerDay = 3f;

        /// <summary>执念诞生阈值（事件强度达标才可能产生执念）。</summary>
        public const float DreamBornThreshold = 40f;

        /// <summary>执念历史上限。</summary>
        public const int DreamHistoryCap = 8;

        /// <summary>执念改目标的概率：RefreshGoal 每次达标执念检查把 CurrentGoal 指到执念映射的触发率。</summary>
        public const float DreamRedirectChance = 0.25f;

        // ---- 随机人生事件 ----
        /// <summary>人生事件掷骰间隔（游戏日）。</summary>
        public const int LifeEventRollIntervalDays = 2;

        /// <summary>每个掷骰日的掷骰基准概率。</summary>
        public const float LifeEventBaseChancePerRoll = 0.35f;

        /// <summary>每个游戏日最多触发的人生事件数。</summary>
        public const int LifeEventMaxPerDay = 1;

        /// <summary>人生事件对生存数值的单次封顶。</summary>
        public const float LifeEventSurvivalDamageCap = 15f;

        /// <summary>人生事件对人格漂移累积的单次封顶。</summary>
        public const float LifeEventPersonalityDriftCap = 8f;

        // ---- 人格漂移 ----
        /// <summary>单桶累积超过该阈值触发迁移。</summary>
        public const float DriftMigrateThreshold = 12f;

        /// <summary>单次迁移步长（clamp）。</summary>
        public const float DriftMigrateStep = 2f;

        /// <summary>滞回带：迁移后反向需再积 (Threshold + Band) 才回迁。</summary>
        public const float DriftHysteresisBand = 6f;

        /// <summary>每游戏日最多迁移次数。</summary>
        public const int DriftMigrateMaxPerDay = 1;

        /// <summary>事件强度 → 漂移量的缩放系数（intensity * scale 累积到桶）。</summary>
        public const float DriftAccumulateScale = 0.12f;

        /// <summary>单次事件对漂移桶的累积封顶。</summary>
        public const float DriftMaxPerEvent = 8f;

        /// <summary>漂移桶绝对值上限（饱和：同向反复累积不无限涨）。</summary>
        public const float DriftBucketAbsCap = 30f;

        // ---- 自发关系系统 ----
        /// <summary>亲密度 ≥ 该值升级为友谊。</summary>
        public const float RelationFriendshipThreshold = 40f;

        /// <summary>亲密度 ≤ 该值升级为敌意。</summary>
        public const float RelationEnmityThreshold = -30f;

        /// <summary>爱慕度 ≥ 该值升级为爱慕。</summary>
        public const float RelationAdmirationThreshold = 40f;

        /// <summary>亲密度绝对值上限。</summary>
        public const float RelationAffinityAbsCap = 100f;

        /// <summary>记仇/爱慕度上限。</summary>
        public const float RelationLevelAbsCap = 100f;

        /// <summary>记仇每日衰减量（仅化解事件清零之外的自然缓和）。</summary>
        public const float RelationGrudgeDecayPerDay = 2f;

        /// <summary>亲密度每日向 0 回归量（长期不互动关系变淡）。</summary>
        public const float RelationAffinityDecayPerDay = 1f;

        /// <summary>爱慕度每日衰减量。</summary>
        public const float RelationAdmirationDecayPerDay = 1f;

        /// <summary>交易成功：亲密度增益（intensity 缩放）。</summary>
        public const float RelationTradeSuccessAffinityScale = 0.15f;

        /// <summary>帮对方完成悬赏：亲密度增益。</summary>
        public const float RelationBountyCompleteAffinity = 8f;

        /// <summary>接取对方悬赏：亲密度增益。</summary>
        public const float RelationBountyAcceptAffinity = 5f;

        /// <summary>对话：亲密度增益。</summary>
        public const float RelationConversationAffinity = 4f;

        /// <summary>交易被拒：记仇度。</summary>
        public const float RelationTradeRejectGrudge = 30f;

        /// <summary>被对方攻击：记仇度。</summary>
        public const float RelationAttackGrudge = 40f;

        /// <summary>记仇时的亲密度惩罚。</summary>
        public const float RelationGrudgeAffinityPenalty = 8f;

        /// <summary>被攻击时的亲密度惩罚。</summary>
        public const float RelationAttackAffinityPenalty = 10f;

        /// <summary>送礼触发概率（漫游决策时）。</summary>
        public const float RelationGiftChance = 0.05f;

        /// <summary>送礼亲密度增益（双方对等）。</summary>
        public const float RelationGiftAffinityGain = 8f;

        /// <summary>收礼方对送礼方的好感度增益（讨好）。</summary>
        public const float RelationGiftFavorabilityDelta = 5f;

        /// <summary>嫉妒旁观者（贪婪高）对完成者的亲密度惩罚。</summary>
        public const float RelationJealousyAffinityPenalty = 4f;

        /// <summary>嫉妒触发的贪婪阈值。</summary>
        public const float RelationJealousyGreedThreshold = 60f;

        /// <summary>嫉妒节流间隔（秒，防每笔悬赏都刷）。</summary>
        public const float RelationJealousyIntervalSeconds = 30f;

        /// <summary>工友突破（敬仰向）：爱慕度增益（M2A 修仙事件接心智层）。</summary>
        public const float RelationBreakthroughAdmirationGain = 30f;

        /// <summary>工友突破（嫉妒向）：记仇度增益。</summary>
        public const float RelationBreakthroughEnvyGrudge = 15f;

        // ---- 命令接受判定理由键 ----
        public const string ReasonAccept = "accept";
        public const string ReasonSurvival = "survival";
        public const string ReasonCooldown = "cooldown";
        public const string ReasonFavorability = "favorability";
        public const string ReasonResentment = "resentment";
        public const string ReasonGratitude = "gratitude";
        public const string ReasonWillingness = "willingness";
        public const string ReasonRandomMood = "random_mood";

        // ---- 事件类型键（EVT_*）----
        public const string EVT_PLAYER_HELP = "player_help";
        public const string EVT_PLAYER_ATTACK = "player_attack";
        public const string EVT_PLAYER_KILL = "player_kill";
        public const string EVT_WORKER_ATTACK = "worker_attack";
        public const string EVT_BOUNTY_COMPLETED = "bounty_completed";
        public const string EVT_BOUNTY_ACCEPTED = "bounty_accepted";
        public const string EVT_BOUNTY_REFUSED = "bounty_refused";
        public const string EVT_TRADE_SUCCESS = "trade_success";
        public const string EVT_TRADE_REJECTED = "trade_rejected";
        public const string EVT_CONVERSATION = "conversation";
        public const string EVT_TASK_COMPLETED = "task_completed";
        public const string EVT_STAGE_UP = "stage_up";
        public const string EVT_NEAR_DEATH = "near_death";
        public const string EVT_GROUND_SLEEP = "ground_sleep";
        public const string EVT_FOUND_ITEM = "found_item";
        public const string EVT_WIND_FALL = "wind_fall";
        public const string EVT_ILLNESS = "illness";
        public const string EVT_INSIGHT = "insight";
        public const string EVT_MISFORTUNE = "misfortune";
        public const string EVT_ENLIGHTENMENT = "enlightenment";
        public const string EVT_SMALL_JOY = "small_joy";
        public const string EVT_NIGHTMARE = "nightmare";
        public const string EVT_CULTIVATION_BREAKTHROUGH = "cultivation_breakthrough";
        public const string EVT_POWER_AWAKEN = "power_awaken";
        public const string EVT_FELLOW_BREAKTHROUGH = "fellow_breakthrough";
        public const string EVT_FELLOW_BREAKTHROUGH_ENVY = "fellow_breakthrough_envy";

        // ---- 对话预设意图事件（M3 包2.4 LLM 对话结算）----
        public const string EVT_TEACH_SEEK = "teach_seek";
        public const string EVT_COMFORTED = "comforted";
        public const string EVT_APOLOGY = "apology";
        public const string EVT_GIFT_PLAYER = "gift_player";
    }
}
