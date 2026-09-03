namespace LAB2D.Domain.TurnBattle
{
    using System.Collections.Generic;
    using LAB2D.Domain.Gameplay.Cultivation;

    /// <summary>
    /// 五行克制规则 — 回合制技能元素 vs 防守方灵根的伤害倍率。
    /// 相克环：金→木→土→水→火→金（克者 → 被克者）。
    /// 克制优先：防守方多灵根同时存在克/被克关系时取克制（鼓励对症下药）。
    /// Enemy 无灵根恒中性 1.0（LingGenRuleService 只对玩家/Worker 生成灵根）。
    /// </summary>
    public static class ElementCounterRuleService
    {
        /// <summary>克制倍率（攻击元素克防守任一灵根）。</summary>
        public const float CounterMultiplier = 1.30f;

        /// <summary>抵抗倍率（防守任一灵根克攻击元素）。</summary>
        public const float ResistMultiplier = 0.75f;

        /// <summary>中性倍率（无元素/相生/无关/无灵根）。</summary>
        public const float NeutralMultiplier = 1.00f;

        /// <summary>
        /// 相克环取被克元素：金克木、木克土、土克水、水克火、火克金。
        /// </summary>
        public static Element GetCounteredElement(Element element)
        {
            switch (element)
            {
                case Element.Metal: return Element.Wood;
                case Element.Wood:  return Element.Earth;
                case Element.Earth: return Element.Water;
                case Element.Water: return Element.Fire;
                case Element.Fire:  return Element.Metal;
                default:            return element;
            }
        }

        /// <summary>attacker 是否克 defender。</summary>
        public static bool Counters(Element attacker, Element defender)
        {
            return GetCounteredElement(attacker) == defender;
        }

        /// <summary>
        /// 攻击元素对防守方灵根的克制倍率：克 1.30 / 被克 0.75 / 其余 1.00。
        /// 无元素技能（默认四技能）与无灵根防守方（Enemy）恒 1.00。
        /// </summary>
        public static float GetCounterMultiplier(Element? attackElement, List<Element> defenderLingGen)
        {
            if (attackElement == null || defenderLingGen == null || defenderLingGen.Count == 0)
            {
                return NeutralMultiplier;
            }

            Element attack = attackElement.Value;

            // 克制优先：任一灵根被攻击元素克 → 1.30
            foreach (Element lingGen in defenderLingGen)
            {
                if (Counters(attack, lingGen))
                {
                    return CounterMultiplier;
                }
            }

            // 抵抗：任一灵根克攻击元素 → 0.75
            foreach (Element lingGen in defenderLingGen)
            {
                if (Counters(lingGen, attack))
                {
                    return ResistMultiplier;
                }
            }

            return NeutralMultiplier;
        }
    }
}
