namespace LAB2D.Domain.Gameplay.Cultivation
{
    using System;
    using System.Collections.Generic;
    using LAB2D.Domain.Character.Growth;

    /// <summary>
    /// 灵根规则 — 玩家出生时随机五行灵根（1 条 60% / 2 条 30% / 3 条 10%，五行均权不重复），
    /// 终身不变；灵根与功法元素的匹配数决定功法修炼速度加成。
    /// 随机依赖通过 <see cref="RandomFloatProvider"/> 注入（Gameplay 层封装 UnityEngine.Random），
    /// 未注入时不生成，可独立测试。
    /// </summary>
    public sealed class LingGenRuleService
    {
        /// <summary>随机浮点数提供者 (minInclusive, maxInclusive)。使用 Roll 前须由 Gameplay 层注入。</summary>
        public static Func<float, float, float> RandomFloatProvider { get; set; }

        /// <summary>灵根条数为 1 的概率。</summary>
        public const float CountOneWeight = 0.6f;

        /// <summary>灵根条数为 2 的概率（剩余 0.1 为 3 条）。</summary>
        public const float CountTwoWeight = 0.3f;

        /// <summary>每个匹配元素的功法修炼速度加成（加数，消费方 +1 使用）。</summary>
        public const float MatchBonusPerElement = 0.2f;

        /// <summary>
        /// 首次调用时随机灵根并标记已生成（终身不变）；玩家与 Worker 均会生成。
        /// 非修炼者（Enemy）、已生成或随机提供者未注入时为空操作。
        /// </summary>
        public static void RollIfNotGenerated(GrowthData growth, bool isPlayerOrWorker)
        {
            if (growth == null || !isPlayerOrWorker || growth.LingGenGenerated || RandomFloatProvider == null)
            {
                return;
            }

            int count = RollCount();
            List<Element> pool = new List<Element>
            {
                Element.Metal, Element.Wood, Element.Water, Element.Fire, Element.Earth,
            };

            growth.LingGenElements.Clear();
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int index = (int)RandomFloatProvider(0f, pool.Count - 1);
                if (index < 0)
                {
                    index = 0;
                }
                else if (index >= pool.Count)
                {
                    index = pool.Count - 1;
                }

                growth.LingGenElements.Add((int)pool[index]);
                pool.RemoveAt(index);
            }

            growth.LingGenGenerated = true;
        }

        /// <summary>
        /// 功法/异能元素与灵根的匹配修炼速度倍率（1 + 0.2 × 匹配数）。
        /// </summary>
        public static float GetCultivationMul(GrowthData growth, Element element)
        {
            if (growth == null || growth.LingGenElements == null)
            {
                return 1f;
            }

            int matches = 0;
            foreach (int owned in growth.LingGenElements)
            {
                if (owned == (int)element)
                {
                    matches++;
                }
            }

            return 1f + (MatchBonusPerElement * matches);
        }

        /// <summary>元素中文名。</summary>
        public static string GetElementName(Element element)
        {
            switch (element)
            {
                case Element.Metal: return "金";
                case Element.Wood:  return "木";
                case Element.Water: return "水";
                case Element.Fire:  return "火";
                case Element.Earth: return "土";
                default:            return element.ToString();
            }
        }

        /// <summary>
        /// 灵根中文名（顿号连接，如「金、水」）；未生成为 "无"。
        /// K 面板与开局揭晓 Tip 共用，单一格式来源。
        /// </summary>
        public static string FormatLingGenName(GrowthData growth)
        {
            if (growth?.LingGenElements == null || growth.LingGenElements.Count == 0)
            {
                return "无";
            }

            var builder = new System.Text.StringBuilder(16);
            foreach (int element in growth.LingGenElements)
            {
                if (builder.Length > 0)
                {
                    builder.Append('、');
                }

                builder.Append(GetElementName((Element)element));
            }

            return builder.ToString();
        }

        /// <summary>
        /// 开局揭晓文案（「天命灵根：金、水——双灵根，匹配元素功法修炼速度 +20%/条」）。
        /// 未生成/空灵根返回 null（调用方跳过揭晓）。
        /// </summary>
        public static string FormatRevealMessage(GrowthData growth)
        {
            if (growth == null || !growth.LingGenGenerated
                || growth.LingGenElements == null || growth.LingGenElements.Count == 0)
            {
                return null;
            }

            string rarity = growth.LingGenElements.Count >= 3
                ? "——罕见的多元天资"
                : (growth.LingGenElements.Count == 2 ? "——双灵根" : string.Empty);
            return $"天命灵根：{FormatLingGenName(growth)}{rarity}，"
                + $"匹配元素功法修炼速度 +{MatchBonusPerElement * 100f:F0}%/条（K 查看修仙面板）";
        }

        /// <summary>按权重滚动灵根条数。</summary>
        private static int RollCount()
        {
            float roll = RandomFloatProvider(0f, 1f);
            if (roll < CountOneWeight)
            {
                return 1;
            }

            if (roll < CountOneWeight + CountTwoWeight)
            {
                return 2;
            }

            return 3;
        }
    }
}
