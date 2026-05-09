namespace LAB2D
{
    /// <summary>
    /// 浮动战斗文字类型枚举
    /// 定义不同战斗事件的浮动文字表现形式，用于区分颜色、大小和动画。
    ///
    /// 使用场景：FloatingTextManager、FloatingTextUI、FloatingTextTool
    /// 允许扩展：可追加新类型，不得删除或重命名已有值
    /// </summary>
    public enum FloatingTextType
    {
        /// <summary>
        /// 普通伤害：白色/淡黄色，标准字号，匀速上浮
        /// </summary>
        Damage = 0,

        /// <summary>
        /// 暴击伤害：橙红色，大字号，弹出缩放动画 + 上浮
        /// </summary>
        Critical = 1,

        /// <summary>
        /// 治疗/回复：绿色，标准字号，上浮 + 微缩放
        /// </summary>
        Heal = 2,

        /// <summary>
        /// 连击计数：金色，特大字号，弹出动画 + 快速上浮 + 短暂停留
        /// </summary>
        Combo = 3,

        /// <summary>
        /// 经验获取：蓝色，小字号，慢速上浮
        /// </summary>
        Experience = 4,

        /// <summary>
        /// 闪避/未命中：灰色，标准字号，"MISS" 固定文字，水平漂移
        /// </summary>
        Dodge = 5,

        /// <summary>
        /// 状态效果：紫色，小字号，显示状态名称（如 "中毒"、"减速"），慢速上浮
        /// </summary>
        StatusEffect = 6,
    }
}
