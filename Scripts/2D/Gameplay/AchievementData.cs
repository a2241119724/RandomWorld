namespace LAB2D.Gameplay
{
    using LAB2D;
    using System;

    /// <summary>
    /// 成就数据定义
    /// 用途：存储单个成就的完整信息，包括 ID、名称、描述、类别、目标值、当前进度、状态和点数奖励。
    /// AchievementManager 维护所有成就实例的列表，UI 层只读查询。
    /// 每个成就实例是独立数据对象，可在运行时创建和更新。
    /// </summary>
    [Serializable]
    public class AchievementData
    {
        // --- 基本信息 ---

        /// <summary>成就唯一标识，如 "combat_kill_100"</summary>
        public string Id;

        /// <summary>成就显示名称（中文）</summary>
        public string Name;

        /// <summary>成就简短描述</summary>
        public string Description;

        /// <summary>成就类别</summary>
        public AchievementCategory Category;

        /// <summary>条件描述模板，如 "击杀 {0} 个敌人"，{0} 会被 TargetValue 替换</summary>
        public string ConditionTemplate;

        // --- 进度与状态 ---

        /// <summary>目标值</summary>
        public int TargetValue;

        /// <summary>当前进度值（跨局持久化时从存档恢复）</summary>
        public int CurrentProgress;

        /// <summary>成就当前状态</summary>
        public AchievementState State;

        // --- 奖励 ---

        /// <summary>解锁该成就获得的成就点数</summary>
        public int Points;

        // --- 扩展 ---

        /// <summary>图标资源名（预留，当前使用类别默认图标）</summary>
        public string IconName;

        /// <summary>进度是否跨局持久化</summary>
        public bool IsProgressPersistent;

        // --- 计算属性 ---

        /// <summary>进度比例（0.0 ~ 1.0）</summary>
        public float ProgressRatio
        {
            get
            {
                return AchievementTool.GetProgressRatio(this.CurrentProgress, this.TargetValue);
            }
        }

        /// <summary>是否已达成目标</summary>
        public bool IsTargetReached
        {
            get
            {
                return this.CurrentProgress >= this.TargetValue;
            }
        }

        /// <summary>格式化的进度文本</summary>
        public string ProgressText
        {
            get
            {
                return AchievementTool.FormatProgress(this.CurrentProgress, this.TargetValue);
            }
        }

        /// <summary>格式化的条件描述文本</summary>
        public string ConditionText
        {
            get
            {
                return AchievementTool.BuildConditionText(this.ConditionTemplate, this.TargetValue);
            }
        }

        /// <summary>类别中文显示名</summary>
        public string CategoryDisplayName
        {
            get
            {
                return AchievementTool.GetCategoryDisplayName(this.Category);
            }
        }

        /// <summary>状态中文显示名</summary>
        public string StateDisplayName
        {
            get
            {
                return AchievementTool.GetStateDisplayName(this.State);
            }
        }

        /// <summary>格式化的点数奖励文本</summary>
        public string PointsText
        {
            get
            {
                return AchievementTool.FormatPoints(this.Points);
            }
        }
    }
}
