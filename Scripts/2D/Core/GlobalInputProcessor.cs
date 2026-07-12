namespace LAB2D.Core
{
    using LAB2D;
    using LAB2D.UI.Panel;
    using UnityEngine;

    /// <summary>
    /// 全局输入处理器 — 从 GlobalInit 提取的职责。
    /// 负责处理 ESC 键面板切换、鼠标点击关闭物品信息面板等全局输入逻辑。
    /// 纯静态工具类，不持有状态。
    /// </summary>
    public static class GlobalInputProcessor
    {
        /// <summary>
        /// 处理每帧的全���输入。
        /// 由 GlobalInit.Update() 调用。
        /// </summary>
        public static void ProcessInput()
        {
            ProcessCloseOrBuildMenu();
            ProcessMouseClickCloseItemInfo();
        }

        /// <summary>
        /// ESC 键：退出当前面板或打开建造菜单。
        /// </summary>
        private static void ProcessCloseOrBuildMenu()
        {
            if (LAB2D.Tool.Tool.IsUIInputActive() || !Input.GetKeyDown(InputKeyConstant.CloseOrBuildMenu))
            {
                return;
            }

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

        /// <summary>
        /// 鼠标点击（左键/中键）：关闭物品信息面板。
        /// </summary>
        private static void ProcessMouseClickCloseItemInfo()
        {
            if (LAB2D.Tool.Tool.IsUIInputActive())
            {
                return;
            }

            if (!Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(2))
            {
                return;
            }

            // 关闭ItemInfo面板
            if (PanelController.Instance.Panels.Count > 0
                && PanelController.Instance.Panels.Peek() == ItemInfoPanel.Instance)
            {
                ItemInfoUI.Instance.Init();
                PanelController.Instance.Close();
            }
        }
    }
}
