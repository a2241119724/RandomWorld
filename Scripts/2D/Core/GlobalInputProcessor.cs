namespace LAB2D.Core
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using LAB2D.UI.Panel;
    using UnityEngine;

    /// <summary>
    /// 全局输入处理器 — 从 GlobalInit 提取的职责。
    /// 负责处理 ESC 键面板切换、鼠标点击关闭物品信息面板等全局输入逻辑。
    /// 实现 ITickable，由 GlobalInit 自动发现和驱动。
    /// </summary>
    public sealed class GlobalInputProcessor : ITickable
    {
        public void Tick(float deltaTime)
        {
            this.ProcessCloseOrBuildMenu();
            this.ProcessMouseClickCloseItemInfo();
            this.ProcessAchievements();
            this.ProcessColonyCommandHud();
        }

        private void ProcessAchievements()
        {
            AchievementManager mgr = AchievementManager.Instance;
            if (mgr == null || !mgr.IsInitialized)
            {
                return;
            }

            mgr.UpdateProgressAll();

            if (mgr.HasPendingUnlock)
            {
                AchievementData pending = mgr.PeekPendingUnlock();
                if (pending != null && AchievementPopup.RuntimeInstance != null)
                {
                    AchievementPopup.RuntimeInstance.Show(pending);
                }
            }

            if (!LAB2D.Tool.Tool.IsUIInputActive() &&
                Input.GetKeyDown(InputKeyConstant.ToggleWorkerTaskAndAchievementHud))
            {
                AchievementPanel.RuntimeInstance?.TogglePanel();
            }
        }

        private void ProcessColonyCommandHud()
        {
            if (Input.GetKeyDown(InputKeyConstant.ToggleColonyCommandCenterHud))
            {
                GameObject hudObj = GameObject.Find(ColonyCommandCenterConstant.HudRootName);
                if (hudObj == null)
                {
                    ColonyCommandCenterHUD.EnsureRuntimePanel();
                    return;
                }

                hudObj.SetActive(!hudObj.activeSelf);
            }
        }

        private void ProcessCloseOrBuildMenu()
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
                if (PanelController.Instance.Panels.Peek() == ItemInfoPanel.Instance)
                {
                    ItemInfoUI.Instance.Init();
                }

                PanelController.Instance.Panels.Peek().OnClick_Back();
            }
        }

        private void ProcessMouseClickCloseItemInfo()
        {
            if (LAB2D.Tool.Tool.IsUIInputActive())
            {
                return;
            }

            if (!Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(2))
            {
                return;
            }

            if (PanelController.Instance.Panels.Count > 0
                && PanelController.Instance.Panels.Peek() == ItemInfoPanel.Instance)
            {
                ItemInfoUI.Instance.Init();
                PanelController.Instance.Close();
            }
        }
    }
}
