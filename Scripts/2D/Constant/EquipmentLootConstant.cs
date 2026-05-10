namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 装备掉落系统公共常量。
    /// 统一管理稀有度颜色、属性倍率、掉落概率、UI 节点名、菜单路径、默认文案和快捷键。
    /// 所有装备相关子模块通过本类获取固定值，避免魔法数字和硬编码字符串散落。
    /// </summary>
    public static class EquipmentLootConstant
    {
        // ============================================================
        // 稀有度颜色（RGBA，与 BackpackItemQualityEnum 视觉对应）
        // ============================================================

        /// <summary>Common 普通 — 灰色</summary>
        public static readonly Color CommonColor = new Color(0.6f, 0.6f, 0.6f);

        /// <summary>Uncommon 不凡 — 绿色</summary>
        public static readonly Color UncommonColor = new Color(0.2f, 0.8f, 0.2f);

        /// <summary>Rare 稀有 — 蓝色</summary>
        public static readonly Color RareColor = new Color(0.2f, 0.4f, 1.0f);

        /// <summary>Epic 史诗 — 紫色</summary>
        public static readonly Color EpicColor = new Color(0.7f, 0.2f, 1.0f);

        /// <summary>Legendary 传说 — 橙色</summary>
        public static readonly Color LegendaryColor = new Color(1.0f, 0.55f, 0.0f);

        /// <summary>Mythic 神话 — 红色</summary>
        public static readonly Color MythicColor = new Color(1.0f, 0.15f, 0.15f);

        // ============================================================
        // 稀有度属性倍率（作用于 AEquipment.RankRandom 上下限）
        // ============================================================

        /// <summary>Common 属性倍率</summary>
        public const float CommonStatMultiplier = 1.0f;

        /// <summary>Uncommon 属性倍率</summary>
        public const float UncommonStatMultiplier = 1.3f;

        /// <summary>Rare 属性倍率</summary>
        public const float RareStatMultiplier = 1.6f;

        /// <summary>Epic 属性倍率</summary>
        public const float EpicStatMultiplier = 2.0f;

        /// <summary>Legendary 属性倍率</summary>
        public const float LegendaryStatMultiplier = 2.5f;

        /// <summary>Mythic 属性倍率</summary>
        public const float MythicStatMultiplier = 3.2f;

        // ============================================================
        // 掉落概率（权重制，总和=100）
        // ============================================================

        /// <summary>基础装备掉落概率（敌人死亡时）</summary>
        public const float BaseEquipmentDropChance = 0.10f;

        /// <summary>Common 掉落权重</summary>
        public const float CommonWeight = 50f;

        /// <summary>Uncommon 掉落权重</summary>
        public const float UncommonWeight = 25f;

        /// <summary>Rare 掉落权重</summary>
        public const float RareWeight = 15f;

        /// <summary>Epic 掉落权重</summary>
        public const float EpicWeight = 7f;

        /// <summary>Legendary 掉落权重</summary>
        public const float LegendaryWeight = 2.5f;

        /// <summary>Mythic 掉落权重</summary>
        public const float MythicWeight = 0.5f;

        /// <summary>稀有度权重总和</summary>
        public const float RarityWeightTotal = CommonWeight + UncommonWeight + RareWeight + EpicWeight + LegendaryWeight + MythicWeight;

        /// <summary>每波稀有度加权提升值（越高波次稀有掉落越多）</summary>
        public const float RarityWeightBonusPerWave = 0.03f;

        // ============================================================
        // 极值属性（Legendary+ 装备的某条属性翻倍）
        // ============================================================

        /// <summary>传说级装备极值属性条数</summary>
        public const int LegendaryExtremeStatCount = 1;

        /// <summary>神话级装备极值属性条数</summary>
        public const int MythicExtremeStatCount = 2;

        /// <summary>极值属性倍率</summary>
        public const float ExtremeStatMultiplier = 2.0f;

        // ============================================================
        // UI 节点名
        // ============================================================

        /// <summary>装备面板 Canvas 名称</summary>
        public const string EquipmentPanelCanvasName = "Ambitious_A010_EquipmentPanel_Canvas";

        /// <summary>装备对比弹窗 Canvas 名称</summary>
        public const string ComparePopupCanvasName = "Ambitious_A010_ComparePopup_Canvas";

        /// <summary>装备面板根节点名</summary>
        public const string EquipmentPanelRootName = "Ambitious_A010_EquipmentPanel_Root";

        /// <summary>对比弹窗根节点名</summary>
        public const string ComparePopupRootName = "Ambitious_A010_ComparePopup_Root";

        // ============================================================
        // Canvas 排序层级
        // ============================================================

        /// <summary>装备面板排序层级</summary>
        public const int EquipmentPanelSortingOrder = 120;

        /// <summary>对比弹窗排序层级</summary>
        public const int ComparePopupSortingOrder = 250;

        // ============================================================
        // 快捷键
        // ============================================================

        /// <summary>装备面板切换键（F9）</summary>
        public const KeyCode EquipmentPanelToggleKey = KeyCode.F9;

        // ============================================================
        // 默认文案
        // ============================================================

        /// <summary>装备面板标题</summary>
        public const string EquipmentPanelTitle = "装备管理";

        /// <summary>对比弹窗标题</summary>
        public const string ComparePopupTitle = "装备对比";

        /// <summary>装备槽位为空时的占位文案</summary>
        public const string EmptySlotText = "（空）";

        /// <summary>替换按钮文案</summary>
        public const string ReplaceButtonText = "替换";

        /// <summary>丢弃按钮文案</summary>
        public const string DiscardButtonText = "丢弃";

        /// <summary>属性提升标记</summary>
        public const string StatUpPrefix = "↑ ";

        /// <summary>属性下降标记</summary>
        public const string StatDownPrefix = "↓ ";

        /// <summary>属性不变标记</summary>
        public const string StatEqualPrefix = "= ";

        /// <summary>稀有度标签格式</summary>
        public const string RarityLabelFormat = "[{0}]";

        /// <summary>属性显示格式</summary>
        public const string StatDisplayFormat = "{0}: {1:F1}";

        /// <summary>对比属性行格式</summary>
        public const string CompareStatFormat = "{0}: {1:F1} → {2:F1}";

        // ============================================================
        // Editor 菜单路径
        // ============================================================

        /// <summary>Editor 菜单根路径</summary>
        public const string EditorMenuRoot = "工具/智能体/装备掉落系统/";

        /// <summary>安装 UI 菜单路径</summary>
        public const string EditorMenuInstall = EditorMenuRoot + "安装装备掉落 UI 到 Game 场景";

        /// <summary>卸载 UI 菜单路径</summary>
        public const string EditorMenuUninstall = EditorMenuRoot + "从 Game 场景移除装备掉落 UI";

        /// <summary>测试掉落菜单路径</summary>
        public const string EditorMenuTestDrop = EditorMenuRoot + "测试掉落（打印稀有度分布）";

        // ============================================================
        // 面板布局参数
        // ============================================================

        /// <summary>面板宽度</summary>
        public const float PanelWidth = 1040f;

        /// <summary>面板高度</summary>
        public const float PanelHeight = 960f;

        /// <summary>对比弹窗宽度</summary>
        public const float ComparePopupWidth = 840f;

        /// <summary>对比弹窗高度</summary>
        public const float ComparePopupHeight = 760f;

        /// <summary>槽位行高度</summary>
        public const float SlotRowHeight = 72f;

        /// <summary>面板字体大小</summary>
        public const int PanelFontSize = 32;

        /// <summary>标题字体大小</summary>
        public const int TitleFontSize = 44;

        /// <summary>稀有度标签字体大小</summary>
        public const int RarityLabelFontSize = 28;
    }
}
