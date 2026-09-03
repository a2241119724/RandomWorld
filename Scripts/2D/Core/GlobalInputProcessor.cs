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
        private IGameLogger gameLogger;

        private IGameLogger GameLogger => this.gameLogger ?? (this.gameLogger = GameLoggerFactory.Get());

        public GlobalInputProcessor()
        {
            // 启动时从 PlayerPrefs 恢复上次的开关状态
            EnemyLootManager.ForceDrop = PlayerPrefs.GetInt(ForceDropPrefsKey, 0) == 1;
            if (EnemyLootManager.ForceDrop)
            {
                this.GameLogger.Log("[ForceDrop] 100%掉落已开启（从上次会话恢复）");
            }
        }

        public void Tick(float deltaTime)
        {
            this.ProcessCloseOrBuildMenu();
            this.ProcessMouseClickCloseItemInfo();
            this.ProcessAchievements();
            this.ProcessRoomListToggle();
            this.ProcessWorkerMindToggle();
        }

        /// <summary>
        /// 数字6 — 房间列表面板切换
        /// </summary>
        private void ProcessRoomListToggle()
        {
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                var controller = ServiceLocator.Get<PanelController>();
                if (controller.Panels.Count > 0 && controller.Panels.Peek() == RoomListPanel.Instance)
                {
                    controller.Close();
                }
                else
                {
                    controller.Show(RoomListPanel.Instance);
                }
            }
        }

        /// <summary>
        /// F12 — Worker 心智面板切换（走 PanelController 栈，ESC 关闭由栈顶 OnClick_Back 覆盖）。
        /// </summary>
        private void ProcessWorkerMindToggle()
        {
            if (!UnityGlobalInputAdapter.GetHudToggleDown(Constant.InputKeyConstant.ToggleWorkerMindHud))
            {
                return;
            }

            var controller = ServiceLocator.Get<PanelController>();
            if (controller.Panels.Count > 0 && controller.Panels.Peek() == WorkerMindPanel.Instance)
            {
                controller.Close();
            }
            else
            {
                controller.Show(WorkerMindPanel.Instance);
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

            // 优先关闭可见的 HUD 面板（F6/F7 等热键切换型 HUD），而非触发面板栈的 OnClick_Back
            if (this.TryCloseVisibleHud())
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

        /// <summary>
        /// 已知的热键切换型 HUD / 面板元信息。
        /// visibility 取值: "CanvasGroup" — 通过 CanvasGroup.alpha 判断; "ActiveSelf" — 通过 gameObject.activeSelf 判断.
        /// closeTarget: 实际关闭操作的目标 GameObject 名（对于 SetActive 型面板，有时 visibility 检查节点和关闭目标不同）。
        /// </summary>
        private struct HudCloseEntry
        {
            public string Name;          // 用于检测可见性的 GameObject 名
            public string VisibilityType; // "CanvasGroup" or "ActiveSelf"
            public string CloseTarget;   // 实际要 SetActive(false) 的目标名；若为空则用 Name 自身
        }

        private static readonly HudCloseEntry[] HudCloseList =
        {
            // 数字9 — 成就面板
            new () { Name = "AchievementPanel", VisibilityType = "ActiveSelf" },
            // 数字0 — 装备面板 (DontDestroyOnLoad)
            new () { Name = "EquipmentPanelRoot", VisibilityType = "ActiveSelf", CloseTarget = "EquipmentPanelManager" },
            // ESC — 存档面板 (ForegroundPanel 管理的子面板)
            new () { Name = "SaveSlotPanel", VisibilityType = "ActiveSelf" },
            // K — 修仙面板
            new () { Name = "CultivationPanel", VisibilityType = "ActiveSelf" },
            // T — 科技面板
            new () { Name = "TechPanel", VisibilityType = "ActiveSelf" },
        };

        /// <summary>
        /// 尝试关闭当前可见的热键切换型 HUD/面板。
        /// 优先关闭已展开的面板，阻止 Esc 继续触发 PausePanel。
        /// </summary>
        /// <returns>是否关闭了至少一个可见的 HUD/面板。</returns>
        private bool TryCloseVisibleHud()
        {
            bool closedAny = false;
            foreach (HudCloseEntry entry in HudCloseList)
            {
                GameObject hudGo = GameObject.Find(entry.Name);
                if (hudGo == null)
                {
                    continue;
                }

                bool isVisible;
                if (entry.VisibilityType == "ActiveSelf")
                {
                    isVisible = hudGo.activeSelf;
                }
                else
                {
                    CanvasGroup cg = hudGo.GetComponent<CanvasGroup>();
                    isVisible = cg != null && cg.alpha >= 0.5f;
                }

                if (!isVisible)
                {
                    continue;
                }

                // 关闭
                GameObject closeTarget = hudGo;
                if (!string.IsNullOrEmpty(entry.CloseTarget))
                {
                    closeTarget = GameObject.Find(entry.CloseTarget) ?? hudGo;
                }

                if (entry.VisibilityType == "ActiveSelf")
                {
                    // 优先尝试通过 SendMessage 调用 Hide() 方法以保持组件内部状态一致
                    closeTarget.SendMessage("Hide", SendMessageOptions.DontRequireReceiver);
                    // 兜底: 如果可见性检测目标仍然 active（说明 Hide() 未生效或不存在），直接 SetActive(false)
                    if (hudGo.activeSelf)
                    {
                        hudGo.SetActive(false);
                    }
                }
                else
                {
                    CanvasGroup cg = hudGo.GetComponent<CanvasGroup>();
                    cg.alpha = 0.0f;
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                }

                closedAny = true;
            }

            return closedAny;
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
