namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 工具菜单
    /// 最下方按钮
    /// </summary>
    public class ToolMenu : MonoBehaviour
    {
        private static readonly KeyCode[] KeyCodes =
        {
            KeyCode.Alpha1, KeyCode.Alpha2,
            KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7,
            KeyCode.Alpha8, KeyCode.Alpha9,
        };

        private Transform btns;

        public void Awake()
        {
            this.btns = Tool.GetComponentInChildren<Transform>(this.gameObject, "Panel");
        }

        public void Update()
        {
            if (!Input.anyKeyDown)
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
