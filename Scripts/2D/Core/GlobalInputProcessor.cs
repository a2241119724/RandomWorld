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
        private const string ForceDropPrefsKey = "Debug_ForceDrop";

        public GlobalInputProcessor()
        {
            // 启动时从 PlayerPrefs 恢复上次的开关状态
            EnemyLootManager.ForceDrop = PlayerPrefs.GetInt(ForceDropPrefsKey, 0) == 1;
            if (EnemyLootManager.ForceDrop)
            {
                Debug.Log("[ForceDrop] 100%掉落已开启（从上次会话恢复）");
            }
        }

        public void Tick(float deltaTime)
        {
            this.ProcessCloseOrBuildMenu();
            this.ProcessMouseClickCloseItemInfo();
            this.ProcessAchievements();
            this.ProcessDebugToggles();
        }

        private void ProcessDebugToggles()
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                EnemyLootManager.ForceDrop = !EnemyLootManager.ForceDrop;
                PlayerPrefs.SetInt(ForceDropPrefsKey, EnemyLootManager.ForceDrop ? 1 : 0);
                PlayerPrefs.Save();
                Debug.Log($"[ForceDrop] 100%掉落已{(EnemyLootManager.ForceDrop ? "开启" : "关闭")}");
            }
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
                ServiceLocator.Get<PanelController>().Show(BuildPanel.Instance);
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

            // 鼠标点击在 UI 面板上时（非 Foreground 主游戏区域），不关闭 ItemInfo
            var uiResults = LAB2D.Tool.Tool.GetUIByMousePos();
            if (uiResults.Count > 0 && uiResults[0].gameObject.name != "Foreground")
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
