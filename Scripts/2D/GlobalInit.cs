namespace LAB2D
{
    using System.Collections.Generic;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using LAB2D.Gameplay;
    using LAB2D.UI.Panel;
    using UnityEngine;

    /// <summary>
    /// 全局初始化 — 游戏入口点。
    /// 负责初始化日志、注册所有单例到 ServiceLocator、启动面板系统。
    /// </summary>
    public class GlobalInit : MonoBehaviour, ITipService
    {
        private readonly bool initPanel = true;
        private List<IBasePanel> dontClosePanels; // ESC不可关闭的面板
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

            this.dontClosePanels = new ()
            {
                ForegroundPanel.Instance,
                CreateOrJoinPanel.Instance,
                CreateDataPanel.Instance,
                AsyncProgressPanel.Instance,
                NewOrContinuePanel.Instance,
                PauseMenuPanel.Instance,
            };
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
            ServiceLocator.Register(AsyncProgressUI.Instance);
        }

        public void Start()
        {
            // init panel
            if (this.initPanel)
            {
                ForegroundPanel.Instance.Init();
                if (PanelController.Instance == null)
                {
                    LogManager.Instance.Log("manager Not Found!!!", LogManager.LogLevelEnum.Error);
                    return;
                }

                PanelController.Instance.Show(CreateOrJoinPanel.Instance);

                // 初始化背包
                BackpackMenuPanel.Instance.Panel.SetActive(true);
                BackpackMenuPanel.Instance.Panel.SetActive(false);

                // A006 殖民地运营指挥中心使用独立运行时 HUD，避免直接改动复杂场景 UI 层级。
                ColonyCommandCenterHUD.EnsureRuntimePanel();

                // A007 成就系统：初始化管理器、弹窗和面板
                AchievementManager.Instance.Initialize();
                AchievementPopup.EnsureRuntimePopup();
                AchievementPanel.EnsureRuntimePanel();

                // A009 浮动战斗文字系统：初始化管理器和对象池
                FloatingTextManager.Instance.EnsureInitialized();

                // A008 主动技能系统：初始化技能管理器和 HUD
                SkillManager.Instance.Initialize();
                SkillHUD.EnsureRuntimePanel();

                // A010 装备掉落稀有度系统：初始化管理器、对比弹窗和装备面板
                EnemyLootManager.Instance.Initialize();
                EquipmentComparePopup.EnsureRuntimePopup();
                EquipmentPanel.EnsureRuntimePanel();

                // A011 附近道具拾取列表：初始化拾取 HUD
                NearbyItemPickupHUD.EnsureRuntimePanel();
            }
        }

        public void Update()
        {
            this.WorkerUpdate();

            // 退出界面(除了ForegroundPanel,CreateOrJoinPanel,CreateMenuPanel,CreateDataPanel,AsyncProgressPanel)
            if (!Tool.IsUIInputActive() && Input.GetKeyDown(InputKeyConstant.CloseOrBuildMenu))
            {
                if (PanelController.Instance.Panels.Count == 0)
                {
                    BuildingUI.Instance.gameObject.SetActive(false);
                    PanelController.Instance.Show(BuildMenuPanel.Instance);
                    IsAvailableMap.Instance.ClearShow();
                }
                else
                {
                    // 不能关闭下面面板
                    if (PanelController.Instance.Panels.Peek() == ItemInfoPanel.Instance)
                    {
                        ItemInfoUI.Instance.Init();
                    }

                    PanelController.Instance.Panels.Peek().OnClick_Back();
                }
            }

            // A007 成就系统：更新进度并检查解锁
            AchievementManager mgr = AchievementManager.Instance;
            if (mgr != null && mgr.IsInitialized)
            {
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
                if (!Tool.IsUIInputActive() && Input.GetKeyDown(InputKeyConstant.ToggleWorkerTaskAndAchievementHud))
                {
                    AchievementPanel.RuntimeInstance?.TogglePanel();
                }
            }

            EnvironmentManager.Instance.UpdateEnergy();

            // F019 玩家生命危险提示：只读刷新本地玩家血量警戒，复用现有 Tip UI，不改变战斗或存档数据。
            PlayerVitalAlertManager.Instance.Tick();

            if (!Tool.IsUIInputActive() && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2)))
            {
                // 关闭ItemInfo面板
                if (PanelController.Instance.Panels.Count > 0
                    && PanelController.Instance.Panels.Peek() == ItemInfoPanel.Instance)
                {
                    ItemInfoUI.Instance.Init();
                    PanelController.Instance.Close();
                }
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

    /// <summary>
    /// 带init方法的MonoBehaviour.
    /// </summary>
    public abstract class MonoBehaviourInit : MonoBehaviour
    {
        /// <summary>
        /// 初始化方法.
        /// </summary>
        public virtual void Init()
        {
        }
    }
}
