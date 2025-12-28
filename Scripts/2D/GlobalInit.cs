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
            }
        }

        public void Update()
        {
            this.WorkerUpdate();

            // 退出界面(除了ForegroundPanel,CreateOrJoinPanel,CreateMenuPanel,CreateDataPanel,AsyncProgressPanel)
            if (Input.GetKeyDown(KeyCode.Escape))
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

            EnvironmentManager.Instance.UpdateEnergy();
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2))
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
                    workerData.CurHungry -= Time.deltaTime * 0.1f;
                }

                if (workerData.CurTired > 0)
                {
                    workerData.CurTired -= Time.deltaTime * 0.01f;
                }
            }
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