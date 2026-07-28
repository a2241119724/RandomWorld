namespace LAB2D.Constant
{
    using UnityEngine;

    /// <summary>
    /// 主动技能系统公共常量：技能ID、默认参数、UI节点名、菜单路径、HUD参数等。
    /// 所有技能相关魔法值统一维护于此，业务脚本通过常量引用而非硬编码。
    /// 修改任何默认值会影响技能手感，请在 Play Mode 中验证。
    /// </summary>
    public static class SkillConstant
    {
        #region 技能ID

        /// <summary>旋风斩技能唯一标识</summary>
        public const string SkillWhirlwind = "skill_whirlwind";

        /// <summary>冲刺技能唯一标识</summary>
        public const string SkillDash = "skill_dash";

        /// <summary>力量爆发技能唯一标识</summary>
        public const string SkillPowerSurge = "skill_power_surge";

        /// <summary>治疗之光技能唯一标识</summary>
        public const string SkillHealingLight = "skill_healing_light";

        #endregion

        #region 旋风斩默认参数

        /// <summary>旋风斩伤害倍率（基于ATN的倍数）</summary>
        public const float WhirlwindDamageMultiplier = 2.0f;

        /// <summary>旋风斩AOE半径（世界单位）</summary>
        public const float WhirlwindRadius = 3.0f;

        /// <summary>旋风斩法力消耗</summary>
        public const int WhirlwindManaCost = 20;

        /// <summary>旋风斩冷却时间（秒）</summary>
        public const float WhirlwindCooldown = 5.0f;

        #endregion

        #region 冲刺默认参数

        /// <summary>冲刺距离（世界单位）</summary>
        public const float DashDistance = 4.0f;

        /// <summary>冲刺无敌帧持续时间（秒）</summary>
        public const float DashInvincibilityDuration = 0.3f;

        /// <summary>冲刺法力消耗</summary>
        public const int DashManaCost = 15;

        /// <summary>冲刺冷却时间（秒）</summary>
        public const float DashCooldown = 3.0f;

        #endregion

        #region 力量爆发默认参数

        /// <summary>力量爆发攻击力倍率</summary>
        public const float PowerSurgeAtnMultiplier = 1.5f;

        /// <summary>力量爆发持续时间（秒）</summary>
        public const float PowerSurgeDuration = 8.0f;

        /// <summary>力量爆发法力消耗</summary>
        public const int PowerSurgeManaCost = 30;

        /// <summary>力量爆发冷却时间（秒）</summary>
        public const float PowerSurgeCooldown = 15.0f;

        #endregion

        #region 治疗之光默认参数

        /// <summary>治疗之光回复生命值（固定值）</summary>
        public const float HealingLightHealAmount = 30.0f;

        /// <summary>治疗之光法力消耗</summary>
        public const int HealingLightManaCost = 25;

        /// <summary>治疗之光冷却时间（秒）</summary>
        public const float HealingLightCooldown = 12.0f;

        #endregion

        #region 技能升级参数

        /// <summary>每级伤害/效果提升比例（乘法，1级=1.0，2级=1.15，依此类推）</summary>
        public const float UpgradeEffectIncrease = 0.15f;

        /// <summary>每级冷却缩减比例（乘法，1级=1.0，2级=0.9，依此类推）</summary>
        public const float UpgradeCooldownReduction = 0.1f;

        /// <summary>技能最大等级</summary>
        public const int MaxSkillLevel = 5;

        /// <summary>从1级升到2级所需经验点数</summary>
        public const int UpgradeCostLevel1To2 = 1;

        /// <summary>从2级升到3级所需经验点数</summary>
        public const int UpgradeCostLevel2To3 = 2;

        /// <summary>从3级升到4级所需经验点数</summary>
        public const int UpgradeCostLevel3To4 = 3;

        /// <summary>从4级升到5级所需经验点数</summary>
        public const int UpgradeCostLevel4To5 = 5;

        #endregion

        #region UI 节点名

        /// <summary>技能HUD独立Canvas名称</summary>
        public const string SkillCanvasName = "SkillHUDCanvas";

        /// <summary>技能HUD根节点名称</summary>
        public const string SkillHUDRootName = "SkillHUDRoot";

        /// <summary>技能按钮前缀</summary>
        public const string SkillButtonPrefix = "SkillButton";

        /// <summary>冷却覆盖层节点名</summary>
        public const string CooldownOverlayName = "CooldownOverlay";

        /// <summary>法力消耗文本节点名</summary>
        public const string ManaCostTextName = "ManaCostText";

        /// <summary>技能名称文本节点名</summary>
        public const string SkillNameTextName = "SkillNameText";

        /// <summary>技能等级文本节点名</summary>
        public const string SkillLevelTextName = "SkillLevelText";

        /// <summary>快捷键文本节点名</summary>
        public const string SkillHotkeyTextName = "SkillHotkeyText";

        #endregion

        #region 菜单路径

        /// <summary>Editor 菜单根路径</summary>
        public const string MenuRoot = "工具/技能/";

        /// <summary>安装技能HUD到Game场景菜单项</summary>
        public const string MenuInstallToScene = "安装技能HUD到Game场景";

        /// <summary>从Game场景移除技能HUD菜单项</summary>
        public const string MenuRemoveFromScene = "从Game场景移除技能HUD";

        /// <summary>验证主动技能系统完整性菜单项</summary>
        public const string MenuVerifySystem = "验证主动技能系统完整性";

        #endregion

        #region 默认文案

        public const string DefaultSkillNameWhirlwind = "旋风斩";
        public const string DefaultSkillNameDash = "冲刺";
        public const string DefaultSkillNamePowerSurge = "力量爆发";
        public const string DefaultSkillNameHealingLight = "治疗之光";

        public const string DefaultSkillDescWhirlwind = "对周围敌人造成范围伤害";
        public const string DefaultSkillDescDash = "快速冲刺并获得短暂无敌";
        public const string DefaultSkillDescPowerSurge = "短时间内大幅提升攻击力";
        public const string DefaultSkillDescHealingLight = "回复自身生命值";

        public const string ManaInsufficientTip = "法力不足";
        public const string SkillOnCooldownTip = "技能冷却中";
        public const string SkillUpgradedTip = "技能升级！";

        #endregion

        #region HUD 参数

        /// <summary>HUD 刷新间隔（秒），避免每帧重建UI</summary>
        public const float HudRefreshInterval = 0.2f;

        /// <summary>HUD Canvas 渲染排序层级，低于浮动文字(100)和成就弹窗(200)</summary>
        public const int CanvasSortingOrder = 80;

        /// <summary>技能按钮宽度（像素）</summary>
        public const float SkillButtonWidth = 120f;

        /// <summary>技能按钮高度（像素）</summary>
        public const float SkillButtonHeight = 120f;

        /// <summary>技能按钮间距（像素）</summary>
        public const float SkillButtonSpacing = 18f;

        /// <summary>HUD 底部边距（像素）</summary>
        public const float HudBottomMargin = 40f;

        /// <summary>默认字体资源路径（复用项目已有字体）</summary>
        public const string FontResourcePath = "Fonts/LegacyRuntime";

        #endregion

        #region 颜色常量

        /// <summary>技能就绪时的按钮颜色</summary>
        public static readonly Color CooldownReadyColor = new Color(1f, 1f, 1f, 1f);

        /// <summary>技能冷却中按钮颜色</summary>
        public static readonly Color CooldownActiveColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);

        /// <summary>法力不足时法力消耗文本颜色</summary>
        public static readonly Color ManaInsufficientColor = new Color(0.5f, 0.5f, 1f, 0.9f);

        /// <summary>法力充足时法力消耗文本颜色</summary>
        public static readonly Color ManaSufficientColor = new Color(0.6f, 0.8f, 1f, 0.9f);

        /// <summary>冷却覆盖层颜色（半透明黑）</summary>
        public static readonly Color CooldownOverlayColor = new Color(0f, 0f, 0f, 0.6f);

        #endregion
    }
}
