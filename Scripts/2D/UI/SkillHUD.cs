namespace LAB2D.UI
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 主动技能 HUD — 运行时动态创建技能按钮栏。
    /// 显示4个技能按钮（旋岚斩/冲刺/力量爆发/治疗之光），
    /// 每个按钮包含冷却覆盖层、法力消耗文本、技能名称、等级和快捷键提示。
    ///
    /// 接入方式：
    ///   1. GlobalInit.Start 调用 EnsureRuntimePanel() 自动挂载到 UI/Foreground
    ///   2. Editor 菜单：工具 > 智能体 > 主动技能系统 > 安装技能HUD到Game场景
    ///   3. 不直接写入 Game.unity 或 ResourcesLocal Prefab
    /// </summary>
    public class SkillHUD : MonoBehaviour
    {
        /// <summary>运行时单例实例（动态创建的 HUD 对象）</summary>
        public static SkillHUD RuntimeInstance { get; private set; }

        /// <summary>技能按钮 Image 组件数组（4个槽位）</summary>
        private Image[] skillButtonImages;

        /// <summary>冷却覆盖层 Image 组件数组</summary>
        private Image[] cooldownOverlays;

        /// <summary>冷却剩余时间 Text 组件数组</summary>
        private Text[] cooldownTexts;

        /// <summary>法力消耗 Text 组件数组</summary>
        private Text[] manaCostTexts;

        /// <summary>技能名称 Text 组件数组</summary>
        private Text[] skillNameTexts;

        /// <summary>技能等级 Text 组件数组</summary>
        private Text[] skillLevelTexts;

        /// <summary>快捷键 Text 组件数组</summary>
        private Text[] hotkeyTexts;

        private bool isVisible = true;
        private float lastRefreshTime;
        private IGameLogger gameLogger;

        private IGameLogger GameLogger => this.gameLogger ?? (this.gameLogger = GameLoggerFactory.Get());

        /// <summary>
        /// 确保运行时技能 HUD 已创建。若已存在则跳过。
        /// 挂载到 UI/Foreground 下，复用 UI 的 Canvas。
        /// </summary>
        public static void EnsureRuntimePanel()
        {
            if (RuntimeInstance != null)
            {
                return;
            }

            Transform uiRoot = GameObject.FindGameObjectWithTag(TagConstant.UI_TAG)?.transform;
            Transform foreground = uiRoot?.Find("Foreground");
            Transform hudTransform = foreground?.Find(SkillConstant.SkillHUDRootName);
            if (hudTransform != null)
            {
                RuntimeInstance = hudTransform.GetComponent<SkillHUD>();
                if (RuntimeInstance != null)
                {
                    RuntimeInstance.EnsureReferences();
                }
                else
                {
                    GameLoggerFactory.Get().LogWarning($"[SkillHUD] 场景中存在 {SkillConstant.SkillHUDRootName} 但未挂载 SkillHUD 组件。");
                }

                return;
            }

            GameLoggerFactory.Get().LogWarning($"[SkillHUD] 在 Foreground 下未找到 {SkillConstant.SkillHUDRootName}，请手动创建。");
        }

        /// <summary>
        /// 切换 HUD 显示/隐藏。
        /// </summary>
        public void ToggleVisibility()
        {
            this.isVisible = !this.isVisible;
            this.gameObject.SetActive(this.isVisible);
        }

        /// <summary>
        /// 从场景中移除技能 HUD（Editor 菜单用）。
        /// </summary>
        public static void RemoveFromScene()
        {
            GameObject root = GameObject.Find(SkillConstant.SkillHUDRootName);
            if (root != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(root);
                }
                else
                {
                    DestroyImmediate(root);
                }
            }

            RuntimeInstance = null;
        }

        private void Awake()
        {
            this.EnsureReferences();
        }

        public void EnsureReferences()
        {
            if (this.skillButtonImages != null) return;

            int count = 4;
            this.skillButtonImages = new Image[count];
            this.cooldownOverlays = new Image[count];
            this.cooldownTexts = new Text[count];
            this.manaCostTexts = new Text[count];
            this.skillNameTexts = new Text[count];
            this.skillLevelTexts = new Text[count];
            this.hotkeyTexts = new Text[count];

            for (int i = 0; i < count; i++)
            {
                Transform btnTransform = this.transform.Find($"{SkillConstant.SkillButtonPrefix}{i}");
                if (btnTransform == null) continue;

                this.skillButtonImages[i] = btnTransform.GetComponent<Image>();
                this.cooldownOverlays[i] = btnTransform.Find(SkillConstant.CooldownOverlayName)?.GetComponent<Image>();
                this.cooldownTexts[i] = btnTransform.Find("CooldownText")?.GetComponent<Text>();
                this.hotkeyTexts[i] = btnTransform.Find(SkillConstant.SkillHotkeyTextName)?.GetComponent<Text>();
                this.manaCostTexts[i] = btnTransform.Find(SkillConstant.ManaCostTextName)?.GetComponent<Text>();
                this.skillNameTexts[i] = btnTransform.Find(SkillConstant.SkillNameTextName)?.GetComponent<Text>();
                this.skillLevelTexts[i] = btnTransform.Find(SkillConstant.SkillLevelTextName)?.GetComponent<Text>();
            }
        }

        private void Update()
        {
            if (!this.isVisible)
            {
                return;
            }

            // 节流刷新
            if (Time.time - this.lastRefreshTime < SkillConstant.HudRefreshInterval)
            {
                return;
            }

            this.lastRefreshTime = Time.time;
            this.RefreshAllButtons();
        }

        private void OnDestroy()
        {
            if (RuntimeInstance == this)
            {
                RuntimeInstance = null;
            }
        }

        /// <summary>
        /// 创建4个技能按钮并挂载到指定父节点。
        /// </summary>
        private void CreateSkillButtons(Transform parent)
        {
            int skillCount = 4;
            this.skillButtonImages = new Image[skillCount];
            this.cooldownOverlays = new Image[skillCount];
            this.cooldownTexts = new Text[skillCount];
            this.manaCostTexts = new Text[skillCount];
            this.skillNameTexts = new Text[skillCount];
            this.skillLevelTexts = new Text[skillCount];
            this.hotkeyTexts = new Text[skillCount];

            for (int i = 0; i < skillCount; i++)
            {
                this.CreateSingleButton(parent, i);
            }

            // 设置根节点大小
            float totalWidth = (SkillConstant.SkillButtonWidth * skillCount)
                               + (SkillConstant.SkillButtonSpacing * (skillCount - 1));
            RectTransform rootRect = parent.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(totalWidth + 20f, SkillConstant.SkillButtonHeight + 30f);
        }

        /// <summary>
        /// 创建单个技能按钮及其子 UI 元素。
        /// </summary>
        private void CreateSingleButton(Transform parent, int index)
        {
            // 按钮背景
            GameObject btnObj = new GameObject($"{SkillConstant.SkillButtonPrefix}{index}");
            btnObj.transform.SetParent(parent, false);
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(SkillConstant.SkillButtonWidth, SkillConstant.SkillButtonHeight);

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = SkillConstant.CooldownReadyColor;
            this.skillButtonImages[index] = btnImage;

            // 冷却覆盖层（从底部向上覆盖表示冷却进度）
            GameObject overlayObj = new GameObject(SkillConstant.CooldownOverlayName);
            overlayObj.transform.SetParent(btnObj.transform, false);
            RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            Image overlayImage = overlayObj.AddComponent<Image>();
            overlayImage.color = SkillConstant.CooldownOverlayColor;
            overlayImage.raycastTarget = false;
            // 初始填充为0（冷却就绪）
            overlayImage.fillMethod = Image.FillMethod.Vertical;
            overlayImage.fillOrigin = (int)Image.OriginVertical.Bottom;
            overlayImage.fillAmount = 0f;
            this.cooldownOverlays[index] = overlayImage;

            // 冷却时间文本（居中显示）
            GameObject cdTextObj = new GameObject("CooldownText");
            cdTextObj.transform.SetParent(btnObj.transform, false);
            RectTransform cdTextRect = cdTextObj.AddComponent<RectTransform>();
            cdTextRect.anchorMin = Vector2.zero;
            cdTextRect.anchorMax = Vector2.one;
            cdTextRect.offsetMin = Vector2.zero;
            cdTextRect.offsetMax = Vector2.zero;
            Text cdText = cdTextObj.AddComponent<Text>();
            cdText.alignment = TextAnchor.MiddleCenter;
            cdText.fontSize = 36;
            cdText.fontStyle = FontStyle.Bold;
            cdText.color = Color.white;
            cdText.raycastTarget = false;
            cdText.text = string.Empty;
            cdText.font = LAB2D.AI.Dialogue.LLM.UIFontConfig.GetFont();
            this.cooldownTexts[index] = cdText;

            // 快捷键文本（左上角）
            GameObject hotkeyObj = new GameObject(SkillConstant.SkillHotkeyTextName);
            hotkeyObj.transform.SetParent(btnObj.transform, false);
            RectTransform hotkeyRect = hotkeyObj.AddComponent<RectTransform>();
            hotkeyRect.anchorMin = new Vector2(0f, 1f);
            hotkeyRect.anchorMax = new Vector2(0f, 1f);
            hotkeyRect.pivot = new Vector2(0f, 1f);
            hotkeyRect.anchoredPosition = new Vector2(5f, -4f);
            hotkeyRect.sizeDelta = new Vector2(38f, 34f);
            Text hotkeyText = hotkeyObj.AddComponent<Text>();
            hotkeyText.alignment = TextAnchor.MiddleLeft;
            hotkeyText.fontSize = 20;
            hotkeyText.fontStyle = FontStyle.Bold;
            hotkeyText.color = new Color(1f, 0.9f, 0.3f, 1f);
            hotkeyText.raycastTarget = false;
            hotkeyText.text = SkillTool.GetHotkeyDisplayText(index);
            hotkeyText.font = LAB2D.AI.Dialogue.LLM.UIFontConfig.GetFont();
            this.hotkeyTexts[index] = hotkeyText;

            // 法力消耗文本（右上角）
            GameObject manaObj = new GameObject(SkillConstant.ManaCostTextName);
            manaObj.transform.SetParent(btnObj.transform, false);
            RectTransform manaRect = manaObj.AddComponent<RectTransform>();
            manaRect.anchorMin = new Vector2(1f, 1f);
            manaRect.anchorMax = new Vector2(1f, 1f);
            manaRect.pivot = new Vector2(1f, 1f);
            manaRect.anchoredPosition = new Vector2(-5f, -4f);
            manaRect.sizeDelta = new Vector2(66f, 34f);
            Text manaText = manaObj.AddComponent<Text>();
            manaText.alignment = TextAnchor.MiddleRight;
            manaText.fontSize = 18;
            manaText.fontStyle = FontStyle.Bold;
            manaText.color = SkillConstant.ManaSufficientColor;
            manaText.raycastTarget = false;
            manaText.text = "MP:0";
            manaText.font = LAB2D.AI.Dialogue.LLM.UIFontConfig.GetFont();
            this.manaCostTexts[index] = manaText;

            // 技能名称文本（底部）
            GameObject nameObj = new GameObject(SkillConstant.SkillNameTextName);
            nameObj.transform.SetParent(btnObj.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 0f);
            nameRect.anchorMax = new Vector2(0.5f, 0f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.anchoredPosition = new Vector2(0f, 4f);
            nameRect.sizeDelta = new Vector2(108f, 26f);
            Text nameText = nameObj.AddComponent<Text>();
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.fontSize = 18;
            nameText.color = Color.white;
            nameText.raycastTarget = false;
            nameText.text = "技能";
            nameText.font = LAB2D.AI.Dialogue.LLM.UIFontConfig.GetFont();
            this.skillNameTexts[index] = nameText;

            // 技能等级文本（底部右侧）
            GameObject levelObj = new GameObject(SkillConstant.SkillLevelTextName);
            levelObj.transform.SetParent(btnObj.transform, false);
            RectTransform levelRect = levelObj.AddComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(1f, 0f);
            levelRect.anchorMax = new Vector2(1f, 0f);
            levelRect.pivot = new Vector2(1f, 0f);
            levelRect.anchoredPosition = new Vector2(-4f, 4f);
            levelRect.sizeDelta = new Vector2(36f, 24f);
            Text levelText = levelObj.AddComponent<Text>();
            levelText.alignment = TextAnchor.MiddleRight;
            levelText.fontSize = 16;
            levelText.color = new Color(0.5f, 1f, 0.5f, 1f);
            levelText.raycastTarget = false;
            levelText.text = "Lv1";
            levelText.font = LAB2D.AI.Dialogue.LLM.UIFontConfig.GetFont();
            this.skillLevelTexts[index] = levelText;
        }

        /// <summary>
        /// 刷新所有技能按钮的显示状态。
        /// 从 SkillManager 读取最新数据并更新 UI。
        /// </summary>
        private void RefreshAllButtons()
        {
            SkillManager mgr = Core.ServiceLocator.Get<SkillManager>();
            if (mgr == null || !mgr.IsInitialized)
            {
                return;
            }

            Player player = Core.ServiceLocator.TryGet(out PlayerManager pm) ? pm.Mine : null;
            int currentMp = player != null && player.CharacterDataLAB != null
                ? player.CharacterDataLAB.Mp
                : 0;

            for (int i = 0; i < 4; i++)
            {
                SkillData skill = mgr.GetSkill(i);
                if (skill == null)
                {
                    continue;
                }

                float remaining = skill.GetRemainingCooldown(Time.time);
                bool isReady = skill.IsReadyAt(Time.time);
                bool hasMana = SkillTool.HasEnoughMana(currentMp, skill.ManaCost);
                float progress = SkillTool.GetCooldownProgress(remaining, skill.CurrentCooldown);

                // 冷却覆盖层
                if (this.cooldownOverlays[i] != null)
                {
                    this.cooldownOverlays[i].fillAmount = progress;
                }

                // 冷却时间文本
                if (this.cooldownTexts[i] != null)
                {
                    this.cooldownTexts[i].text = SkillTool.FormatCooldownRemaining(remaining);
                    this.cooldownTexts[i].enabled = !isReady;
                }

                // 按钮背景颜色
                if (this.skillButtonImages[i] != null)
                {
                    this.skillButtonImages[i].color = isReady
                        ? SkillConstant.CooldownReadyColor
                        : SkillConstant.CooldownActiveColor;
                }

                // 法力消耗文本
                if (this.manaCostTexts[i] != null)
                {
                    this.manaCostTexts[i].text = $"MP:{skill.ManaCost}";
                    this.manaCostTexts[i].color = hasMana
                        ? SkillConstant.ManaSufficientColor
                        : SkillConstant.ManaInsufficientColor;
                }

                // 技能名称
                if (this.skillNameTexts[i] != null)
                {
                    this.skillNameTexts[i].text = skill.SkillName;
                }

                // 技能等级
                if (this.skillLevelTexts[i] != null)
                {
                    this.skillLevelTexts[i].text = $"Lv{skill.Level}";
                }
            }
        }
    }
}
