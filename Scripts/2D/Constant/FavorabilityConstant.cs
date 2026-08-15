namespace LAB2D.Constant
{
    using LAB2D.Domain.Worker;
    using UnityEngine;

    /// <summary>
    /// 好感度系统常量。
    /// 业务魔法值（阈值/增减量/价格规则）委托自 <see cref="FavorabilityRuleService"/>，
    /// 此处保留向后兼容引用；空间/时间/UI 调参类常量直接维护于此。
    /// </summary>
    public static class FavorabilityConstant
    {
        // ---- 委托自 FavorabilityRuleService ----
        public const float InitialFavorability = FavorabilityRuleService.InitialFavorability;
        public const float PlayerBountyRefuseThreshold = FavorabilityRuleService.PlayerBountyRefuseThreshold;
        public const float WorkerBountyRefuseThreshold = FavorabilityRuleService.WorkerBountyRefuseThreshold;
        public const float TradeRefuseThreshold = FavorabilityRuleService.TradeRefuseThreshold;
        public const float AttackToPlayerDelta = FavorabilityRuleService.AttackToPlayerDelta;
        public const float KillToPlayerBonus = FavorabilityRuleService.KillToPlayerBonus;
        public const float WorkerAttackDelta = FavorabilityRuleService.WorkerAttackDelta;
        public const float HelpVsEnemyDelta = FavorabilityRuleService.HelpVsEnemyDelta;
        public const float PlayerBountyCompleteDelta = FavorabilityRuleService.PlayerBountyCompleteDelta;
        public const float PlayerBountyCompleteLowDelta = FavorabilityRuleService.PlayerBountyCompleteLowDelta;
        public const float PlayerBountyLowReward = FavorabilityRuleService.PlayerBountyLowReward;
        public const float WorkerBountyCompleteDelta = FavorabilityRuleService.WorkerBountyCompleteDelta;
        public const float TradeSuccessBuyerDelta = FavorabilityRuleService.TradeSuccessBuyerDelta;
        public const float TradeSuccessSellerDelta = FavorabilityRuleService.TradeSuccessSellerDelta;
        public const float TradeRejectDelta = FavorabilityRuleService.TradeRejectDelta;
        public const float ConversationDelta = FavorabilityRuleService.ConversationDelta;
        public const float ConversationDailyCap = FavorabilityRuleService.ConversationDailyCap;
        public const float ProximityPerTick = FavorabilityRuleService.ProximityPerTick;
        public const float ProximityPerTickWithPlayer = FavorabilityRuleService.ProximityPerTickWithPlayer;
        public const float ProximityMaxPerPair = FavorabilityRuleService.ProximityMaxPerPair;

        // ---- 空间/时间调参 ----
        /// <summary>接近扫描节流间隔（秒）。</summary>
        public const float ProximityTickInterval = 3f;

        /// <summary>接近判定半径（地图格）。</summary>
        public const float ProximityRadiusMapTiles = 4f;

        /// <summary>Player 救危加分冷却（秒/Worker）。</summary>
        public const float HelpCoolDownSeconds = 30f;

        /// <summary>游戏日秒数（对话每日限额按游戏日重置）。</summary>
        public const float GameDaySeconds = 600f;

        // ---- HUD ----
        /// <summary>好感度 HUD 显示/隐藏热键（F11）。</summary>
        public const KeyCode HudToggleKey = InputKeyConstant.ToggleFavorabilityHud;

        /// <summary>好感度 HUD 根节点名（Editor 创建/运行时查找用）。</summary>
        public const string HudRootName = "FavorabilityHUD";

        /// <summary>好感度 HUD 文本子节点名。</summary>
        public const string HudTextName = "FavorabilityText";

        /// <summary>好感度 HUD 刷新间隔（秒）。</summary>
        public const float HudRefreshInterval = 0.5f;
    }
}
