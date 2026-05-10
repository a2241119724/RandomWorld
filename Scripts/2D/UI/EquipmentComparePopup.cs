namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 装备对比弹窗。
    /// 当玩家拾取装备时，自动弹出对比弹窗，显示当前已装备 vs 新装备的属性差异。
    /// 使用运行时动态创建独立 Canvas 的方式生成 UI。
    /// MonoBehaviour 组件，通过 EnsureRuntimePopup() 静态方法安全创建。
    /// </summary>
    public class EquipmentComparePopup : MonoBehaviour
    {
        /// <summary>运行时弹窗单例引用</summary>
        private static EquipmentComparePopup runtimeInstance;

        private Canvas canvas;
        private GameObject rootPanel;
        /// <summary>替换回调</summary>
        private Action onReplace;

        /// <summary>丢弃回调</summary>
        private Action onDiscard;

        /// <summary>
        /// 确保运行时对比弹窗存在（如果不存在则创建）。
        /// </summary>
        public static void EnsureRuntimePopup()
        {
            if (runtimeInstance != null && runtimeInstance.rootPanel != null) return;

            GameObject go = new GameObject("Ambitious_A010_ComparePopup_Manager");
            DontDestroyOnLoad(go);
            runtimeInstance = go.AddComponent<EquipmentComparePopup>();
            runtimeInstance.CreateUI();
        }

        /// <summary>
        /// 获取运行时实例。
        /// </summary>
        public static EquipmentComparePopup Instance
        {
            get { return runtimeInstance; }
        }

        /// <summary>
        /// 创建对比弹窗 UI（独立 Canvas + Panel）。
        /// </summary>
        private void CreateUI()
        {
            // 创建独立 Canvas
            GameObject canvasGo = new GameObject(EquipmentLootConstant.ComparePopupCanvasName);
            canvasGo.transform.SetParent(this.transform, false);
            this.canvas = canvasGo.AddComponent<Canvas>();
            this.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            this.canvas.sortingOrder = EquipmentLootConstant.ComparePopupSortingOrder;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            // 创建背景遮罩（半透明黑色，点击空白区域关闭）
            GameObject bgGo = new GameObject("BgMask");
            bgGo.transform.SetParent(canvasGo.transform, false);
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.5f);
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            Button bgBtn = bgGo.AddComponent<Button>();
            bgBtn.onClick.AddListener(() => this.Hide());

            // 创建根面板
            this.rootPanel = new GameObject(EquipmentLootConstant.ComparePopupRootName);
            this.rootPanel.transform.SetParent(canvasGo.transform, false);
            Image panelImg = this.rootPanel.AddComponent<Image>();
            panelImg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
            RectTransform panelRt = this.rootPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(EquipmentLootConstant.ComparePopupWidth, EquipmentLootConstant.ComparePopupHeight);

            // 创建 VerticalLayoutGroup 用于内容排列
            VerticalLayoutGroup vlg = this.rootPanel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(32, 32, 24, 24);
            vlg.spacing = 12;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = this.rootPanel.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 初始隐藏：禁用整个 Canvas 避免拦截点击
            this.canvas.enabled = false;
            this.rootPanel.SetActive(false);
        }

        /// <summary>
        /// 显示装备对比弹窗。
        /// </summary>
        /// <param name="oldAttr">当前已装备的属性（可为 null 表示空槽）</param>
        /// <param name="newAttr">新装备的属性</param>
        /// <param name="rarity">新装备稀有度</param>
        /// <param name="slotType">装备槽位类型</param>
        /// <param name="onReplace">替换回调</param>
        /// <param name="onDiscard">丢弃回调</param>
        public void ShowCompare(
            Character.Attribute oldAttr,
            Character.Attribute newAttr,
            EquipmentRarityType rarity,
            AEquipment.EquipTypeEnum slotType,
            Action onReplace,
            Action onDiscard)
        {
            if (this.rootPanel == null)
            {
                this.CreateUI();
            }

            this.onReplace = onReplace;
            this.onDiscard = onDiscard;

            // 清空旧子内容（保留 LayoutGroup 和 ContentSizeFitter）
            for (int i = this.rootPanel.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = this.rootPanel.transform.GetChild(i).gameObject;
                Destroy(child);
            }

            // 标题
            this.CreateTextInPanel("Title", EquipmentLootConstant.ComparePopupTitle,
                EquipmentLootConstant.TitleFontSize, Color.white, TextAnchor.MiddleCenter);

            // 稀有度标签
            string rarityLabel = EquipmentLootTool.FormatRarityLabel(rarity);
            Color rarityColor = EquipmentLootTool.GetRarityColor(rarity);
            this.CreateTextInPanel("RarityLabel", rarityLabel,
                EquipmentLootConstant.RarityLabelFontSize, rarityColor, TextAnchor.MiddleCenter);

            // 槽位名称
            string slotName = EquipmentLootTool.GetSlotName(slotType);
            this.CreateTextInPanel("SlotLabel", "槽位: " + slotName,
                EquipmentLootConstant.PanelFontSize, new Color(0.8f, 0.8f, 0.8f), TextAnchor.MiddleCenter);

            // 分隔线
            this.CreateTextInPanel("Separator", "──────────────",
                EquipmentLootConstant.PanelFontSize, Color.gray, TextAnchor.MiddleCenter);

            // 属性对比标题
            this.CreateTextInPanel("CompareTitle", "属性对比（旧 → 新）",
                EquipmentLootConstant.PanelFontSize, new Color(0.9f, 0.9f, 0.5f), TextAnchor.MiddleCenter);

            // 属性对比行
            List<string> compareLines = EquipmentLootTool.BuildCompareLines(oldAttr, newAttr);
            foreach (string line in compareLines)
            {
                Color lineColor = Color.white;
                if (line.StartsWith(EquipmentLootConstant.StatUpPrefix))
                    lineColor = new Color(0.2f, 1.0f, 0.2f);
                else if (line.StartsWith(EquipmentLootConstant.StatDownPrefix))
                    lineColor = new Color(1.0f, 0.2f, 0.2f);

                this.CreateTextInPanel("StatLine", line,
                    EquipmentLootConstant.PanelFontSize, lineColor, TextAnchor.MiddleLeft);
            }

            // 提升统计
            int upgrades = EquipmentLootTool.CountUpgrades(oldAttr, newAttr);
            string summary = upgrades >= 5 ? "大幅提升！建议替换" :
                             upgrades >= 3 ? "有所提升" :
                             upgrades >= 1 ? "略微提升" :
                             oldAttr == null ? "空槽装备" : "不如当前装备";
            Color summaryColor = upgrades >= 3 ? Color.green : (upgrades >= 1 ? Color.yellow : Color.red);
            if (oldAttr == null) summaryColor = Color.cyan;

            this.CreateTextInPanel("Summary", summary,
                EquipmentLootConstant.PanelFontSize, summaryColor, TextAnchor.MiddleCenter);

            // 按钮区域
            GameObject btnRow = new GameObject("ButtonRow");
            btnRow.transform.SetParent(this.rootPanel.transform, false);
            HorizontalLayoutGroup hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 40;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            RectTransform btnRowRt = btnRow.GetComponent<RectTransform>();
            btnRowRt.sizeDelta = new Vector2(EquipmentLootConstant.ComparePopupWidth - 64, 80);

            // 替换按钮
            this.CreateButtonInPanel(btnRow.transform, "ReplaceBtn", EquipmentLootConstant.ReplaceButtonText,
                new Color(0.2f, 0.7f, 0.2f), () =>
                {
                    this.onReplace?.Invoke();
                    this.Hide();
                });

            // 丢弃按钮
            this.CreateButtonInPanel(btnRow.transform, "DiscardBtn", EquipmentLootConstant.DiscardButtonText,
                new Color(0.7f, 0.2f, 0.2f), () =>
                {
                    this.onDiscard?.Invoke();
                    this.Hide();
                });

            // 显示
            this.canvas.enabled = true;
            this.rootPanel.SetActive(true);
        }

        /// <summary>
        /// 隐藏对比弹窗。
        /// </summary>
        public void Hide()
        {
            if (this.canvas != null)
            {
                this.canvas.enabled = false;
            }

            if (this.rootPanel != null)
            {
                this.rootPanel.SetActive(false);
            }

            this.onReplace = null;
            this.onDiscard = null;
        }

        /// <summary>
        /// 在根面板中创建文本对象。
        /// </summary>
        private void CreateTextInPanel(string name, string text, int fontSize, Color color, TextAnchor alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(this.rootPanel.transform, false);
            Text txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(EquipmentLootConstant.ComparePopupWidth - 64, fontSize + 16);
        }

        /// <summary>
        /// 在指定父节点下创建按钮。
        /// </summary>
        private void CreateButtonInPanel(Transform parent, string name, string text, Color bgColor, Action onClick)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = bgColor;
            Button btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(280, 72);

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            Text label = labelGo.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = EquipmentLootConstant.PanelFontSize;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;
        }
    }
}
