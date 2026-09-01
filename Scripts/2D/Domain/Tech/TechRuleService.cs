namespace LAB2D.Domain.Tech
{
    /// <summary>
    /// 科技规则服务 — 研究可行性判定与已研究加成聚合（纯函数，便于单测）。
    /// </summary>
    public static class TechRuleService
    {
        /// <summary>
        /// 是否可研究：未研究过且研究点足够。
        /// </summary>
        /// <param name="isResearched">是否已研究。</param>
        /// <param name="researchPoints">当前研究点。</param>
        /// <param name="def">科技定义。</param>
        /// <returns>是否可研究。</returns>
        public static bool CanResearch(bool isResearched, float researchPoints, TechDef def)
        {
            return def != null && !isResearched && researchPoints >= def.Cost;
        }

        /// <summary>
        /// 聚合已研究科技的总加成到指定维度。
        /// </summary>
        /// <param name="researchedIds">已研究科技 Id 集合。</param>
        /// <param name="selector">加成维度选择器。</param>
        /// <returns>加数总和（0.25 = +25%），无匹配返回 0。</returns>
        public static float SumBonus(System.Collections.Generic.IEnumerable<string> researchedIds, System.Func<TechDef, float> selector)
        {
            if (researchedIds == null || selector == null)
            {
                return 0f;
            }

            float sum = 0f;
            foreach (string id in researchedIds)
            {
                TechDef def = TechLibrary.Get(id);
                if (def != null)
                {
                    sum += selector(def);
                }
            }

            return sum;
        }
    }
}
