namespace LAB2D.Core
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using LAB2D.Gameplay;
    using LAB2D.Gameplay.TurnBattle;
    using LAB2D.Map;
    using LAB2D.UI;
    using LAB2D.UI.Action;
    using LAB2D.UI.Panel;
    using LAB2D.UnityAdapter;
    using System;
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
            this.ProcessJoinBattle(deltaTime);
            this.ProcessCaveExplore();
        }

        /// <summary>
        /// B 键 — 靠近大世界交战区时加入回合制战斗。
        /// 交战检测器由此处驱动（0.5s 节流轮询 + 滞回），按下时实时重检防节流窗口错过。
        /// 有面板打开、战斗中或联机时不触发。
        /// 按 B 后每个静默分支都有 [BattleDiag] 留痕（按键低频事件点）：
        /// 若按 B 时一条日志都没有 = 按键被 UI 输入态（IsUIInputActive）吞掉。
        /// </summary>
        private void ProcessJoinBattle(float deltaTime)
        {
            TurnBattleManager battleManager = TurnBattleManager.Instance;
            battleManager.Detector.Tick(deltaTime);

            this.UpdateBattlePrompt(battleManager);

            if (!UnityGlobalInputAdapter.GetHudToggleDown(Constant.InputKeyConstant.JoinBattle))
            {
                return;
            }

            if (battleManager.IsActive)
            {
                AWorkerTask.LogProvider("[BattleDiag] 按B忽略：已在战斗中", LogManager.LogLevelEnum.Debug);
                return;
            }

            // 有模态面板打开时不加入（战斗面板自身经 BattleStarted 事件打开，不依赖此处）。
            // 判据用 IsForeground 而非 Panels.Count：ForegroundPanel 是常驻前景（游戏主界面），
            // 正常游玩时栈=[Foreground] 非空，Count>0 会永久拦截 B 键（2026-09-05 实测按 B 无反应根因）
            PanelController controller = ServiceLocator.Get<PanelController>();
            if (controller != null && !controller.IsForeground())
            {
                AWorkerTask.LogProvider(
                    $"[BattleDiag] 按B忽略：有面板打开（{controller.Panels.Count} 层，非前景态）",
                    LogManager.LogLevelEnum.Debug);
                return;
            }

            Player player = ServiceLocator.Get<PlayerManager>()?.Mine;
            if (player == null)
            {
                AWorkerTask.LogProvider("[BattleDiag] 按B忽略：玩家不存在", LogManager.LogLevelEnum.Debug);
                return;
            }

            // 实时重检一次：0.5s 节流缓存在交战刚起的瞬间可能还是空的
            battleManager.Detector.DetectNow();
            if (!battleManager.Detector.TryGetNearbyEncounter(
                    (Vector2)player.transform.position, out BattleEncounter encounter))
            {
                string nearest = this.DescribeNearestEncounter(battleManager.Detector, player.transform.position);
                AWorkerTask.LogProvider(
                    $"[BattleDiag] 按B无交战：encounters={battleManager.Detector.Encounters.Count} 玩家pos={player.transform.position}{nearest}",
                    LogManager.LogLevelEnum.Debug);
                return;
            }

            AWorkerTask.LogProvider(
                $"[BattleDiag] 按B命中交战：Worker×{encounter.Workers.Count} Enemy×{encounter.Enemies.Count}，尝试开战",
                LogManager.LogLevelEnum.Debug);

            if (!battleManager.TryStartBattle(encounter, player, out string failReason))
            {
                ShowTip($"无法加入战斗：{failReason}");
            }

            // 成功路径无需提示：战斗面板经 Manager.BattleStarted 事件自动打开
        }

        /// <summary>
        /// [BattleDiag] 最近一次聚合的交战组描述（按 B 无交战时区分
        /// "聚合组为空"与"有交战但中心距玩家超 JoinBattleRadius"）。
        /// </summary>
        private string DescribeNearestEncounter(BattleEncounterDetector detector, Vector3 playerPos)
        {
            BattleEncounter nearest = null;
            float bestSqr = float.MaxValue;
            foreach (BattleEncounter encounter in detector.Encounters)
            {
                float sqr = (encounter.Center - (Vector2)playerPos).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    nearest = encounter;
                }
            }

            if (nearest == null)
            {
                return "（聚合组为空：无 Worker↔Enemy 交战边）";
            }

            return $"（最近交战中心距 {Mathf.Sqrt(bestSqr):F1} > 半径 {BattleEncounterDetector.JoinBattleRadius:F0}，"
                + $"Worker×{nearest.Workers.Count} Enemy×{nearest.Enemies.Count}）";
        }

        /// <summary>交战提示条显隐 — 玩家检测半径内有交战且非战斗中时显示。</summary>
        private void UpdateBattlePrompt(TurnBattleManager battleManager)
        {
            BattlePromptHUD prompt = BattlePromptHUD.Instance;
            if (prompt == null)
            {
                return;
            }

            if (battleManager.IsActive)
            {
                prompt.SetVisible(false);
                return;
            }

            Player player = ServiceLocator.Get<PlayerManager>()?.Mine;
            if (player == null)
            {
                prompt.SetVisible(false);
                return;
            }

            bool hasNearby = battleManager.Detector.HasNearbyBattle
                && battleManager.Detector.TryGetNearbyEncounter((Vector2)player.transform.position, out _);
            prompt.SetVisible(hasNearby);
        }

        private static void ShowTip(string text)
        {
            try
            {
                GameServices.ShowTipProvider(text);
            }
            catch (Exception)
            {
                // Tip 不可用时静默降级（测试环境）
            }
        }

        /// <summary>
        /// N/O 键 — 上古洞府探索交互：提示条显隐 + 读条进度 + 按键转发 AncientCaveManager。
        /// 读条推进/打断在 Manager.Tick（ITickable 已接线），此处只管输入与 HUD。
        /// </summary>
        private void ProcessCaveExplore()
        {
            CaveExploreHUD hud = CaveExploreHUD.Instance;
            if (!ServiceLocator.TryGet(out AncientCaveManager caveManager) || caveManager == null)
            {
                hud?.SetVisible(false);
                return;
            }

            Player player = ServiceLocator.Get<PlayerManager>()?.Mine;
            if (player == null)
            {
                hud?.SetVisible(false);
                return;
            }

            // 读条中：只显示进度，不再响应按键（打断走移动/受击通道）
            if (caveManager.IsPlayerExploring)
            {
                hud?.SetExploring(caveManager.PlayerExploreProgress);
                return;
            }

            // 提示条显隐：附近交互半径内有可探索洞府
            bool nearCave = false;
            if (ServiceLocator.TryGet(out TileMap tileMap) && tileMap != null)
            {
                Vector3Int posMap = tileMap.WorldPosToMapPos(player.transform.position);
                nearCave = caveManager.FindRevealedCaveNear(posMap, AncientCaveManager.ExploreInteractRadius) >= 0;
                if (nearCave && hud != null)
                {
                    hud.SetIdleText();
                }
            }

            hud?.SetVisible(nearCave);
            if (!nearCave)
            {
                return;
            }

            if (UnityGlobalInputAdapter.GetHudToggleDown(Constant.InputKeyConstant.ExploreCave))
            {
                // 有面板打开时不触发（ProcessJoinBattle 同款守卫）：判据用 IsForeground 而非
                // Panels.Count——ForegroundPanel 常驻前景，正常游玩时栈非空，Count==0 永远拦截
                PanelController controller = ServiceLocator.Get<PanelController>();
                if (controller == null || controller.IsForeground())
                {
                    caveManager.TryStartPlayerExplore();
                }
            }
            else if (UnityGlobalInputAdapter.GetHudToggleDown(Constant.InputKeyConstant.DispatchCaveExplore))
            {
                caveManager.TryDispatchWorkerExplore();
            }
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
            else if (!controller.Panels.Contains(WorkerMindPanel.Instance))
            {
                // 已在栈中被其他面板（如对话）盖住时按 F12 不动作——
                // PanelController.Show 无幂等保护，重复 Push 会栈堆积、需多次 ESC 才能退净
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
