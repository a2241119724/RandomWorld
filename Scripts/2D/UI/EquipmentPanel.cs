namespace LAB2D.UI
{
    using LAB2D;
    using LAB2D.UnityAdapter;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using GameCharacter = LAB2D.Character.Character;

    /// <summary>
    /// 装备管理面板。
    /// 展示玩家所有装备槽位及其属性，支持卸下装备。
    /// F9 切换显示/隐藏，运行时动态创建独立 Canvas。
    /// MonoBehaviour 组件，通过 EnsureRuntimePanel() 静态方法安全创建。
    /// </summary>
    public class EquipmentPanel : MonoBehaviour
    {
        /// <summary>运行时面板单例引用</summary>
        private static EquipmentPanel runtimeInstance;

        private Canvas canvas;
        private GameObject rootPanel;
        private bool isVisible;

        /// <summary>
        /// 确保运行时装备面板存在（如果不存在则创建）。
        /// </summary>
        public static void EnsureRuntimePanel()
        {
            if (runtimeInstance != null && runtimeInstance.rootPanel != null) return;

            GameObject go = new GameObject("EquipmentPanelManager");
            DontDestroyOnLoad(go);
            runtimeInstance = go.AddComponent<EquipmentPanel>();
            runtimeInstance.CreateUI();
        }

        /// <summary>
        /// 获取运行时实例。
        /// </summary>
        public static EquipmentPanel Instance
        {
            get { return runtimeInstance; }
        }

        /// <summary>
        /// 创建装备面板 UI（独立 Canvas + 槽位列表）。
        /// </summary>
        private void CreateUI()
        {
            // 创建独立 Canvas
            GameObject canvasGo = new GameObject(EquipmentLootConstant.EquipmentPanelCanvasName);
            canvasGo.transform.SetParent(this.transform, false);
            this.canvas = canvasGo.AddComponent<Canvas>();
            this.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            this.canvas.sortingOrder = EquipmentLootConstant.EquipmentPanelSortingOrder;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            // 创建根面板
            this.rootPanel = new GameObject(EquipmentLootConstant.EquipmentPanelRootName);
            this.rootPanel.transform.SetParent(canvasGo.transform, false);
            Image panelImg = this.rootPanel.AddComponent<Image>();
            panelImg.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);
            RectTransform panelRt = this.rootPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(EquipmentLootConstant.PanelWidth, EquipmentLootConstant.PanelHeight);

            // 内容排列
            VerticalLayoutGroup vlg = this.rootPanel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 20, 20);
            vlg.spacing = 8;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // 标题
            this.CreateTextInPanel("Title", EquipmentLootConstant.EquipmentPanelTitle,
                EquipmentLootConstant.TitleFontSize, Color.white, TextAnchor.MiddleCenter);

            // 提示行
            this.CreateTextInPanel("Hint", "按 F9 关闭 | 显示所有已装备物品属性",
                24, new Color(0.6f, 0.6f, 0.6f), TextAnchor.MiddleCenter);

            // 总属性标题
            this.CreateTextInPanel("TotalTitle", "── 总属性加成 ──",
                EquipmentLootConstant.PanelFontSize, new Color(0.9f, 0.9f, 0.3f), TextAnchor.MiddleCenter);

            // 总属性文本（动态更新）
            this.CreateTextInPanel("TotalStats", "加载中...",
                EquipmentLootConstant.PanelFontSize, Color.white, TextAnchor.MiddleCenter);

            // 分隔线
            this.CreateTextInPanel("Separator", "──────────────",
                EquipmentLootConstant.PanelFontSize, Color.gray, TextAnchor.MiddleCenter);

            // 槽位标题
            this.CreateTextInPanel("SlotTitle", "── 装备槽位 ──",
                EquipmentLootConstant.PanelFontSize, new Color(0.9f, 0.9f, 0.3f), TextAnchor.MiddleCenter);

            // 为每个装备槽位创建一行
            AEquipment.EquipTypeEnum[] slotTypes = (AEquipment.EquipTypeEnum[])
                System.Enum.GetValues(typeof(AEquipment.EquipTypeEnum));

            foreach (AEquipment.EquipTypeEnum slotType in slotTypes)
            {
                if (slotType == AEquipment.EquipTypeEnum.Null) continue;

                string slotName = EquipmentLootTool.GetSlotName(slotType);
                string slotKey = "Slot_" + slotType.ToString();
                this.CreateTextInPanel(slotKey, slotName + ": " + EquipmentLootConstant.EmptySlotText,
                    EquipmentLootConstant.PanelFontSize, new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleLeft);
            }

            // 初始隐藏：禁用整个 Canvas 避免拦截点击
            this.canvas.enabled = false;
            this.rootPanel.SetActive(false);
            this.isVisible = false;
        }

        /// <summary>
        /// Unity Update：检测 F9 切换键。
        /// </summary>
        private void Update()
        {
            if (UnityGlobalInputAdapter.GetEquipmentPanelToggleDown(this.isVisible))
            {
                this.Toggle();
            }

            if (this.isVisible)
            {
                this.RefreshContent();
            }
        }

        /// <summary>
        /// 切换面板显示/隐藏。
        /// </summary>
        public void Toggle()
        {
            if (this.rootPanel == null)
            {
                this.CreateUI();
            }

            this.isVisible = !this.isVisible;
            if (this.canvas != null)
            {
                this.canvas.enabled = this.isVisible;
            }

            this.rootPanel.SetActive(this.isVisible);

            if (this.isVisible)
            {
                this.RefreshContent();
            }
        }

        /// <summary>
        /// 显示面板。
        /// </summary>
        public void Show()
        {
            if (!this.isVisible) this.Toggle();
        }

        /// <summary>
        /// 隐藏面板。
        /// </summary>
        public void Hide()
        {
            if (this.isVisible) this.Toggle();
        }

        /// <summary>
        /// 刷新面板内容（总属性 + 各槽位装备信息）。
        /// </summary>
        public void RefreshContent()
        {
            if (this.rootPanel == null) return;

            Player player = PlayerManager.Instance?.Mine;
            if (player == null) return;

            GameCharacter.CharacterData charData = player.CharacterDataLAB;
            if (charData == null) return;

            Dictionary<AEquipment.EquipTypeEnum, AEquipment> equipments = charData.GetEquipments();

            // 更新总属性文本
            Transform totalStatTr = this.rootPanel.transform.Find("TotalStats");
            if (totalStatTr != null)
            {
                Text totalText = totalStatTr.GetComponent<Text>();
                if (totalText != null)
                {
                    GameCharacter.Attribute equipOnlyAttr = new GameCharacter.Attribute();
                    if (equipments != null)
                    {
                        foreach (AEquipment eq in equipments.Values)
                        {
                            if (eq?.Attribute != null)
                            {
                                equipOnlyAttr.ATN += eq.Attribute.ATN;
                                equipOnlyAttr.INT += eq.Attribute.INT;
                                equipOnlyAttr.DEF += eq.Attribute.DEF;
                                equipOnlyAttr.RES += eq.Attribute.RES;
                                equipOnlyAttr.CRT += eq.Attribute.CRT;
                                equipOnlyAttr.CSD += eq.Attribute.CSD;
                                equipOnlyAttr.SPD += eq.Attribute.SPD;
                                equipOnlyAttr.HIT += eq.Attribute.HIT;
                            }
                        }
                    }

                    totalText.text = string.Format(
                        "ATN:+{0:F0} INT:+{1:F0} DEF:+{2:F0} RES:+{3:F0}\nCRT:+{4:F2} CSD:+{5:F1} SPD:+{6:F0} HIT:+{7:F0}",
                        equipOnlyAttr.ATN, equipOnlyAttr.INT, equipOnlyAttr.DEF, equipOnlyAttr.RES,
                        equipOnlyAttr.CRT, equipOnlyAttr.CSD, equipOnlyAttr.SPD, equipOnlyAttr.HIT);
                }
            }

            // 更新各槽位行
            AEquipment.EquipTypeEnum[] slotTypes = (AEquipment.EquipTypeEnum[])
                System.Enum.GetValues(typeof(AEquipment.EquipTypeEnum));

            foreach (AEquipment.EquipTypeEnum slotType in slotTypes)
            {
                if (slotType == AEquipment.EquipTypeEnum.Null) continue;

                string slotKey = "Slot_" + slotType.ToString();
                Transform slotTr = this.rootPanel.transform.Find(slotKey);
                if (slotTr == null) continue;

                Text slotText = slotTr.GetComponent<Text>();
                if (slotText == null) continue;

                string slotName = EquipmentLootTool.GetSlotName(slotType);

                if (equipments != null && equipments.TryGetValue(slotType, out AEquipment eq) && eq?.Attribute != null)
                {
                    // 按品质获取显示颜色
                    Color qualityColor = EquipmentLootTool.GetQualityColor(eq.Quality);
                    string attrSummary = EquipmentLootTool.FormatAttributeSummary(eq.Attribute);
                    slotText.text = string.Format("{0} [{1}]: {2}",
                        slotName, eq.Quality.ToString(), attrSummary);
                    slotText.color = qualityColor;
                }
                else
                {
                    slotText.text = slotName + ": " + EquipmentLootConstant.EmptySlotText;
                    slotText.color = new Color(0.5f, 0.5f, 0.5f);
                }
            }
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
            rt.sizeDelta = new Vector2(EquipmentLootConstant.PanelWidth - 48, fontSize + 20);
        }
    }
}
