namespace LAB2D
{
    using LAB2D.AI.Dialogue.Core;
    using LAB2D.AI.Dialogue.Memory;
    using LAB2D.AI.Dialogue.Prompt;
    using LAB2D.AI.Dialogue.RAG;
    using LAB2D.AI.Dialogue.UI;
    using LAB2D.Character;
    using LAB2D.Character.Enemy;
    using LAB2D.Character.Player;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using LAB2D.Gameplay;
    using LAB2D.Item.Backpack.Equipment.Weapon;
    using LAB2D.Map;
    using LAB2D.Network;
    using LAB2D.UI.Action;
    using LAB2D.UI.Panel;
    using LAB2D.UnityAdapter;
    using PimDeWitte.UnityMainThreadDispatcher;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 全局初始化 — 游戏入口点。
    /// 负责初始化日志、注册所有单例到 ServiceLocator、启动面板系统。
    /// Update 循环通过 ITickable 自动发现机制驱动，不再硬编码调用。
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class GlobalInit : MonoBehaviour, ITipService
    {
        private readonly bool initPanel = true;
        private List<ITickable> orderedTickables;
        private List<IInitializable> orderedInitializables;

        public static GlobalInit Instance { get; private set; }

        /// <summary>
        /// 在场景加载前注册所有 Singleton&lt;T&gt; 和 ASingletonSaveData&lt;T&gt; 服务。
        /// 这些服务通过 new T() 自创建，不依赖 MonoBehaviour Awake 生命周期，
        /// 因此可以提前注册，避免 OnEnable 时序问题。
        /// MonoBehaviour 服务（约 11 个）仍在 RegisterServices() 中延迟注册。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSafeServices()
        {
            ServiceLocator.Register(LogManager.Instance);
            // TerrainConfigDatabase 必须在 ResourceManager 之前注册（ResourceManager 构造函数使用它）。
            ServiceLocator.Register(new TerrainConfigDatabase());
            ServiceLocator.Register(ResourceManager.Instance);
            ServiceLocator.Register(ArchiveManager.Instance);
            ServiceLocator.Register(FrameControl.Instance);
            ServiceLocator.Register(NameGenerator.Instance);
            ServiceLocator.Register(EnvironmentManager.Instance);
            // IGameTime + IGameLogger 必须在任何可能访问时间的 Singleton 之前注册。
            // UnityGameTime 仅封装 UnityEngine.Time，无场景依赖，可在 BeforeSceneLoad 安全注册。
            ServiceLocator.Register<IGameTime>(new UnityGameTime());
            ServiceLocator.Register<IGameLogger>(new UnityLogger());
            ServiceLocator.Register(InventoryManager.Instance);
            ServiceLocator.Register(DropManager.Instance);
            ServiceLocator.Register(RoomManager.Instance);
            ServiceLocator.Register(FurnitureManager.Instance);
            ServiceLocator.Register(FarmlandManager.Instance);
            ServiceLocator.Register(ItemInstanceFactory.Instance);
            // CharacterCreator 必须在对应的 Manager 之前注册，
            // 因为 CharacterManager 构造函数通过 ServiceLocator.Get<CC>() 获取 Creator。
            ServiceLocator.Register(PlayerCreator.Instance);
            ServiceLocator.Register(EnemyCreator.Instance);
            ServiceLocator.Register(WorkerCreator.Instance);
            ServiceLocator.Register(PlayerManager.Instance);
            ServiceLocator.Register(EnemyManager.Instance);
            ServiceLocator.Register(WorkerManager.Instance);
            ServiceLocator.Register(WaveManager.Instance);
            ServiceLocator.Register<IWaveStateProvider>(WaveManager.Instance);
            ServiceLocator.Register(WaveBossRewardManager.Instance);
            ServiceLocator.Register(AchievementManager.Instance);
            ServiceLocator.Register(SkillManager.Instance);
            ServiceLocator.Register<ISkillManager>(SkillManager.Instance);
            ServiceLocator.Register(ComboBonusManager.Instance);
            ServiceLocator.Register(DeathPenaltyManager.Instance);
            ServiceLocator.Register(EnemyLootManager.Instance);
            ServiceLocator.Register(FloatingTextManager.Instance);
            ServiceLocator.Register(EquipmentBeamManager.Instance);
            ServiceLocator.Register(GameplaySessionStats.Instance);
            ServiceLocator.Register(SessionResultManager.Instance);
            ServiceLocator.Register(PlayerVitalAlertManager.Instance);
            ServiceLocator.Register<IPlayerVitalAlertManager>(PlayerVitalAlertManager.Instance);
            ServiceLocator.Register(WorkerConditionManager.Instance);
            ServiceLocator.Register<IWorkerConditionManager>(WorkerConditionManager.Instance);
            ServiceLocator.Register(WorkerSupplyIssueManager.Instance);
            ServiceLocator.Register<IWorkerSupplyIssueManager>(WorkerSupplyIssueManager.Instance);
            ServiceLocator.Register(WorkerTaskCongestionAdvisor.Instance);
            ServiceLocator.Register<IWorkerTaskCongestionAdvisor>(WorkerTaskCongestionAdvisor.Instance);
            ServiceLocator.Register(WorkerEfficiencyTracker.Instance);
            ServiceLocator.Register(CurrencyManager.Instance);
            ServiceLocator.Register(ColonyCommandCenterManager.Instance);
            ServiceLocator.Register<IColonyCommandCenterService>(ColonyCommandCenterManager.Instance);
            ServiceLocator.Register(ItemCollectionTracker.Instance);
            ServiceLocator.Register(WeatherGameplayEffect.Instance);
            ServiceLocator.Register<IWeatherGameplayService>(WeatherGameplayEffect.Instance);
            ServiceLocator.Register(PanelController.Instance);
            ServiceLocator.Register(PromptBuilder.Instance);
            ServiceLocator.Register(DialogueManager.Instance);
            ServiceLocator.Register(DialogueMemoryManager.Instance);
            ServiceLocator.Register(GameKnowledgeRetriever.Instance);
            ServiceLocator.Register(AttackEffectManager.Instance);
            ServiceLocator.Register(EventBus.Instance);
            ServiceLocator.Register(SelectManagerPool.Instance);
            ServiceLocator.Register(WaveEventFeedback.Instance);
            ServiceLocator.Register(OfflineNetworkView.Instance);
            ServiceLocator.Register(NullSyncSender.Instance);
        }

        public void Awake()
        {
            Instance = this;
            ServiceLocator.Register(this);
            Application.targetFrameRate = GlobalData.MaxFrame;

            LogManager.Instance.Init();

            this.RegisterServices();
            this.BuildTickableList();
            this.BuildInitializableList();

            this.gameObject.AddComponent<CharacterDamageUIPresenter>();

            PromptBuilder.Instance.Init();
        }

        private void RegisterServices()
        {
            ServiceLocator.Register(new MapInitCoordinator());
            ServiceLocator.Register<ITipService>(this);

            // IGameTime + IGameLogger 已在 RegisterSafeServices() 中提前注册
            ServiceLocator.Register<IEnemySpawnService>(new UnityEnemySpawnAdapter());
            ServiceLocator.Register<IItemDefinitionProvider>(new UnityItemDefinitionAdapter());

            UnityMapAdapter mapAdapter = new UnityMapAdapter();
            ServiceLocator.Register<IMapWalkabilityQuery>(mapAdapter);
            ServiceLocator.Register<IMapSpawnPointProvider>(mapAdapter);

            ServiceLocator.Register(CoroutineManager.Instance);
            ServiceLocator.Register(WeatherManager.Instance);
            ServiceLocator.Register(NetworkConnect.Instance);
            ServiceLocator.Register(ItemDataManager.Instance);
            ServiceLocator.Register(DropDataManager.Instance);
            ServiceLocator.Register(WorkerTaskManager.Instance);

            ServiceLocator.Register(TileMap.Instance);
            ServiceLocator.Register(BuildMap.Instance);
            ServiceLocator.Register(ResourceMap.Instance);
            ServiceLocator.Register(ItemMap.Instance);
            ServiceLocator.Register(GatherMap.Instance);
            ServiceLocator.Register(IsAvailableMap.Instance);

            // ABasePanel<T> 子类 — 构造函数调用 Init() 依赖 GameObject.FindGameObjectWithTag，
            // 必须在 Awake 阶段（场景加载后）注册，不能放在 RegisterSafeServices（BeforeSceneLoad）。
            ServiceLocator.Register(ItemInfoPanel.Instance);
            ServiceLocator.Register(ForegroundPanel.Instance);
            ServiceLocator.Register(BuildMenuPanel.Instance);
            ServiceLocator.Register(PauseMenuPanel.Instance);
            ServiceLocator.Register(SettingMenuPanel.Instance);
            ServiceLocator.Register(NewOrContinuePanel.Instance);
            ServiceLocator.Register(CreateDataPanel.Instance);
            ServiceLocator.Register(CreateMenuPanel.Instance);
            ServiceLocator.Register(JoinMenuPanel.Instance);
            ServiceLocator.Register(AsyncProgressPanel.Instance);
            ServiceLocator.Register(CreateOrJoinPanel.Instance);
            ServiceLocator.Register(WorkerTaskTogglePanel.Instance);
            ServiceLocator.Register(InventoryMenuPanel.Instance);
            ServiceLocator.Register(AIChatPanel.Instance);
            ServiceLocator.Register(DialoguePanel.Instance);
            ServiceLocator.Register(UnityMainThreadDispatcher.Instance);
        }

        /// <summary>
        /// 构建有序 Tick 列表。顺序决定 Tick 调用顺序。
        /// 新增 ITickable 实现时在此添加即可，无需修改 Update()。
        /// </summary>
        private void BuildTickableList()
        {
            this.orderedTickables = new List<ITickable>
            {
                new WorkerUpdateSystem(),
                ServiceLocator.Get<AchievementManager>(),
                new GlobalInputProcessor(),
                ServiceLocator.Get<WorkerTaskManager>(),
                ServiceLocator.Get<EnvironmentManager>(),
                ServiceLocator.Get<PlayerVitalAlertManager>(),
            };
        }

        /// <summary>
        /// 构建有序 Init 列表。在面板初始化前调用，确保数据层先就绪。
        /// </summary>
        private void BuildInitializableList()
        {
            this.orderedInitializables = new List<IInitializable>
            {
                ServiceLocator.Get<AchievementManager>(),
                ServiceLocator.Get<SkillManager>(),
                ServiceLocator.Get<EquipmentBeamManager>(),
                ServiceLocator.Get<EnemyLootManager>(),
                ServiceLocator.Get<ComboBonusManager>(),
            };
        }

        public void Start()
        {
            foreach (IInitializable initializable in this.orderedInitializables)
            {
                initializable.Initialize();
            }

            if (this.initPanel)
            {
                if (ServiceLocator.Get<PanelController>() == null)
                {
                    AWorkerTask.LogProvider("manager Not Found!!!", LogManager.LogLevelEnum.Error);
                    return;
                }

                GlobalPanelInitializer.InitializeAll();
            }
        }

        public void Update()
        {
            float dt = Time.deltaTime;
            foreach (ITickable tickable in this.orderedTickables)
            {
                tickable.Tick(dt);
            }
        }

        public void ShowTip(string text)
        {
            GameObject g = ServiceLocator.Get<ResourceManager>().Instantiate(PrefabConstant.TIP);
            if (g == null)
            {
                return;
            }

            g.GetComponent<TipUI>().SetText(text);
            g.transform.SetParent(this.transform, false);
        }
    }
}
