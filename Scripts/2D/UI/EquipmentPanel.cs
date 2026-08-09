namespace LAB2D.UI
{
    using LAB2D;
    using LAB2D.Manager;
    using LAB2D.UnityAdapter;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using GameCharacter = LAB2D.Character.Character;

    /// <summary>
    /// 装备管理面板。
    /// 展示玩家所有装备槽位及其属性，支持卸下装备。
    /// F9 切换显示/隐藏，通过预制体（EquipmentPanel）加载 UI 结构，运行时从 AssetBundle 实例化。
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
        /// 确保运行时装备面板存在（如果不存在则从预制体创建）。
        /// </summary>
        public static void EnsureRuntimePanel()
        {
            if (runtimeInstance != null && runtimeInstance.rootPanel != null) return;

            GameObject go = Core.ServiceLocator.Get<ResourceManager>().Instantiate("EquipmentPanel", isLocal: false);
            go.name = "EquipmentPanelManager";
            DontDestroyOnLoad(go);
            runtimeInstance = go.GetComponent<EquipmentPanel>();
            if (runtimeInstance == null)
            {
                runtimeInstance = go.AddComponent<EquipmentPanel>();
            }

            runtimeInstance.InitializeReferences();
        }

        /// <summary>
        /// 获取运行时实例。
        /// </summary>
        public static EquipmentPanel Instance
        {
            get { return runtimeInstance; }
        }

        /// <summary>
        /// 从预制体初始化引用（替代代码生成 UI）。
        /// </summary>
        private void InitializeReferences()
        {
            this.canvas = this.transform.Find(EquipmentLootConstant.EquipmentPanelCanvasName)?.GetComponent<Canvas>();
            Transform rootTr = this.transform.Find(EquipmentLootConstant.EquipmentPanelCanvasName + "/" + EquipmentLootConstant.EquipmentPanelRootName);
            if (rootTr != null)
            {
                this.rootPanel = rootTr.gameObject;
            }

            // 初始隐藏
            if (this.canvas != null)
            {
                this.canvas.enabled = false;
            }

            if (this.rootPanel != null)
            {
                this.rootPanel.SetActive(false);
            }

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
                this.InitializeReferences();
            }

            if (this.rootPanel == null) return;

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

            Player player = Core.ServiceLocator.TryGet(out PlayerManager pm) ? pm.Mine : null;
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
    }
}
