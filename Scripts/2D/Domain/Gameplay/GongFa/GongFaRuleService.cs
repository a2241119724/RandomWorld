namespace LAB2D.Domain.Gameplay.GongFa
{
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Gameplay.Cultivation;

    /// <summary>
    /// 武学功法规则服务 — 学习/激活条件的纯函数判定与修炼速度计算。
    /// 不依赖 UnityEngine，随机数经静态缝由 Gameplay 层注入（本服务无需随机）。
    /// </summary>
    public static class GongFaRuleService
    {
        /// <summary>
        /// 是否可学习功法：未学过且境界达标。
        /// </summary>
        public static bool CanLearn(GrowthData growth, GongFaDef def)
        {
            if (growth == null || def == null)
            {
                return false;
            }

            return !growth.LearnedGongFaIds.Contains(def.Id)
                   && growth.RealmIndex >= def.RequiredRealmIndex;
        }

        /// <summary>
        /// 是否可激活内功：是内功且已学习。
        /// </summary>
        public static bool CanActivate(GrowthData growth, GongFaDef def)
        {
            if (growth == null || def == null || !def.IsNeiGong)
            {
                return false;
            }

            return growth.LearnedGongFaIds.Contains(def.Id);
        }

        /// <summary>
        /// 功法修炼速度倍率：激活内功的五行与灵根匹配数加成（每匹配一行 +20%）。
        /// 打坐灵气积累时由 CultivationManager 消费。
        /// </summary>
        public static float GetCultivationMul(GrowthData growth, GongFaDef activeNeiGong)
        {
            if (growth == null || activeNeiGong == null)
            {
                return 1f;
            }

            return LingGenRuleService.GetCultivationMul(growth, activeNeiGong.Element);
        }
    }
}
