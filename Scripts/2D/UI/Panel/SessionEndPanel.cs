namespace LAB2D.UI.Panel
{
    using LAB2D;
    using LAB2D.Constant;
    using LAB2D.Core;
    using LAB2D.Domain.Gameplay;
    using LAB2D.Gameplay;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 终局结算面板 — 山门陷落（失败）/ 小镇屹立（胜利）的会话结算展示。
    /// 失败=山门被破上限次数（TriggerGameOver 置 timeScale=0）；胜利=核心升至满级。
    /// IsOverlay=true 不暂停栈顶；ESC/「继续查看」关闭后可经山门 HUD「查看结算」重开（OpenLast）。
    /// UI 全部运行时代码构建（Game.unity 无法手改 YAML，不走场景摆放/prefab）。
    /// 「重新开始/返回大厅」显式恢复 timeScale 并复位会话统计
    /// （GameplaySessionStats.ResetSession 只在构造时被调，读档/重开不复位是既有坑）。
    /// </summary>
    public class SessionEndPanel : ABasePanel<SessionEndPanel>
    {
        private Text titleText;
        private Text statsText;
        private SessionResultData lastResult;
        private SessionEndingType lastEnding = SessionEndingType.None;

        /// <summary>像素字体缓存（ark-pixel 加载失败回退 Unity 内置）。</summary>
        private static Font cachedFont;

        public SessionEndPanel()
        {
            this.Name = "SessionEndPanel";

            // 安全加载：先找场景对象 → 无则创建空节点（无 prefab，结构由本类代码构建）
            Transform parent = this.Controller?.Parent;
            if (parent == null)
            {
                GameObject uiRoot = GameObject.FindGameObjectWithTag(TagConstant.UI_TAG);
                parent = uiRoot?.transform;
            }

            if (parent != null)
            {
                Transform existing = parent.Find(this.Name);
                if (existing != null)
                {
                    this.Panel = existing.gameObject;
                }
                else
                {
                    this.Panel = new GameObject(this.Name, typeof(RectTransform));
                    this.Panel.transform.SetParent(parent, false);
                }

                this.Panel.name = this.Name;
                this.Panel.SetActive(false);
            }

            this.BuildUI();
        }

        /// <inheritdoc/>
        public override bool IsOverlay => true;

        /// <summary>
        /// 打开终局结算面板（终局触发方调用：MountainGateManager.TriggerGameOver/TriggerVictory）。
        /// </summary>
        /// <param name="result">终局采集的结算数据（可为 null，面板显示占位文案）。</param>
        /// <param name="ending">结局类型。</param>
        public void Open(SessionResultData result, SessionEndingType ending)
        {
            if (this.Panel == null)
            {
                return;
            }

            this.lastResult = result;
            this.lastEnding = ending;

            // 防重复入栈：已在栈顶时仅刷新内容（终局触发方可能重复调用）
            if (this.Controller.Panels.Count > 0 && this.Controller.Panels.Peek() == this)
            {
                this.ApplyContent();
                this.Panel.SetActive(true);
                return;
            }

            this.Controller.Show(this);
        }

        /// <summary>
        /// 重开上一次结算（山门 HUD「查看结算」入口）：
        /// 无缓存时从 MountainGateManager 终局态推断结局，数据取最近一次采集。
        /// 非终局状态下不打开。
        /// </summary>
        public void OpenLast()
        {
            if (this.Panel == null)
            {
                return;
            }

            if (this.lastResult == null)
            {
                MountainGateManager gate = MountainGateManager.Instance;
                SessionEndingType inferred = SessionEndingType.None;
                if (gate != null && gate.IsGameOver)
                {
                    inferred = SessionEndingType.Defeat;
                }
                else if (gate != null && gate.IsVictory)
                {
                    inferred = SessionEndingType.Victory;
                }

                if (inferred == SessionEndingType.None)
                {
                    return;
                }

                this.lastEnding = inferred;
                this.lastResult = SessionResultManager.Instance.LatestResult;
            }

            this.Open(this.lastResult, this.lastEnding);
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            this.ApplyContent();
        }

        /// <inheritdoc/>
        public override void OnClick_Back()
        {
            // ESC 关闭（等价「继续查看」）：显式恢复 timeScale，关后可经山门 HUD 重开，不算死路
            Time.timeScale = 1f;
            this.Controller.Close();
        }

        /// <summary>
        /// 构建全屏遮罩 + 标题 + 统计 + 三按钮子树（照 WorkerMindPanel 自建模式）。
        /// </summary>
        private void BuildUI()
        {
            if (this.Panel == null)
            {
                return;
            }

            RectTransform root = this.Panel.GetComponent<RectTransform>();
            if (root == null)
            {
                return;
            }

            // 全屏遮罩：四向 stretch 拉满
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            Image shade = this.Panel.GetComponent<Image>();
            if (shade == null)
            {
                shade = this.Panel.AddComponent<Image>();
            }

            shade.color = PixelUITheme.DialogShadeDark;
            shade.raycastTarget = true;

            // 盖住 F3 ResultCard 等同层结算 UI
            this.Panel.transform.SetAsLastSibling();

            this.titleText = CreateText(root, "Title", string.Empty, 36, Color.white);
            RectTransform titleRect = this.titleText.GetComponent<RectTransform>();
            SetCenterRect(titleRect, new Vector2(0f, 165f), new Vector2(520f, 64f));
            this.titleText.alignment = TextAnchor.MiddleCenter;

            this.statsText = CreateText(root, "Stats", string.Empty, 12, PixelUITheme.TextOnDark);
            RectTransform statsRect = this.statsText.GetComponent<RectTransform>();
            SetCenterRect(statsRect, new Vector2(0f, -10f), new Vector2(500f, 270f));
            this.statsText.alignment = TextAnchor.MiddleCenter;

            Button restart = this.CreateButton(root, "Restart", "重新开始", new Vector2(-190f, -165f));
            restart.onClick.AddListener(this.OnClick_Restart);

            Button backLobby = this.CreateButton(root, "BackLobby", "返回大厅", new Vector2(0f, -165f));
            backLobby.onClick.AddListener(this.OnClick_BackLobby);

            Button continueView = this.CreateButton(root, "ContinueView", "继续查看", new Vector2(190f, -165f));
            continueView.onClick.AddListener(this.OnClick_Continue);
        }

        /// <summary>
        /// 应用缓存的结算数据到标题与统计区。
        /// </summary>
        private void ApplyContent()
        {
            if (this.titleText == null || this.statsText == null)
            {
                return;
            }

            bool isVictory = this.lastEnding == SessionEndingType.Victory;
            this.titleText.text = isVictory ? "小镇屹立" : "山门陷落";
            this.titleText.color = isVictory ? PixelUITheme.VictoryTitle : PixelUITheme.DeathTitle;
            this.statsText.text = this.BuildStatsText(isVictory);
        }

        /// <summary>
        /// 生成统计 RichText：结局描述 + 经营天数 + 评分/星级/评级 + 战斗/生存/时长。
        /// </summary>
        private string BuildStatsText(bool isVictory)
        {
            string endingLine = isVictory
                ? $"山门核心修至 {BuildingDamageRuleService.CoreMaxLevel} 级，小镇在妖潮中屹立不倒"
                : $"山门核心被破 {BuildingDamageRuleService.CoreMaxDownfalls} 次，护山大阵崩塌";

            // 经营天数（第 N 天从 1 计）；GameTimeManager 不可用时省略该行
            string dayLine = string.Empty;
            try
            {
                int day = GameTimeManager.Instance.CurrentDayIndex + 1;
                dayLine = $"\n共经营 {day} 天";
            }
            catch
            {
                // 省略天数
            }

            SessionResultData r = this.lastResult;
            if (r == null)
            {
                return $"{endingLine}{dayLine}\n\n（未采集到结算数据）";
            }

            string stars = new string('★', r.StarRating) + new string('☆', 5 - r.StarRating);
            return $"{endingLine}{dayLine}\n\n" +
                   $"<color={PixelUITheme.RichGold}>评分</color> {r.CombatScore}    {stars} ({r.StarRating}/5)    评级 {r.GradeText}\n" +
                   $"击杀 {r.TotalDefeatedEnemyCount}    最高连击 {r.MaxCombo}\n" +
                   $"造成伤害 {r.TotalDamageDealt}    承受伤害 {r.TotalDamageTaken}\n" +
                   $"玩家死亡 {r.PlayerDeathCount} 次    工人死亡 {r.TotalWorkerDeathCount} 次\n" +
                   $"会话时长 {FormatDuration(r.SessionDuration)}";
        }

        /// <summary>
        /// 「继续查看」：恢复 timeScale 并关闭面板（重开入口 = 山门 HUD「查看结算」）。
        /// </summary>
        private void OnClick_Continue()
        {
            // 显式恢复（失败终局时为 0），不依赖面板栈隐式行为
            Time.timeScale = 1f;
            this.Controller.Close();
        }

        private void OnClick_Restart()
        {
            this.ExitToMenuPanel(NewOrContinuePanel.Instance);
        }

        private void OnClick_BackLobby()
        {
            this.ExitToMenuPanel(CreateOrJoinPanel.Instance);
        }

        /// <summary>
        /// 回主菜单（面板栈路线，禁止 LoadScene("Menu")——Menu.unity 不在 Build Settings）：
        /// 恢复 timeScale → 复位会话统计 → 逐层关闭整栈 → Show 目标面板（照 PausePanel.OnClick_BackMenu）。
        /// </summary>
        /// <param name="target">目标面板（NewOrContinuePanel / CreateOrJoinPanel）。</param>
        private void ExitToMenuPanel(IBasePanel target)
        {
            Time.timeScale = 1f;
            ServiceLocator.Get<GameplaySessionStats>()?.ResetSession();

            PanelController controller = ServiceLocator.Get<PanelController>();
            while (controller.Panels.Count > 0)
            {
                controller.Close();
            }

            controller.Show(target);
        }

        /// <summary>
        /// 创建按钮（照 WaveBossRewardPanel.CreateOptionButton：Image 四态 + 子 Text）。
        /// </summary>
        private Button CreateButton(Transform parent, string name, string label, Vector2 position)
        {
            GameObject buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            SetCenterRect(buttonRect, position, new Vector2(170f, 46f));

            Image image = buttonObject.GetComponent<Image>();
            image.color = PixelUITheme.ButtonNormal;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = PixelUITheme.ButtonNormal;
            colors.highlightedColor = PixelUITheme.ButtonHighlighted;
            colors.pressedColor = PixelUITheme.ButtonPressed;
            colors.selectedColor = PixelUITheme.ButtonSelected;
            button.colors = colors;

            Text text = CreateText(buttonObject.transform, "Text", label, 24, PixelUITheme.TextPrimary);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        /// <summary>
        /// 创建文本（照 WorkerMindUI.CreateText，字体优先 ark-pixel 像素字体）。
        /// </summary>
        private static Text CreateText(Transform parent, string name, string text, int fontSize, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.font = LoadPixelFont();
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>
        /// 加载像素字体：ark-pixel 优先，失败回退 Unity 内置 LegacyRuntime。
        /// </summary>
        private static Font LoadPixelFont()
        {
            if (cachedFont == null)
            {
                cachedFont = Resources.Load<Font>("Font/ark-pixel-12px-monospaced-zh_cn");
                if (cachedFont == null)
                {
                    cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
            }

            return cachedFont;
        }

        /// <summary>
        /// 居中锚点矩形的 anchor/pivot/位置/尺寸一次设置。
        /// </summary>
        private static void SetCenterRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>
        /// 会话时长格式化：秒 → 秒/分秒/时分秒。
        /// </summary>
        private static string FormatDuration(float seconds)
        {
            if (seconds < 60f)
            {
                return $"{seconds:0} 秒";
            }

            int totalMinutes = Mathf.FloorToInt(seconds / 60f);
            int remainSeconds = Mathf.FloorToInt(seconds % 60f);
            return totalMinutes >= 60
                ? $"{totalMinutes / 60} 时 {totalMinutes % 60} 分 {remainSeconds} 秒"
                : $"{totalMinutes} 分 {remainSeconds} 秒";
        }
    }
}
