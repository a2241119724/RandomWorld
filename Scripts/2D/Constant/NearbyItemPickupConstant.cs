namespace LAB2D.Constant
{
    using UnityEngine;

    /// <summary>
    /// 附近道具拾取系统公共常量。
    /// </summary>
    public static class NearbyItemPickupConstant
    {
        // ============================================================
        // UI 节点名
        // ============================================================

        public const string CanvasName = "NearbyItemPickup";
        public const string PanelRootName = "NearbyItemPickupPanel";

        // ============================================================
        // Canvas 排序层级
        // ============================================================

        public const int CanvasSortingOrder = 90;

        // ============================================================
        // 检测参数
        // ============================================================

        /// <summary>检测半径（瓦片格数），默认3格即7x7区域</summary>
        public const int DetectionRadius = 3;

        /// <summary>轮询间隔（秒）</summary>
        public const float PollInterval = 0.3f;

        // ============================================================
        // 默认文案
        // ============================================================

        public const string PanelTitle = "附近道具";
        public const string PickUpButtonText = "拾取";
        public const string EmptyHint = "附近没有道具";

        // ============================================================
        // 面板布局参数
        // ============================================================

        /// <summary>面板宽度</summary>
        public const float PanelWidth = 260f;

        /// <summary>面板最大高度</summary>
        public const float PanelMaxHeight = 450f;

        /// <summary>标题栏高度</summary>
        public const float TitleBarHeight = 36f;

        /// <summary>单个道具条目高度</summary>
        public const float ItemEntryHeight = 44f;

        /// <summary>条目间间距</summary>
        public const float ItemEntrySpacing = 4f;

        /// <summary>面板内边距</summary>
        public const float Padding = 12f;

        /// <summary>字体大小 - 标题</summary>
        public const int TitleFontSize = 24;

        /// <summary>字体大小 - 道具名称</summary>
        public const int ItemNameFontSize = 12;

        /// <summary>字体大小 - 数量</summary>
        public const int CountFontSize = 12;

        /// <summary>字体大小 - 拾取按钮</summary>
        public const int ButtonFontSize = 12;

        /// <summary>拾取按钮宽度</summary>
        public const float PickUpButtonWidth = 64f;

        /// <summary>拾取按钮高度</summary>
        public const float PickUpButtonHeight = 30f;

        /// <summary>面板距屏幕右侧的偏移</summary>
        public const float PanelRightMargin = 20f;

        /// <summary>面板距屏幕顶部的偏移</summary>
        public const float PanelTopMargin = -20f;

        // ============================================================
        // 颜色
        // ============================================================

        public static readonly Color PanelBgColor = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        public static readonly Color TitleColor = new Color(1f, 0.85f, 0.3f);
        public static readonly Color ItemNameColor = Color.white;
        public static readonly Color CountColor = new Color(0.7f, 0.7f, 0.7f);
        public static readonly Color PickUpBtnColor = new Color(0.2f, 0.6f, 0.2f);
        public static readonly Color EntryBgColor = new Color(0.18f, 0.18f, 0.18f, 0.9f);
        public static readonly Color EntryBgColorAlt = new Color(0.22f, 0.22f, 0.22f, 0.9f);
    }
}
