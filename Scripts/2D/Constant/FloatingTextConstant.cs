namespace LAB2D.Constant
{
    using UnityEngine;

    /// <summary>
    /// 浮动战斗文字系统常量
    /// 集中管理颜色、字号、动画参数、对象池大小、节点命名和 Editor 菜单路径。
    /// 修改颜色/字号/动画参数会影响所有浮动文字表现；修改节点名需同步 FloatingTextTool。
    /// </summary>
    public static class FloatingTextConstant
    {
        // =============================================================================================================
        // 颜色定义（遵循 PixelUITheme 可爱像素风配色）
        // =============================================================================================================

        /// <summary>普通伤害颜色：暖金色</summary>
        public static readonly Color DamageColor = new Color32(255, 245, 200, 255);

        /// <summary>暴击伤害颜色：橙红色</summary>
        public static readonly Color CriticalColor = new Color32(255, 120, 80, 255);

        /// <summary>治疗颜色：薄荷绿</summary>
        public static readonly Color HealColor = new Color32(126, 203, 90, 255);

        /// <summary>连击颜色：金色</summary>
        public static readonly Color ComboColor = new Color32(249, 213, 110, 255);

        /// <summary>经验颜色：天蓝</summary>
        public static readonly Color ExperienceColor = new Color32(124, 184, 228, 255);

        /// <summary>闪避颜色：灰色</summary>
        public static readonly Color DodgeColor = new Color32(160, 150, 140, 255);

        /// <summary>状态效果颜色：淡紫</summary>
        public static readonly Color StatusEffectColor = new Color32(197, 180, 227, 255);

        // =============================================================================================================
        // 字号定义
        // =============================================================================================================

        /// <summary>普通伤害字号</summary>
        public const int DamageFontSize = 36;

        /// <summary>暴击伤害字号（较大以突出强调）</summary>
        public const int CriticalFontSize = 52;

        /// <summary>治疗字号</summary>
        public const int HealFontSize = 32;

        /// <summary>连击字号（特大）</summary>
        public const int ComboFontSize = 60;

        /// <summary>经验字号（较小）</summary>
        public const int ExperienceFontSize = 24;

        /// <summary>闪避字号</summary>
        public const int DodgeFontSize = 30;

        /// <summary>状态效果字号（较小）</summary>
        public const int StatusEffectFontSize = 26;

        // =============================================================================================================
        // 动画参数
        // =============================================================================================================

        /// <summary>默认上浮速度（世界单位/秒）</summary>
        public const float DefaultFloatSpeed = 2.5f;

        /// <summary>暴击上浮速度（稍快）</summary>
        public const float CriticalFloatSpeed = 3.5f;

        /// <summary>连击上浮速度（快速）</summary>
        public const float ComboFloatSpeed = 4.0f;

        /// <summary>经验/状态上浮速度（较慢）</summary>
        public const float SlowFloatSpeed = 1.5f;

        /// <summary>默认存活时间（秒），超时后开始淡出</summary>
        public const float DefaultLifetime = 0.8f;

        /// <summary>暴击存活时间（稍长以增强视觉冲击）</summary>
        public const float CriticalLifetime = 1.0f;

        /// <summary>连击存活时间</summary>
        public const float ComboLifetime = 1.2f;

        /// <summary>淡出持续时间（秒）</summary>
        public const float FadeOutDuration = 0.3f;

        /// <summary>暴击文字弹出缩放倍数</summary>
        public const float CriticalPopScale = 1.4f;

        /// <summary>连击文字弹出缩放倍数</summary>
        public const float ComboPopScale = 1.6f;

        /// <summary>弹出缩放动画持续时间（秒）</summary>
        public const float PopAnimDuration = 0.12f;

        /// <summary>水平随机偏移范围（世界单位）</summary>
        public const float RandomOffsetX = 0.35f;

        // =============================================================================================================
        // 对象池
        // =============================================================================================================

        /// <summary>对象池默认容量</summary>
        public const int DefaultPoolSize = 30;

        /// <summary>对象池最大容量</summary>
        public const int MaxPoolSize = 60;

        // =============================================================================================================
        // 节点名
        // =============================================================================================================

        /// <summary>浮动文字 Canvas 名称</summary>
        public const string CanvasName = "FloatingTextCanvas";

        /// <summary>浮动文字根节点名称</summary>
        public const string RootName = "FloatingTextRoot";

        /// <summary>浮动文字池容器名称</summary>
        public const string PoolContainerName = "FloatingTextPool";

        /// <summary>单个浮动文字对象前缀</summary>
        public const string FloatingTextPrefix = "FloatingText";

        // =============================================================================================================
        // 常量文案
        // =============================================================================================================

        /// <summary>闪避显示文字</summary>
        public const string DodgeText = "MISS";

        /// <summary>治疗文字前缀</summary>
        public const string HealPrefix = "+";

        /// <summary>经验文字前缀</summary>
        public const string ExpPrefix = "+";

        /// <summary>经验文字后缀</summary>
        public const string ExpSuffix = " EXP";

        /// <summary>连击文字前缀</summary>
        public const string ComboPrefix = "x";

        // =============================================================================================================
        // Editor 菜单路径
        // =============================================================================================================

        /// <summary>验证浮动文字系统配置的菜单路径</summary>
        public const string EditorMenuValidate = "工具/战斗文字/验证浮动文字系统配置";
    }
}
