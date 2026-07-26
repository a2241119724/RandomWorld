namespace LAB2D.UI
{
    using UnityEngine;

    /// <summary>
    /// 可爱像素风 UI 主题配色
    /// </summary>
    public static class PixelUITheme
    {
        // === 主按钮 (确认/通用) ===
        public static readonly Color ButtonNormal = new Color32(242, 160, 175, 255);
        public static readonly Color ButtonHighlighted = new Color32(252, 200, 213, 255);
        public static readonly Color ButtonPressed = new Color32(249, 213, 110, 255);
        public static readonly Color ButtonSelected = new Color32(126, 203, 154, 255);
        public static readonly Color ButtonDisabled = new Color(0, 0, 0, 0);

        // === 危险按钮 (清除/删除) ===
        public static readonly Color DestroyNormal = new Color32(232, 131, 122, 255);
        public static readonly Color DestroyHighlighted = new Color32(240, 160, 153, 255);
        public static readonly Color DestroyPressed = new Color32(212, 112, 104, 255);

        // === 对话框 ===
        public static readonly Color DialogBoxBg = new Color32(255, 245, 236, 245);
        public static readonly Color ModalShade = new Color(0.29f, 0.22f, 0.16f, 0.50f);
        public static readonly Color DialogShadeDark = new Color(0.20f, 0.14f, 0.10f, 0.72f);
        public static readonly Color ViewportBg = new Color(0.25f, 0.18f, 0.14f, 0.30f);

        // === 文本颜色 ===
        public static readonly Color TextPrimary = new Color32(74, 55, 40, 255);
        public static readonly Color TextSecondary = new Color32(139, 125, 114, 255);
        public static readonly Color TextAccent = new Color32(232, 93, 117, 255);
        public static readonly Color TextOnDark = Color.white;

        // === RichText 颜色 (HTML Hex) ===
        public const string RichGold = "#F9D56E";
        public const string RichSky = "#7CB8E4";
        public const string RichPink = "#F2A0AF";
        public const string RichCoral = "#F27A6B";
        public const string RichMint = "#7ECB9A";
        public const string RichLavender = "#C5B4E3";

        // === 状态条 ===
        public static readonly Color HpBarFill = new Color32(242, 122, 107, 255);
        public static readonly Color MpBarFill = new Color32(197, 180, 227, 255);
        public static readonly Color ExpBarFill = new Color32(249, 213, 110, 255);

        // === 死亡画面 ===
        public static readonly Color DeathTitle = new Color32(255, 105, 125, 255);
        public static readonly Color DeathText = new Color32(220, 200, 180, 255);
        public static readonly Color DeathCount = new Color32(180, 160, 140, 255);

        // === 存档槽 ===
        public static readonly Color SaveSlotTitleText = new Color32(242, 122, 107, 255);

        // === 伤害数值 ===
        public static readonly Color DamageNormal = new Color32(255, 245, 200, 255);
        public static readonly Color DamageCritical = new Color32(255, 120, 110, 255);
    }
}
