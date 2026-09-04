namespace LAB2D.Core
{
    using LAB2D.Gameplay;
    using LAB2D.UI;
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
            Core.ServiceLocator.Get<PanelController>().Show(CreateOrJoinPanel.Instance);

            // 背包面板
            BackpackPanel.Instance.Panel.SetActive(true);
            BackpackPanel.Instance.Panel.SetActive(false);

            // 殖民地运营指挥中心
            ColonyCommandCenterHUD.EnsureRuntimePanel();

            // 天气玩法 HUD (F4)
            WeatherGameplayHUD.EnsureRuntimePanel();

            // 工人状态 HUD (F5)
            WorkerConditionHUD.EnsureRuntimePanel();

            // 工人补给 HUD (F6)
            WorkerSupplyHUD.EnsureRuntimePanel();

            // 工人任务队列 HUD (F7)
            WorkerTaskQueueHUD.EnsureRuntimePanel();

            // 好感度 HUD (F11)
            FavorabilityHUD.EnsureRuntimePanel();

            // 成就系统（数据初始化由 IInitializable 链路完成）
            AchievementPopup.EnsureRuntimePopup();
            AchievementPanel.EnsureRuntimePanel();

            // 浮动战斗文字系统
            Core.ServiceLocator.Get<FloatingTextManager>().EnsureInitialized();

            // 主动技能系统（数据初始化由 IInitializable 链路完成）
            SkillHUD.EnsureRuntimePanel();

            // 装备掉落稀有度系统（数据初始化由 IInitializable 链路完成）
            EquipmentComparePopup.EnsureRuntimePopup();
            EquipmentPanel.EnsureRuntimePanel();

            // 附近道具拾取列表
            NearbyItemPickupHUD.EnsureRuntimePanel();

            // 任务栏列表 HUD (数字6键)
            TaskBoardHUD.EnsureRuntimePanel();

            // 修仙面板 (K键，纯代码构建)
            CultivationPanel.EnsureRuntimePanel();

            // 科技面板 (T键，纯代码构建)
            TechPanel.EnsureRuntimePanel();

            // 山门核心 HUD (G键，纯代码构建)
            MountainGateHUD.EnsureRuntimePanel();

            // 本局天机 HUD (H键，纯代码构建)
            SessionModifierHUD.EnsureRuntimePanel();

            // 附近交战提示条 (B 键加入战斗的入口提示，显隐由 GlobalInputProcessor 驱动)
            BattlePromptHUD.EnsureRuntimePanel();
        }
    }
}
