namespace LAB2D.Core
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using LAB2D.UI.Action;
    using LAB2D.UI.Panel;
    using LAB2D.UnityAdapter;
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
        }

        private void ProcessAchievements()
        {
            if (UnityGlobalInputAdapter.GetToggleWorkerTaskAndAchievementHudDown())
            {
                AchievementPanel.RuntimeInstance?.TogglePanel();
            }
        }

        private void ProcessCloseOrBuildMenu()
        {
            if (!UnityGlobalInputAdapter.GetCloseOrBuildMenuDown())
            {
                return;
            }

            if (ServiceLocator.Get<PanelController>().Panels.Count == 0)
            {
                ServiceLocator.Get<BuildingUI>().gameObject.SetActive(false);
                ServiceLocator.Get<PanelController>().Show(BuildMenuPanel.Instance);
                ServiceLocator.Get<IsAvailableMap>().ClearShow();
            }
            else
            {
                if (ServiceLocator.Get<PanelController>().Panels.Peek() == ItemInfoPanel.Instance)
                {
                    ServiceLocator.Get<ItemInfoUI>().Init();
                }

                ServiceLocator.Get<PanelController>().Panels.Peek().OnClick_Back();
            }
        }

        private void ProcessMouseClickCloseItemInfo()
        {
            if (!UnityGlobalInputAdapter.GetItemInfoCloseClickDown())
            {
                return;
            }

            if (ServiceLocator.Get<PanelController>().Panels.Count > 0
                && ServiceLocator.Get<PanelController>().Panels.Peek() == ItemInfoPanel.Instance)
            {
                ServiceLocator.Get<ItemInfoUI>().Init();
                ServiceLocator.Get<PanelController>().Close();
            }
        }
    }
}
