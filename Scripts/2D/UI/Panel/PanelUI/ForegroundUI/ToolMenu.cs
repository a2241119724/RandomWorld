namespace LAB2D.UI.Panel.PanelUI.ForegroundUI
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.UnityAdapter;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 工具菜单
    /// 最下方按钮
    /// </summary>
    public class ToolMenu : MonoBehaviour
    {
        private static readonly KeyCode[] KeyCodes = InputKeyConstant.ToolMenuKeys;

        /// <summary>
        /// 匹配数字按键
        /// </summary>
        public IBasePanel[] ToolMenus { get; private set; }

        public void Awake()
        {
            this.ToolMenus = new IBasePanel[]
            {
                BuildMenuPanel.Instance, BackpackMenuPanel.Instance,
                WorkerTaskTogglePanel.Instance, InventoryMenuPanel.Instance, AIChatPanel.Instance,
            };
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.gameObject, "GeneratorWorker").onClick.AddListener(this.Onclick_GeneratorWorker);
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.gameObject, "GeneratorItem").onClick.AddListener(this.Onclick_GeneratorItem);
        }

        public void Update()
        {
            if (!UnityGlobalInputAdapter.TryGetToolMenuIndexDown(KeyCodes, this.ToolMenus.Length, out int menuIndex))
            {
                return;
            }

            if (PanelController.Instance.Panels.Peek() == ForegroundPanel.Instance)
            {
                PanelController.Instance.Show(this.ToolMenus[menuIndex]);
            }
            else if (PanelController.Instance.Panels.Peek() == this.ToolMenus[menuIndex])
            {
                PanelController.Instance.Close();
            }
        }

        /// <summary>
        /// 测试生成玩家
        /// </summary>
        private void Onclick_GeneratorWorker()
        {
            if (PlayerManager.Instance.Mine == null)
            {
                AWorkerTask.LogProvider("玩家尚未加载完成，无法生成Worker", LogManager.LogLevelEnum.Warning);
                return;
            }

            WorkerManager.Instance.Create(PlayerManager.Instance.Mine.transform.position);
        }


        private void Onclick_GeneratorItem()
        {
            if (PlayerManager.Instance.Mine == null)
            {
                AWorkerTask.LogProvider("玩家尚未加载完成，无法生成物品", LogManager.LogLevelEnum.Warning);
                return;
            }

            EnemyLootManager.Instance.TryDropLoot(
                PlayerManager.Instance.Mine.transform.position, waveNumber: 0);
        }
    }
}
