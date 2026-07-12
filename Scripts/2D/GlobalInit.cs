namespace LAB2D
{
    using LAB2D.AI.Dialogue.Prompt;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using LAB2D.Gameplay;
    using UnityEngine;

    /// <summary>
    /// 全局初始化 — 游戏入口点。
    /// 负责初始化日志、注册所有单例到 ServiceLocator、启动面板系统。
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class GlobalInit : MonoBehaviour, ITipService
    {
        private readonly bool initPanel = true;
        private WorkerUpdateSystem workerUpdateSystem;

        /// <summary>
        /// 单例。保持向后兼容，新代码应使用 ServiceLocator.Get&lt;ITipService&gt;()。
        /// </summary>
        public static GlobalInit Instance { get; private set; }

        public void Awake()
        {
            Instance = this;
            this.workerUpdateSystem = new WorkerUpdateSystem();
            LogManager.Instance.Init();
            Application.targetFrameRate = GlobalData.MaxFrame;

            // 注册所有单例到 ServiceLocator 以实现依赖注入
            this.RegisterServices();

            // 预热 PromptBuilder，避免首次对话时触发 Resources.LoadAll 造成卡顿
            PromptBuilder.Instance.Init();
        }

        /// <summary>
        /// 将所有 Singleton 实例注册到 ServiceLocator。
        /// 注册顺序：基础设施 → 数据 → 地图 → 角色 → 游戏玩法 → UI。
        /// </summary>
        private void RegisterServices()
        {
            // 基础设施服务
            ServiceLocator.Register<ITipService>(this);
            ServiceLocator.Register(LogManager.Instance);
            ServiceLocator.Register(ResourceManager.Instance);
            ServiceLocator.Register(CoroutineManager.Instance);
            ServiceLocator.Register(WeatherManager.Instance);
            ServiceLocator.Register(ArchiveManager.Instance);
            ServiceLocator.Register(FrameControl.Instance);
            ServiceLocator.Register(NetworkConnect.Instance);
            ServiceLocator.Register(NameGenerator.Instance);

            // 数据服务
            ServiceLocator.Register(ItemDataManager.Instance);
            ServiceLocator.Register(DropDataManager.Instance);
            ServiceLocator.Register(EnvironmentManager.Instance);

            // 地图服务
            ServiceLocator.Register(TileMap.Instance);
            ServiceLocator.Register(BuildMap.Instance);
            ServiceLocator.Register(ResourceMap.Instance);
            ServiceLocator.Register(ItemMap.Instance);
            ServiceLocator.Register(GatherMap.Instance);
            ServiceLocator.Register(IsAvailableMap.Instance);

            // 物品服务
            ServiceLocator.Register(InventoryManager.Instance);
            ServiceLocator.Register(DropManager.Instance);
            ServiceLocator.Register(RoomManager.Instance);
            ServiceLocator.Register(FurnitureManager.Instance);
            ServiceLocator.Register(FarmlandManager.Instance);
            ServiceLocator.Register(ItemInstanceFactory.Instance);

            // 角色管理器
            ServiceLocator.Register(PlayerManager.Instance);
            ServiceLocator.Register(EnemyManager.Instance);
            ServiceLocator.Register(WorkerManager.Instance);

            // 游戏玩法服务
            ServiceLocator.Register(WaveManager.Instance);
            ServiceLocator.Register(WaveBossRewardManager.Instance);
            ServiceLocator.Register(AchievementManager.Instance);
            ServiceLocator.Register(SkillManager.Instance);
            ServiceLocator.Register(ComboBonusManager.Instance);
            ServiceLocator.Register(DeathPenaltyManager.Instance);
            ServiceLocator.Register(EnemyLootManager.Instance);
            ServiceLocator.Register(FloatingTextManager.Instance);
            ServiceLocator.Register(EquipmentBeamManager.Instance);
            ServiceLocator.Register(GameplaySessionStats.Instance);
            ServiceLocator.Register(SessionResultManager.Instance);
            ServiceLocator.Register(PlayerVitalAlertManager.Instance);
            ServiceLocator.Register(WorkerConditionManager.Instance);
            ServiceLocator.Register(WorkerSupplyIssueManager.Instance);
            ServiceLocator.Register(WorkerTaskCongestionAdvisor.Instance);
            ServiceLocator.Register(WorkerEfficiencyTracker.Instance);
            ServiceLocator.Register(ColonyCommandCenterManager.Instance);
            ServiceLocator.Register(ItemCollectionTracker.Instance);
            ServiceLocator.Register(WeatherGameplayEffect.Instance);

            // UI 服务
            ServiceLocator.Register(PanelController.Instance);
            // AsyncProgressUI 通过自身 Awake() 自注册到 ServiceLocator
        }

        public void Start()
        {
            if (this.initPanel)
            {
                if (PanelController.Instance == null)
                {
                    LogManager.Instance.Log("manager Not Found!!!", LogManager.LogLevelEnum.Error);
                    return;
                }

                GlobalPanelInitializer.InitializeAll();
            }
        }

        public void Update()
        {
            this.WorkerUpdate();

            // 全局输入处理（ESC 面板切换、鼠标点击关闭物品信息）
            GlobalInputProcessor.ProcessInput();

            // 成就系统：更新进度、检查解锁、F7 切换面板
            this.ProcessAchievements();

            EnvironmentManager.Instance.UpdateEnergy();

            // 玩家生命危险提示
            PlayerVitalAlertManager.Instance.Tick();
        }

        /// <summary>
        /// 成就系统每帧轮询：更新进度、展示待解锁弹窗、F7 切换面板。
        /// </summary>
        private void ProcessAchievements()
        {
            AchievementManager mgr = AchievementManager.Instance;
            if (mgr == null || !mgr.IsInitialized)
            {
                return;
            }

            mgr.UpdateProgressAll();

            // 检查是否有待展示的解锁弹窗
            if (mgr.HasPendingUnlock)
            {
                AchievementData pending = mgr.PeekPendingUnlock();
                if (pending != null && AchievementPopup.RuntimeInstance != null)
                {
                    AchievementPopup.RuntimeInstance.Show(pending);
                }
            }

            // F7 切换成就面板
            if (!LAB2D.Tool.Tool.IsUIInputActive() && Input.GetKeyDown(InputKeyConstant.ToggleWorkerTaskAndAchievementHud))
            {
                AchievementPanel.RuntimeInstance?.TogglePanel();
            }
        }

        /// <summary>
        /// 展示通知.
        /// </summary>
        /// <param name="text">通知内容.</param>
        public void ShowTip(string text)
        {
            GameObject g = ResourceManager.Instance.Instantiate(PrefabConstant.TIP);
            if (g == null)
            {
                return;
            }

            g.GetComponent<TipUI>().SetText(text);
            g.transform.SetParent(this.transform, false);

            // 由于实例化产生形状变化,重新设置
            // g.transform.localScale = Vector3.zero;
            // RectTransform rt = g.GetComponent<RectTransform>();
            // rt.offsetMin = Vector2.zero;
            // rt.offsetMax = Vector2.zero;
            // Vector3 v = rt.localPosition; // 相对坐标
            // rt.localPosition = new Vector3(v.x, v.y, 0);
        }

        private void WorkerUpdate()
        {
            // 委托给独立 Tick 系统（Phase 7 重构）
            this.workerUpdateSystem.Tick(Time.deltaTime);
        }
    }
}
