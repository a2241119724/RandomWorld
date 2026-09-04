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
    using LAB2D.UI;
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
            ServiceLocator.Register(AnimationManager.Instance);
            ServiceLocator.Register(ArchiveManager.Instance);
            ServiceLocator.Register(FrameControl.Instance);
            ServiceLocator.Register(NameGenerator.Instance);
            ServiceLocator.Register(EnvironmentManager.Instance);
            ServiceLocator.Register(GameTimeManager.Instance);
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
            ServiceLocator.Register(FavorabilityManager.Instance);
            ServiceLocator.Register(MarketService.Instance);
            ServiceLocator.Register(new PlayerBountyService());
            ServiceLocator.Register(TaskBoardManager.Instance);
            ServiceLocator.Register(MountainGateManager.Instance);
            ServiceLocator.Register(new ShopNPCGenerator());

            // 注入所有权名字解析：0=Player, >0=Worker名字
            Domain.Worker.ItemOwnershipService.OwnerNameProvider = (ownerId) =>
            {
                if (ownerId == 0) return "Player";
                var cm = ServiceLocator.Get<CurrencyManager>();
                var worker = cm?.FindWorker(ownerId);
                return worker != null ? worker.name : $"Worker#{ownerId}";
            };

            ServiceLocator.Register(ColonyCommandCenterManager.Instance);
            ServiceLocator.Register<IColonyCommandCenterService>(ColonyCommandCenterManager.Instance);
            ServiceLocator.Register(ItemCollectionTracker.Instance);
            ServiceLocator.Register(WeatherGameplayEffect.Instance);
            ServiceLocator.Register<IWeatherGameplayService>(WeatherGameplayEffect.Instance);
            ServiceLocator.Register(TemperatureEffect.Instance);
            ServiceLocator.Register<ITemperatureEffectService>(TemperatureEffect.Instance);
            ServiceLocator.Register(TerrainEffectManager.Instance);
            ServiceLocator.Register<ITerrainEffectService>(TerrainEffectManager.Instance);
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

            // 成长系统接线：词条随机提供者注入 + GrowthCollectProvider 接管（装备词条进属性管线）
            GrowthBonusService.Install();

            // 修仙系统（打坐/突破/灵根，IInitializable + ITickable 由下表驱动）
            ServiceLocator.Register(CultivationManager.Instance);

            // 武学功法系统（学习/激活内功/读档重建外功注册，IInitializable + ITickable 由下表驱动）
            ServiceLocator.Register(GongFaManager.Instance);

            // 科技系统（研究点产出/建筑解锁 gating，ITickable 由下表驱动；存档走 ASingletonSaveData）
            ServiceLocator.Register(TechManager.Instance);

            // 箭塔防御（已建成箭塔自动索敌射击，ITickable 由下表驱动；塔数据在 BuildMap 存档，无独立存档）
            ServiceLocator.Register(ArrowTowerManager.Instance);

            // 回合制战斗（G 键加入大世界交战：快照构建→回合结算→结果写回；
            // 交战检测由 GlobalInputProcessor 驱动，面板经 Manager 事件打开）
            ServiceLocator.Register(Gameplay.TurnBattle.TurnBattleManager.Instance);

            // 灵气环境（空间灵气浓度图：灵脉撒点/聚灵阵扫描/浓度查询，IInitializable + ITickable 由下表驱动；灵脉点集走 ASingletonSaveData）
            ServiceLocator.Register(LingQiManager.Instance);

            // 异能觉醒系统（受击 roll 觉醒/读档重建异能注册，IInitializable + ITickable 由下表驱动）
            ServiceLocator.Register(AwakenedPowerManager.Instance);

            // 防守夜调度（入夜按 DefenceDraftRuleService 派 WorkerDefendTask，IInitializable 由下表驱动）
            ServiceLocator.Register(WorkerDefenceManager.Instance);

            // 昼夜光照（驱动全局光强度/色温，ITickable 由下表驱动，排在 GameTimeManager 之后采样当帧新时间）
            ServiceLocator.Register(DayNightLightManager.Instance);
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
            ServiceLocator.Register(new WorkerMindService());
            ServiceLocator.Register(WorkerMindManager.Instance);

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
            ServiceLocator.Register(BuildPanel.Instance);
            ServiceLocator.Register(PausePanel.Instance);
            ServiceLocator.Register(SettingPanel.Instance);
            ServiceLocator.Register(NewOrContinuePanel.Instance);
            ServiceLocator.Register(CreateDataPanel.Instance);
            ServiceLocator.Register(CreatePanel.Instance);
            ServiceLocator.Register(JoinPanel.Instance);
            ServiceLocator.Register(AsyncProgressPanel.Instance);
            ServiceLocator.Register(CreateOrJoinPanel.Instance);
            ServiceLocator.Register(WorkerTaskTogglePanel.Instance);
            ServiceLocator.Register(InventoryPanel.Instance);
            ServiceLocator.Register(AIChatPanel.Instance);
            ServiceLocator.Register(DialoguePanel.Instance);
            ServiceLocator.Register(ShopPanel.Instance);
            ServiceLocator.Register(RoomListPanel.Instance);
            ServiceLocator.Register(TurnBattlePanel.Instance);
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
                // 时钟源最前：时间先推进，后续系统（温度/WorkerUpdate/修仙等）读到当帧新时间
                ServiceLocator.Get<GameTimeManager>(),
                // 全局光照紧跟时钟：采样当帧新时间驱动昼夜明暗/色温
                ServiceLocator.Get<DayNightLightManager>(),
                new WorkerUpdateSystem(),
                ServiceLocator.Get<AchievementManager>(),
                new GlobalInputProcessor(),
                ServiceLocator.Get<WorkerTaskManager>(),
                ServiceLocator.Get<TemperatureEffect>(),
                ServiceLocator.Get<EnvironmentManager>(),
                ServiceLocator.Get<PlayerVitalAlertManager>(),
                ServiceLocator.Get<FavorabilityManager>(),
                ServiceLocator.Get<WorkerMindManager>(),
                ServiceLocator.Get<CultivationManager>(),
                ServiceLocator.Get<GongFaManager>(),
                ServiceLocator.Get<AwakenedPowerManager>(),
                ServiceLocator.Get<TechManager>(),
                ServiceLocator.Get<ArrowTowerManager>(),
                // 灵气环境（聚灵阵 2s 节流重扫，浓度查询被动响应无 Tick 负担）
                ServiceLocator.Get<LingQiManager>(),
            };
        }

        /// <summary>
        /// 构建有序 Init 列表。在面板初始化前调用，确保数据层先就绪。
        /// </summary>
        private void BuildInitializableList()
        {
            this.orderedInitializables = new List<IInitializable>
            {
                // 时钟最先：读档时间落位后，后续系统初始化才能拿到正确相位/天数
                ServiceLocator.Get<GameTimeManager>(),
                ServiceLocator.Get<AchievementManager>(),
                ServiceLocator.Get<SkillManager>(),
                ServiceLocator.Get<CultivationManager>(),
                ServiceLocator.Get<GongFaManager>(),
                ServiceLocator.Get<AwakenedPowerManager>(),
                ServiceLocator.Get<WorkerDefenceManager>(),
                ServiceLocator.Get<EquipmentBeamManager>(),
                ServiceLocator.Get<EnemyLootManager>(),
                // 每局修饰符（有档恢复/无档 roll——LoadData 已跑时 Initialize 兜底幂等）
                ServiceLocator.Get<SessionModifierManager>(),
                ServiceLocator.Get<ComboBonusManager>(),
                // 灵气环境（订阅 OnMapReady 撒灵脉，读档路径在 LoadData 恢复/迁移）
                ServiceLocator.Get<LingQiManager>(),
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

            // 订阅地图初始化完成事件
            ServiceLocator.Get<MapInitCoordinator>().OnMapReady += this.OnMapReadyInitTaskBoard;
            ServiceLocator.Get<ShopNPCGenerator>().SubscribeToMapReady();
        }

        /// <summary>
        /// 地图就绪回调 — 初始化任务栏与山门核心位置（地图中心附近第一个可到达的空地）。
        /// 此时 TileMapDataLAB 已加载完成，可安全访问 Height/Width。
        /// </summary>
        private void OnMapReadyInitTaskBoard()
        {
            TileMap tileMap = ServiceLocator.Get<TileMap>();
            int centerX = tileMap.TileMapDataLAB.Height / 2;
            int centerY = tileMap.TileMapDataLAB.Width / 2;
            Vector3Int center = new Vector3Int(centerX, centerY, 0);

            // 山门核心先选位（胜负锚点），任务栏在其周边避开
            MountainGateManager.Instance.InitPosition(center);
            TaskBoardManager.Instance.InitPosition(center);
        }

        public void Update()
        {
            float dt = Time.deltaTime;
            foreach (ITickable tickable in this.orderedTickables)
            {
                tickable.Tick(dt);
            }

            // 默认隐藏的面板热键在此统一分发：面板 GameObject inactive 时自身 Update 不跑，
            // 检测写在面板内会导致热键永远无法唤醒面板（spec：HUD 热键全局分发避免 inactive 失效）
            if (UnityGlobalInputAdapter.GetHudToggleDown(InputKeyConstant.ToggleCultivationHud))
            {
                CultivationPanel.ToggleHotkey();
            }

            if (UnityGlobalInputAdapter.GetHudToggleDown(InputKeyConstant.ToggleTechHud))
            {
                TechPanel.ToggleHotkey();
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
