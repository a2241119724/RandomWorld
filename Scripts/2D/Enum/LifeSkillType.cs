namespace LAB2D.Enum
{
    /// <summary>
    /// 生活技能类型 — Worker 用进废退的熟练度维度（干活加经验，等级提升效率并解锁科技联动）。
    /// 从 WorkerTaskType 中的工作类任务映射：Gather(伐木/采矿按目标区分)、Plant(农耕)。
    /// </summary>
    public enum LifeSkillType
    {
        /// <summary>伐木 — 完成资源采集（树）任务积累经验。</summary>
        Felling = 0,

        /// <summary>采矿 — 完成地形挖掘（山/矿石）任务积累经验。</summary>
        Mining = 1,

        /// <summary>农耕 — 完成种植/收获任务积累经验。</summary>
        Farming = 2,
    }
}
