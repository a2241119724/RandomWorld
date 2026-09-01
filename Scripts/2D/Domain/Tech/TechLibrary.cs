namespace LAB2D.Domain.Tech
{
    using System.Collections.Generic;

    /// <summary>
    /// 科技静态库 — 全部科技的唯一定义点。
    /// 本版 3 条（最小可玩）：灵耕术（农耕提速）/ 聚灵阵（解锁建筑+打坐提速）/ 高级研究法（研究提速）。
    /// </summary>
    public static class TechLibrary
    {
        /// <summary>灵耕术 — 农耕 +25%。</summary>
        public static readonly TechDef SpiritFarming = new TechDef
        {
            Id = "tech_spirit_farming",
            Name = "灵耕术",
            Description = "以灵气浸润土壤，农耕工作速度 +25%",
            Cost = 30f,
            FarmSpeedBonus = 0.25f,
        };

        /// <summary>聚灵阵 — 解锁聚灵阵建筑，打坐灵气积累 +50%。</summary>
        public static readonly TechDef SpiritArray = new TechDef
        {
            Id = "tech_spirit_array",
            Name = "聚灵阵",
            Description = "解锁建筑「聚灵阵」，打坐灵气积累 +50%",
            Cost = 60f,
            UnlockBuildName = "SpiritArray",
            MeditateSpeedBonus = 0.5f,
        };

        /// <summary>高级研究法 — 研究点产出 ×2。</summary>
        public static readonly TechDef AdvancedResearch = new TechDef
        {
            Id = "tech_advanced_research",
            Name = "高级研究法",
            Description = "改良研究方法论，研究点产出 ×2",
            Cost = 120f,
            ResearchSpeedBonus = 1.0f,
        };

        /// <summary>全部科技（面板展示顺序即此表顺序）。</summary>
        public static readonly List<TechDef> All = new List<TechDef>
        {
            SpiritFarming,
            SpiritArray,
            AdvancedResearch,
        };

        /// <summary>按 Id 查询科技，未找到返回 null。</summary>
        public static TechDef Get(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return All.Find(t => t.Id == id);
        }
    }
}
