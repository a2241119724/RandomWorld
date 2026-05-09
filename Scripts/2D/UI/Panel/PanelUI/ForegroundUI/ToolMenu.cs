namespace LAB2D
{
    using UnityEngine;

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
        }

        public void Update()
        {
            if (Tool.IsUIInputActive() || !Input.anyKeyDown)
            {
                return;
            }

            for (int i = 0; i < this.ToolMenus.Length; i++)
            {
                if (Input.GetKeyDown(KeyCodes[i]))
                {
                    if (PanelController.Instance.Panels.Peek() == ForegroundPanel.Instance)
                    {
                        PanelController.Instance.Show(this.ToolMenus[i]);
                    }
                    else if (PanelController.Instance.Panels.Peek() == this.ToolMenus[i])
                    {
                        PanelController.Instance.Close();
                    }

                    break;
                }
            }
        }
    }
}
