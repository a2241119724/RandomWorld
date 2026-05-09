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

        private Transform btns;

        public void Awake()
        {
            this.btns = Tool.GetComponentInChildren<Transform>(this.gameObject, "Menu");
        }

        public void Update()
        {
            if (Tool.IsUIInputActive() || !Input.anyKeyDown)
            {
                return;
            }

            for (int i = 0; i < this.btns.childCount; i++)
            {
                if (Input.GetKeyDown(KeyCodes[i]))
                {
                    if (PanelController.Instance.Panels.Peek() == ForegroundPanel.Instance)
                    {
                        PanelController.Instance.Show(ForegroundPanel.Instance.ToolMenus[i]);
                    }
                    else if (PanelController.Instance.Panels.Peek() == ForegroundPanel.Instance.ToolMenus[i])
                    {
                        PanelController.Instance.Close();
                    }

                    break;
                }
            }
        }
    }
}
