namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 全局初始化.
    /// </summary>
    public class GlobalInit : MonoBehaviour
    {
        private const int FONT_SIZE = 20;
        private readonly List<string> fontExcludeText = new List<string>()
        {
            "Label",
        };

        private bool initPanel = true;
        private bool initFont = true;
        private GameObject tip; // 提示框预制体

        /// <summary>
        /// 单例.
        /// </summary>
        public static GlobalInit Instance { get; private set; }

        /// <summary>
        /// 展示通知.
        /// </summary>
        /// <param name="text">通知内容.</param>
        public void ShowTip(string text)
        {
            GameObject g = Instantiate(this.tip);
            if (g == null)
            {
                LogManager.Instance.Log("tip Instantiate Error!!!", LogManager.LogLevel.Error);
                return;
            }

            g.name = this.tip.name;
            g.GetComponent<TipUI>().setText(text);
            g.transform.SetParent(this.transform, false);

            // 由于实例化产生形状变化,重新设置
            // g.transform.localScale = Vector3.zero;
            // RectTransform rt = g.GetComponent<RectTransform>();
            // rt.offsetMin = Vector2.zero;
            // rt.offsetMax = Vector2.zero;
            // Vector3 v = rt.localPosition; // 相对坐标
            // rt.localPosition = new Vector3(v.x, v.y, 0);
        }

        private void Awake()
        {
            Instance = this;
            this.tip = ResourcesManager.Instance.GetPrefab("Tip");
            if (this.initFont)
            {
                // init font
                Text[] texts = FindObjectsOfType<Text>();
                foreach (Text text in texts)
                {
                    if (this.fontExcludeText.Contains(text.name))
                    {
                        continue;
                    }

                    text.fontSize = FONT_SIZE;
                }
            }
        }

        private void Start()
        {
            // init panel
            if (this.initPanel)
            {
                ForegroundPanel.Instance.Init();
                if (PanelController.Instance == null)
                {
                    LogManager.Instance.Log("manager Not Found!!!", LogManager.LogLevel.Error);
                    return;
                }

                PanelController.Instance.show(CreateOrJoinPanel.Instance);

                // 初始化背包
                BackpackMenuPanel.Instance.panel.SetActive(true);
                BackpackMenuPanel.Instance.panel.SetActive(false);
            }

            // 添加10个种植任务
            for (int i = 0; i < 10; i++)
            {
                WorkerTaskManager.Instance.AddTask(new WorkerPlantTask.PlantTaskBuilder().build());
            }
        }

        private void Update()
        {
            this.WorkerUpdate();

            // 退出界面(除了ForegroundPanel,CreateOrJoinPanel,CreateMenuPanel,CreateDataPanel,AsyncProgressPanel)
            if (Input.GetKey(KeyCode.Escape))
            {
                if (PanelController.Instance.Panels.Count == 0)
                {
                    BuildingUI.Instance.enabled = false;
                    PanelController.Instance.show(BuildMenuPanel.Instance);
                    IsAvailableMap.Instance.clearShow();
                }
                else if (PanelController.Instance.Panels.Peek() != ForegroundPanel.Instance &&
                    PanelController.Instance.Panels.Peek() != CreateOrJoinPanel.Instance &&
                    PanelController.Instance.Panels.Peek() != CreateMenuPanel.Instance &&
                    PanelController.Instance.Panels.Peek() != CreateDataPanel.Instance &&
                    PanelController.Instance.Panels.Peek() != AsyncProgressPanel.Instance)
                { // 不能关闭这些面板
                    if (PanelController.Instance.Panels.Peek() == ItemInfoPanel.Instance)
                    {
                        ItemInfoUI.Instance.Init();
                    }

                    PanelController.Instance.close();
                }
            }

            EnvironmentManager.Instance.updateEnergy();
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2))
            {
                // 关闭ItemInfo面板
                if (PanelController.Instance.Panels.Count > 0
                    && PanelController.Instance.Panels.Peek() == ItemInfoPanel.Instance)
                {
                    ItemInfoUI.Instance.Init();
                    PanelController.Instance.close();
                }
            }
        }

        private void WorkerUpdate()
        {
            List<Worker> workers = WorkerManager.Instance.Characters;
            foreach (Worker worker in workers)
            {
                // 按照时间对饥饿值与疲劳值进行自然衰减
                worker.CurHungry -= Time.deltaTime * 0.1f;
                worker.CurTired -= Time.deltaTime * 0.01f;
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

    /// <summary>
    /// 实现LoadData,SaveData.
    /// </summary>
    public abstract class ASaveData : ISaveData
    {
        public ASaveData()
        {
            Instances.Add(this);
        }

        /// <summary>
        /// 单例.
        /// </summary>
        public static List<ISaveData> Instances { get; set; } = new List<ISaveData>();

        /// <summary>
        /// 加载数据.
        /// </summary>
        public virtual void LoadData()
        {
        }

        /// <summary>
        /// 保存数据.
        /// </summary>
        public virtual void SaveData()
        {
        }
    }

    /// <summary>
    /// 带有MonoBehaviour的ISaveData.
    /// </summary>
    public abstract class AMonoSaveData : MonoBehaviour, ISaveData
    {
        public AMonoSaveData()
        {
            Instances.Add(this);
        }

        /// <summary>
        /// 单例.
        /// </summary>
        public static List<ISaveData> Instances { get; set; } = new List<ISaveData>();

        /// <summary>
        /// 加载数据.
        /// </summary>
        public virtual void LoadData()
        {
        }

        /// <summary>
        /// 保存数据.
        /// </summary>
        public virtual void SaveData()
        {
        }
    }
}