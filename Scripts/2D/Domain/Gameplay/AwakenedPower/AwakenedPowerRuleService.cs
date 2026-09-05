namespace LAB2D.Domain.Gameplay.AwakenedPower
{
    using System;
    using System.Collections.Generic;
    using LAB2D.Domain.Character.Growth;

    /// <summary>
    /// 异能觉醒规则服务 — 觉醒概率与条件的纯函数判定。
    /// 随机数经静态缝注入（Domain 层不依赖 UnityEngine），由 Gameplay 层接线。
    /// </summary>
    public static class AwakenedPowerRuleService
    {
        /// <summary>基础觉醒概率（满血时）。</summary>
        public const float BaseAwakenChance = 0.03f;

        /// <summary>濒死加成系数：血量越低越易觉醒（残血时概率最高 10%）。</summary>
        public const float LowHpBonusFactor = 0.07f;

        /// <summary>同时可拥有的异能上限（包 5 扩池 2→6 后由 1 提至 2：6 选 2，玩家技能栏 8 槽容纳）。</summary>
        public const int MaxAwakenedCount = 2;

        /// <summary>
        /// 随机数提供者（返回 [min, max)），Gameplay 层安装时注入。
        /// 未注入时觉醒判定恒不触发（测试可替换为序列桩）。
        /// </summary>
        public static Func<float, float, float> RandomFloatProvider { get; set; }

        /// <summary>
        /// 本次受击的觉醒概率：0.03 + (1 - Hp/MaxHp) × 0.07，濒死时约 10%。
        /// </summary>
        public static float GetAwakenChance(float hp, float maxHp)
        {
            if (maxHp <= 0f)
            {
                return 0f;
            }

            float hpRatio = Math.Min(1f, Math.Max(0f, hp / maxHp));
            return BaseAwakenChance + ((1f - hpRatio) * LowHpBonusFactor);
        }

        /// <summary>
        /// 是否可觉醒：尚未达到异能上限。
        /// </summary>
        public static bool CanAwaken(GrowthData growth)
        {
            if (growth == null || growth.AwakenedPowerIds == null)
            {
                return false;
            }

            return growth.AwakenedPowerIds.Count < MaxAwakenedCount;
        }

        /// <summary>
        /// 从觉醒池中随机取一个异能 Id（不与已有重复；池耗尽返回 null）。
        /// </summary>
        public static string RollPowerId(GrowthData growth)
        {
            if (!CanAwaken(growth) || AllAvailable(growth).Count == 0)
            {
                return null;
            }

            List<AwakenedPowerDef> pool = AllAvailable(growth);
            int pick = (int)(RandomFloatProvider?.Invoke(0f, pool.Count) ?? -1f);
            if (pick < 0 || pick >= pool.Count)
            {
                return null;
            }

            return pool[pick].Id;
        }

        /// <summary>未觉醒的异能子池。</summary>
        private static List<AwakenedPowerDef> AllAvailable(GrowthData growth)
        {
            List<AwakenedPowerDef> pool = new List<AwakenedPowerDef>();
            foreach (AwakenedPowerDef def in AwakenedPowerLibrary.All)
            {
                if (!growth.AwakenedPowerIds.Contains(def.Id))
                {
                    pool.Add(def);
                }
            }

            return pool;
        }
    }
}
