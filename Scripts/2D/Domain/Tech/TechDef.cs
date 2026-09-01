namespace LAB2D.Domain.Tech
{
    /// <summary>
    /// 科技定义 — 研究点解锁的永久收益（数值加成或建筑解锁）。
    /// 纯 C# 数据类，静态库 <see cref="TechLibrary"/> 提供实例。
    /// </summary>
    public class TechDef
    {
        /// <summary>科技唯一标识（存档持久化键）。</summary>
        public string Id;

        /// <summary>科技显示名称。</summary>
        public string Name;

        /// <summary>科技描述文本。</summary>
        public string Description;

        /// <summary>研究所需科技点。</summary>
        public float Cost;

        /// <summary>
        /// 解锁的建筑名（对应 ABuildItem 类名/TileName），null 表示无数值解锁。
        /// 解锁后玩家才能手动建造该建筑。
        /// </summary>
        public string UnlockBuildName;

        /// <summary>农耕任务速度加成（加数，0.25 = +25%；消费方 +1 使用）。</summary>
        public float FarmSpeedBonus;

        /// <summary>研究点产出倍率加成（加数，1.0 = 产出 ×2；消费方 +1 使用）。</summary>
        public float ResearchSpeedBonus;

        /// <summary>打坐灵气积累加成（加数，0.5 = +50%；消费方 +1 使用）。</summary>
        public float MeditateSpeedBonus;
    }
}
