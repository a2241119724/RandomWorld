namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 全局初始化.
    /// </summary>
    public class GlobalInit : MonoBehaviour
    {
        private readonly bool initPanel = true;
        private List<IBasePanel> dontClosePanels; // ESC不可关闭的面板

        /// <summary>
        /// 单例.
        /// </summary>
        public static GlobalInit Instance { get; private set; }

        public void Awake()
        {
            Instance = this;
            LogManager.Instance.Init();
            Application.targetFrameRate = GlobalData.MaxFrame;
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
            List<AWorker> workers = WorkerManager.Instance.Characters;
            foreach (AWorker worker in workers)
            {
                // 按照时间对饥饿值与疲劳值进行自然衰减
                AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
                if (workerData.CurHungry > 0)
                {
                    workerData.CurHungry = Mathf.Max(
                        0.0f,
                        workerData.CurHungry - (Time.deltaTime * WorkerConditionConstant.HungryDecayPerSecond));
                }

                if (workerData.CurTired > 0)
                {
                    workerData.CurTired = Mathf.Max(
                        0.0f,
                        workerData.CurTired - (Time.deltaTime * WorkerConditionConstant.TiredDecayPerSecond));
                }

                WorkerConditionManager.Instance.UpdateWorkerCondition(worker);
            }

            // 只读刷新工人补给缺口提示，内部会按固定间隔节流，避免每帧输出 Tip。
            WorkerSupplyIssueManager.Instance.Tick();

            // 只读刷新任务队列拥堵提示，复用现有任务快照和 Tip UI，不改变任务调度。
            WorkerTaskCongestionAdvisor.Instance.Tick();

            // 只读刷新殖民地指挥中心报告，聚合人力、补给、任务拥堵和阻塞诊断，不改变任务调度。
            ColonyCommandCenterManager.Instance.Tick();
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
