namespace LAB2D.Domain.Gameplay
{
    /// <summary>
    /// 建筑伤害规则服务 — 建筑/山门核心耐久结算纯函数。
    /// M1.3 基建：妖兽子弹命中建筑走 BuildMap.DamageBuilding，山门核心走 MountainGateManager.DamageCore，
    /// 两者共用本服务的结算结果（扣血 + 是否被毁），保证数值规则单一来源。
    /// </summary>
    public sealed class BuildingDamageRuleService
    {
        /// <summary>普通建筑默认最大耐久（旧存档 Hp<=0 时兜底初始化）。</summary>
        public const float DefaultBuildingMaxHp = 200f;

        /// <summary>山门核心最大耐久。</summary>
        public const float CoreMaxHp = 500f;

        /// <summary>核心被击破后恢复的耐久比例（宽闸门：先降级再终局）。</summary>
        public const float CoreReviveHpRatio = 0.6f;

        /// <summary>核心可被击破的最大次数，第 CoreMaxDownfalls 次被毁即终局失败。</summary>
        public const int CoreMaxDownfalls = 3;

        /// <summary>核心升级等级上限（达到即阶段胜利）。</summary>
        public const int CoreMaxLevel = 3;

        /// <summary>核心 1→2 级升级金币消耗。</summary>
        public const int CoreUpgradeCostLevel2 = 200;

        /// <summary>核心 2→3 级升级金币消耗。</summary>
        public const int CoreUpgradeCostLevel3 = 500;

        /// <summary>
        /// 一次伤害的结算结果。
        /// </summary>
        public readonly struct BuildingDamageResult
        {
            /// <summary>结算后剩余耐久（被毁时为 0）。</summary>
            public float RemainingHp { get; }

            /// <summary>是否被摧毁。</summary>
            public bool IsDestroyed { get; }

            public BuildingDamageResult(float remainingHp, bool isDestroyed)
            {
                this.RemainingHp = remainingHp;
                this.IsDestroyed = isDestroyed;
            }
        }

        /// <summary>
        /// 结算一次伤害：扣血并判定是否被毁。伤害 <=0 时原样返回。
        /// </summary>
        /// <param name="currentHp">当前耐久。</param>
        /// <param name="damage">本次伤害（已过防御结算的最终值）。</param>
        /// <returns>结算结果。</returns>
        public BuildingDamageResult ApplyDamage(float currentHp, float damage)
        {
            if (damage <= 0f)
            {
                return new BuildingDamageResult(currentHp, false);
            }

            float hp = currentHp - damage;
            bool destroyed = hp <= 0f;
            return new BuildingDamageResult(destroyed ? 0f : hp, destroyed);
        }

        /// <summary>
        /// 核心被击破后降级恢复的耐久（宽闸门：不直接终局，留翻盘窗口）。
        /// </summary>
        public float ComputeCoreReviveHp()
        {
            return CoreMaxHp * CoreReviveHpRatio;
        }

        /// <summary>
        /// 核心从 fromLevel 升到下一级的金币消耗：1→200、2→500，已满级或非法等级返回 0（无可升级）。
        /// </summary>
        /// <param name="fromLevel">当前核心等级。</param>
        /// <returns>升级金币消耗；0 表示不可升级。</returns>
        public int GetCoreUpgradeCost(int fromLevel)
        {
            switch (fromLevel)
            {
                case 1: return CoreUpgradeCostLevel2;
                case 2: return CoreUpgradeCostLevel3;
                default: return 0;
            }
        }
    }
}
