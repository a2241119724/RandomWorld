namespace LAB2D
{
    using LAB2D.AI.Dialogue.Prompt;
    using LAB2D.Character;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using LAB2D.Gameplay;
    using LAB2D.UnityAdapter;
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

        public void Awake()
        {
            Instance = this;
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
            // 全局协调器 — 必须在其他服务之前注册
            ServiceLocator.Register(new MapInitCoordinator());

            ServiceLocator.Register<ITipService>(this);

            ServiceLocator.Register<IGameTime>(new UnityGameTime());
            ServiceLocator.Register<IGameLogger>(new UnityLogger());
            ServiceLocator.Register<IEnemySpawnService>(new UnityEnemySpawnAdapter());
            ServiceLocator.Register<IItemDefinitionProvider>(new UnityItemDefinitionAdapter());

            UnityMapAdapter mapAdapter = new UnityMapAdapter();
            ServiceLocator.Register<IMapWalkabilityQuery>(mapAdapter);
            ServiceLocator.Register<IMapSpawnPointProvider>(mapAdapter);

            ServiceLocator.Register(LogManager.Instance);
            ServiceLocator.Register(ResourceManager.Instance);
            ServiceLocator.Register(CoroutineManager.Instance);
            ServiceLocator.Register(WeatherManager.Instance);
            ServiceLocator.Register(ArchiveManager.Instance);
            ServiceLocator.Register(FrameControl.Instance);
            ServiceLocator.Register(NetworkConnect.Instance);
            ServiceLocator.Register(NameGenerator.Instance);

            ServiceLocator.Register(ItemDataManager.Instance);
            ServiceLocator.Register(DropDataManager.Instance);
            ServiceLocator.Register(EnvironmentManager.Instance);

            ServiceLocator.Register(TileMap.Instance);
            ServiceLocator.Register(BuildMap.Instance);
            ServiceLocator.Register(ResourceMap.Instance);
            ServiceLocator.Register(ItemMap.Instance);
            ServiceLocator.Register(GatherMap.Instance);
            ServiceLocator.Register(IsAvailableMap.Instance);

            ServiceLocator.Register(InventoryManager.Instance);
            ServiceLocator.Register(DropManager.Instance);
            ServiceLocator.Register(RoomManager.Instance);
            ServiceLocator.Register(FurnitureManager.Instance);
            ServiceLocator.Register(FarmlandManager.Instance);
            ServiceLocator.Register(ItemInstanceFactory.Instance);

            ServiceLocator.Register(PlayerManager.Instance);
            ServiceLocator.Register(EnemyManager.Instance);
            ServiceLocator.Register(WorkerManager.Instance);

            ServiceLocator.Register(WaveManager.Instance);
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
            ServiceLocator.Register(ColonyCommandCenterManager.Instance);
            ServiceLocator.Register<IColonyCommandCenterService>(ColonyCommandCenterManager.Instance);
            ServiceLocator.Register(ItemCollectionTracker.Instance);
            ServiceLocator.Register(WeatherGameplayEffect.Instance);
            ServiceLocator.Register<IWeatherGameplayService>(WeatherGameplayEffect.Instance);

            ServiceLocator.Register(PanelController.Instance);
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
                new GlobalInputProcessor(),
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
