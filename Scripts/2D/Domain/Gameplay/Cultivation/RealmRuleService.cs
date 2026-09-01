namespace LAB2D.Domain.Gameplay.Cultivation
{
    using LAB2D.Domain.Character.Growth;

    /// <summary>
    /// 修仙境界规则 — 打坐速率、突破条件与突破结算。
    /// 纯 C# 实现，无 UnityEngine 依赖，可独立测试。
    /// </summary>
    public sealed class RealmRuleService
    {
        /// <summary>打坐基础灵气获取速率（灵气/秒，未乘任何加成）。</summary>
        public const float MeditateQiPerSec = 2f;

        /// <summary>打坐回蓝速率（Mp/秒）。</summary>
        public const float MeditateMpPerSec = 2f;

        /// <summary>取角色当前境界定义。</summary>
        public static RealmDef GetRealm(GrowthData growth)
        {
            return RealmLibrary.Get(growth?.RealmIndex ?? 0);
        }

        /// <summary>从当前境界突破到下一境界所需灵气。</summary>
        public static float QiToNext(GrowthData growth)
        {
            return GetRealm(growth).QiToNext;
        }

        /// <summary>是否满足突破条件（未达最高境界且灵气充足）。</summary>
        public static bool CanBreakthrough(GrowthData growth)
        {
            if (growth == null || RealmLibrary.IsMax(growth.RealmIndex))
            {
                return false;
            }

            return growth.Qi >= GetRealm(growth).QiToNext;
        }

        /// <summary>
        /// 执行突破：扣除本境界所需灵气、境界 +1、永久加成累进。
        /// </summary>
        /// <returns>是否突破成功（不满足条件时为 false，数据不变）。</returns>
        public static bool Breakthrough(GrowthData growth)
        {
            if (!CanBreakthrough(growth))
            {
                return false;
            }

            RealmDef current = GetRealm(growth);
            growth.Qi -= current.QiToNext;
            growth.RealmIndex = current.Index + 1;
            growth.PermanentRealmBonus += RealmLibrary.Get(growth.RealmIndex).Bonus;
            return true;
        }

        /// <summary>
        /// 计算修炼灵气增量：基础速率 × 时长 × 场景系数 × 修炼速度倍率。
        /// 玩家打坐 Tick 与 Worker 睡眠吐纳共用此公式，保证两侧节奏一致。
        /// </summary>
        /// <param name="growth">修炼者成长数据（Special.CultivationSpeedMul 为内功修炼速度加成）。</param>
        /// <param name="seconds">修炼时长（秒）。</param>
        /// <param name="extraSpeedBonus">额外修炼速度加数（如聚灵阵科技 +0.5）。</param>
        /// <param name="scale">场景系数（床睡 1.0 / 地面睡 0.5）。</param>
        /// <returns>灵气增量（入参非法时为 0）。</returns>
        public static float ComputeQiGain(GrowthData growth, float seconds, float extraSpeedBonus = 0f, float scale = 1f)
        {
            if (growth == null || seconds <= 0f)
            {
                return 0f;
            }

            float speedMul = 1f + growth.Special.CultivationSpeedMul + extraSpeedBonus;
            return MeditateQiPerSec * seconds * scale * speedMul;
        }
    }
}
