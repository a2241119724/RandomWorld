namespace LAB2D.Constant
{
    using UnityEngine;

    /// <summary>
    /// 波次 Boss 与波间奖励系统常量。
    /// 集中维护 Boss 判定、属性倍率、奖励数值、UI 节点、菜单路径和默认文案，避免魔法值散落到业务脚本。
    /// 修改这些数值会影响关卡节奏和战斗手感，提交前应在 Play Mode 中复验。
    /// </summary>
    public class WaveBossRewardConstant
    {
        /// <summary>Editor 菜单根路径。</summary>
        public const string MenuRoot = "工具/智能体/波次Boss奖励/";

        /// <summary>真实 Game 场景名称。</summary>
        public const string GameSceneName = "Game";

        /// <summary>运行时独立 Canvas 名称。</summary>
        public const string CanvasName = "Ambitious_A004_WaveBossReward_Canvas";

        /// <summary>奖励面板根节点名称。</summary>
        public const string PanelRootName = "Ambitious_A004_WaveBossReward_Root";

        /// <summary>奖励面板标题节点名称。</summary>
        public const string TitleTextName = "WaveBossRewardTitle";

        /// <summary>奖励面板摘要节点名称。</summary>
        public const string SummaryTextName = "WaveBossRewardSummary";

        /// <summary>奖励按钮节点名称前缀。</summary>
        public const string OptionButtonPrefix = "WaveBossRewardOption_";

        /// <summary>奖励按钮文本节点名称前缀。</summary>
        public const string OptionTextPrefix = "WaveBossRewardOptionText_";

        /// <summary>字体资源路径，使用项目现有中文像素字体。</summary>
        public const string FontResourcePath = "Font/ark-pixel-12px-monospaced-zh_cn";

        /// <summary>奖励面板默认标题。</summary>
        public const string PanelTitle = "波间奖励";

        /// <summary>没有待选奖励时的默认文案。</summary>
        public const string EmptyRewardText = "等待波次奖励...";

        /// <summary>Boss 波出现间隔，每 3 波触发一次。</summary>
        public const int BossWaveInterval = 3;

        /// <summary>每次波间奖励提供的选项数量。</summary>
        public const int RewardOptionCount = 3;

        /// <summary>Boss 波额外生成的护卫敌人数。</summary>
        public const int BossGuardianExtraEnemyCount = 1;

        /// <summary>普通敌人生命缩放每波增量。</summary>
        public const float NormalEnemyHealthScalePerWave = 0.08f;

        /// <summary>普通敌人攻击缩放每波增量。</summary>
        public const float NormalEnemyAttackScalePerWave = 0.05f;

        /// <summary>普通敌人防御缩放每波增量。</summary>
        public const float NormalEnemyDefenseScalePerWave = 0.03f;

        /// <summary>Boss 生命基础倍率。</summary>
        public const float BossHealthMultiplier = 2.8f;

        /// <summary>Boss 攻击基础倍率。</summary>
        public const float BossAttackMultiplier = 1.65f;

        /// <summary>Boss 防御基础倍率。</summary>
        public const float BossDefenseMultiplier = 1.35f;

        /// <summary>Boss 视觉缩放倍率。</summary>
        public const float BossVisualScale = 1.45f;

        /// <summary>普通波回血奖励比例。</summary>
        public const float NormalHealPercent = 0.32f;

        /// <summary>Boss 波回血奖励比例。</summary>
        public const float BossHealPercent = 0.55f;

        /// <summary>普通波经验奖励基础值。</summary>
        public const int NormalExperienceBase = 8;

        /// <summary>Boss 波经验奖励基础值。</summary>
        public const int BossExperienceBase = 20;

        /// <summary>普通波伤害强化增量。</summary>
        public const float NormalDamageBoost = 0.10f;

        /// <summary>Boss 波伤害强化增量。</summary>
        public const float BossDamageBoost = 0.18f;

        /// <summary>普通波减伤强化增量。</summary>
        public const float NormalDefenseBoost = 0.08f;

        /// <summary>Boss 波减伤强化增量。</summary>
        public const float BossDefenseBoost = 0.14f;

        /// <summary>普通波移动强化增量。</summary>
        public const float NormalMoveSpeedBoost = 0.06f;

        /// <summary>Boss 波移动强化增量。</summary>
        public const float BossMoveSpeedBoost = 0.10f;

        /// <summary>玩家本局伤害强化上限，防止奖励堆叠失控。</summary>
        public const float MaxPlayerDamageBonus = 0.75f;

        /// <summary>玩家本局减伤上限，避免伤害完全归零。</summary>
        public const float MaxPlayerDamageReduction = 0.45f;

        /// <summary>玩家本局移动强化上限，避免移动手感失控。</summary>
        public const float MaxPlayerMoveSpeedBonus = 0.35f;

        /// <summary>奖励面板刷新间隔。</summary>
        public const float PanelRefreshInterval = 0.25f;

        /// <summary>选择第一个奖励的热键。</summary>
        public const KeyCode RewardOptionOneKey = InputKeyConstant.BossRewardOption1;

        /// <summary>选择第二个奖励的热键。</summary>
        public const KeyCode RewardOptionTwoKey = InputKeyConstant.BossRewardOption2;

        /// <summary>选择第三个奖励的热键。</summary>
        public const KeyCode RewardOptionThreeKey = InputKeyConstant.BossRewardOption3;
    }
}
