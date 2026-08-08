namespace LAB2D.Tool
{
    using LAB2D.Core;
    using LAB2D.Domain.Gameplay;
    using LAB2D.Map;

    /// <summary>
    /// 地形效果工具类。
    /// 负责适配层查询和展示文本构建。
    /// 所有游戏规则逻辑委托给 TerrainEffectRuleService。
    /// </summary>
    public static class TerrainEffectTool
    {
        private static readonly TerrainEffectRuleService RuleService = new TerrainEffectRuleService();

        /// <summary>
        /// 安全获取玩家移速倍率。
        /// </summary>
        public static float GetPlayerMoveSpeedMultiplier(int terrainId)
        {
            var db = ServiceLocator.Get<TerrainConfigDatabase>();
            if (db == null) return TerrainEffectRuleService.DefaultMoveSpeedMultiplier;
            return db.GetPlayerMoveSpeedMultiplier(terrainId);
        }

        /// <summary>
        /// 安全获取工人移速倍率。
        /// </summary>
        public static float GetWorkerMoveSpeedMultiplier(int terrainId)
        {
            var db = ServiceLocator.Get<TerrainConfigDatabase>();
            if (db == null) return TerrainEffectRuleService.DefaultMoveSpeedMultiplier;
            return db.GetWorkerMoveSpeedMultiplier(terrainId);
        }

        /// <summary>
        /// 安全获取敌人移速倍率。
        /// </summary>
        public static float GetEnemyMoveSpeedMultiplier(int terrainId)
        {
            var db = ServiceLocator.Get<TerrainConfigDatabase>();
            if (db == null) return TerrainEffectRuleService.DefaultMoveSpeedMultiplier;
            return db.GetEnemyMoveSpeedMultiplier(terrainId);
        }

        /// <summary>
        /// 安全获取工人疲劳衰减倍率。
        /// </summary>
        public static float GetWorkerTiredDecayMultiplier(int terrainId)
        {
            var db = ServiceLocator.Get<TerrainConfigDatabase>();
            if (db == null) return TerrainEffectRuleService.DefaultTiredDecayMultiplier;
            return db.GetWorkerTiredDecayMultiplier(terrainId);
        }

        /// <summary>
        /// 安全获取工人饥饿衰减倍率。
        /// </summary>
        public static float GetWorkerHungryDecayMultiplier(int terrainId)
        {
            var db = ServiceLocator.Get<TerrainConfigDatabase>();
            if (db == null) return TerrainEffectRuleService.DefaultHungryDecayMultiplier;
            return db.GetWorkerHungryDecayMultiplier(terrainId);
        }

        /// <summary>
        /// 构建地形效果摘要文本。
        /// </summary>
        public static string BuildEffectSummary(int terrainId)
        {
            var db = ServiceLocator.Get<TerrainConfigDatabase>();
            var config = db?.GetById(terrainId);
            if (config == null) return "未知地形";

            var e = config.effectData;
            if (e == null) return $"{config.name}：无特殊效果";

            return $"{config.name}效果\n" +
                $"玩家移速: {e.playerMoveSpeedMultiplier:0.00}x\n" +
                $"工人移速: {e.workerMoveSpeedMultiplier:0.00}x\n" +
                $"敌人移速: {e.enemyMoveSpeedMultiplier:0.00}x\n" +
                $"疲劳消耗: {e.workerTiredDecayMultiplier:0.00}x\n" +
                $"饥饿消耗: {e.workerHungryDecayMultiplier:0.00}x";
        }
    }
}
