namespace LAB2D.Core
{
    using LAB2D.Gameplay;
    using LAB2D.UI.Panel;

    /// <summary>
    /// 全局面板初始化器 — 从 GlobalInit 提取的职责。
    /// 负责在游戏启动时创建并初始化所有 HUD、弹窗和面板的运行时实例。
    /// 纯静态工具类，不持有状态。
    /// </summary>
    public static class GlobalPanelInitializer
    {
        /// <summary>
        /// 初始化所有面板和 HUD 的运行时实例。
        /// 由 GlobalInit.Start() 调用。
        /// </summary>
        public static void InitializeAll()
        {
            // 基础面板
            ForegroundPanel.Instance.Init();
            PanelController.Instance.Show(CreateOrJoinPanel.Instance);

            // 背包面板
            BackpackMenuPanel.Instance.Panel.SetActive(true);
            BackpackMenuPanel.Instance.Panel.SetActive(false);

            // A006 殖民地运营指挥中心
            ColonyCommandCenterHUD.EnsureRuntimePanel();

            // F012 天气玩法 HUD (F4)
            WeatherGameplayHUD.EnsureRuntimePanel();

            // 工人状态 HUD (F5)
            WorkerConditionHUD.EnsureRuntimePanel();

            // 工人补给 HUD (F6)
            WorkerSupplyHUD.EnsureRuntimePanel();

            // 工人任务队列 HUD (F7)
            WorkerTaskQueueHUD.EnsureRuntimePanel();

            // A007 成就系统
            AchievementManager.Instance.Initialize();
            AchievementPopup.EnsureRuntimePopup();
            AchievementPanel.EnsureRuntimePanel();

            // A009 浮动战斗文字系统
            FloatingTextManager.Instance.EnsureInitialized();

            // A008 主动技能系统
            SkillManager.Instance.Initialize();
            SkillHUD.EnsureRuntimePanel();

            // A010 装备掉落稀有度系统
            EnemyLootManager.Instance.Initialize();
            EquipmentComparePopup.EnsureRuntimePopup();
            EquipmentPanel.EnsureRuntimePanel();

            // A011 附近道具拾取列表
            NearbyItemPickupHUD.EnsureRuntimePanel();
        }
    }
}
