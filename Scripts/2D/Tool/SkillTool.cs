namespace LAB2D.Tool
{
    using LAB2D;
    using LAB2D.Character.Enemy;
    using LAB2D.Domain.Gameplay;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 主动技能系统公共工具类。
    /// 提供技能伤害计算、冷却时间格式化、法力校验、范围敌人查询、升级成本计算等可复用方法。
    /// 所有方法均为静态，无状态依赖，不引用 UnityEditor。
    /// Manager 和 HUD 子模块均通过本工具类完成共享计算，避免重复逻辑。
    /// </summary>
    public static class SkillTool
    {
        private static readonly SkillRuleService RuleService = new SkillRuleService();

        /// <summary>
        /// 计算技能实际伤害值。
        /// </summary>
        /// <param name="baseAtn">玩家基础物理攻击力</param>
        /// <param name="damageMultiplier">技能伤害倍率</param>
        /// <param name="skillLevel">技能等级（1-5），每级提升 UpgradeEffectIncrease 倍率</param>
        /// <returns>最终技能伤害值，不小于1</returns>
        public static float CalculateSkillDamage(float baseAtn, float damageMultiplier, int skillLevel)
        {
            return RuleService.CalculateSkillDamage(
                baseAtn,
                damageMultiplier,
                skillLevel,
                SkillConstant.UpgradeEffectIncrease);
        }

        /// <summary>
        /// 计算技能当前等级的实际冷却时间。
        /// </summary>
        /// <param name="baseCooldown">技能基础冷却时间（秒）</param>
        /// <param name="skillLevel">技能等级（1-5），每级缩减 UpgradeCooldownReduction 比例</param>
        /// <returns>最终冷却时间（秒），不小于0.5秒</returns>
        public static float CalculateSkillCooldown(float baseCooldown, int skillLevel)
        {
            return RuleService.CalculateSkillCooldown(
                baseCooldown,
                skillLevel,
                SkillConstant.UpgradeCooldownReduction);
        }

        /// <summary>
        /// 格式化冷却剩余时间为可读字符串。
        /// </summary>
        /// <param name="remainingSeconds">剩余冷却秒数</param>
        /// <returns>格式化文本，如 "3.2s"；冷却完成返回空字符串</returns>
        public static string FormatCooldownRemaining(float remainingSeconds)
        {
            if (remainingSeconds <= 0f)
            {
                return string.Empty;
            }

            return remainingSeconds < 1.0f
                ? $"{remainingSeconds:F1}s"
                : $"{RuleService.ToCooldownDisplaySeconds(remainingSeconds)}s";
        }

        /// <summary>
        /// 检查玩家当前法力是否足够施放技能。
        /// </summary>
        /// <param name="currentMp">玩家当前法力值</param>
        /// <param name="manaCost">技能法力消耗</param>
        /// <returns>法力充足返回 true</returns>
        public static bool HasEnoughMana(int currentMp, int manaCost)
        {
            return RuleService.HasEnoughMana(currentMp, manaCost);
        }

        /// <summary>
        /// 获取技能升级所需经验点数。
        /// </summary>
        /// <param name="currentLevel">当前技能等级（1-4），5级已达到上限</param>
        /// <returns>升级所需经验点数；已满级返回-1</returns>
        public static int GetUpgradeCost(int currentLevel)
        {
            return RuleService.GetUpgradeCost(currentLevel);
        }

        /// <summary>
        /// 查询玩家周围指定半径内所有存活敌人。
        /// 用于 SelfAOE 类型技能的伤害目标选取。
        /// 遍历 EnemyManager 的 Characters 列表，过滤 null 引用和死亡敌人。
        /// </summary>
        /// <param name="center">AOE 中心世界坐标（通常为玩家位置）</param>
        /// <param name="radius">AOE 半径（世界单位）</param>
        /// <returns>半径内的存活敌人列表，无敌人时返回空列表</returns>
        public static List<AEnemy> GetEnemiesInRadius(Vector3 center, float radius)
        {
            List<AEnemy> result = new List<AEnemy>();
            if (Core.ServiceLocator.Get<EnemyManager>() == null)
            {
                return result;
            }

            float radiusSqr = radius * radius;
            foreach (AEnemy enemy in Core.ServiceLocator.Get<EnemyManager>().Characters)
            {
                if (enemy == null || enemy.CharacterDataLAB == null || enemy.CharacterDataLAB.Hp <= 0)
                {
                    continue;
                }

                if ((enemy.transform.position - center).sqrMagnitude <= radiusSqr)
                {
                    result.Add(enemy);
                }
            }

            return result;
        }

        /// <summary>
        /// 计算技能 Buff 的实际倍率（基于技能等级）。
        /// 用于 SelfBuff 类型技能的效果计算。
        /// </summary>
        /// <param name="baseMultiplier">技能基础倍率</param>
        /// <param name="skillLevel">技能等级（1-5）</param>
        /// <returns>等级加成后的倍率</returns>
        public static float CalculateBuffMultiplier(float baseMultiplier, int skillLevel)
        {
            return RuleService.CalculateBuffMultiplier(
                baseMultiplier,
                skillLevel,
                SkillConstant.UpgradeEffectIncrease);
        }

        /// <summary>
        /// 计算技能治疗量（基于技能等级）。
        /// 用于 SelfHeal 类型技能。
        /// </summary>
        /// <param name="baseHealAmount">基础治疗量</param>
        /// <param name="skillLevel">技能等级（1-5）</param>
        /// <returns>等级加成后的治疗量</returns>
        public static float CalculateHealAmount(float baseHealAmount, int skillLevel)
        {
            return RuleService.CalculateHealAmount(
                baseHealAmount,
                skillLevel,
                SkillConstant.UpgradeEffectIncrease);
        }

        /// <summary>
        /// 获取技能的快捷键显示文本。
        /// </summary>
        /// <param name="skillSlotIndex">技能槽位索引（0-3）</param>
        /// <returns>快捷键文本，如 "Q"、"E"、"R"、"F"</returns>
        public static string GetHotkeyDisplayText(int skillSlotIndex)
        {
            KeyCode[] hotkeys = { InputKeyConstant.SkillHotkey1, InputKeyConstant.SkillHotkey2, InputKeyConstant.SkillHotkey3, InputKeyConstant.SkillHotkey4 };
            if (skillSlotIndex < 0 || skillSlotIndex >= hotkeys.Length)
            {
                return "?";
            }

            return hotkeys[skillSlotIndex].ToString().Replace("Alpha", string.Empty);
        }

        /// <summary>
        /// 获取冷却进度比例（0-1），用于UI冷却覆盖层高度计算。
        /// </summary>
        /// <param name="remainingSeconds">剩余冷却秒数</param>
        /// <param name="totalCooldown">总冷却秒数</param>
        /// <returns>冷却进度比例，0=冷却完成，1=刚进入冷却</returns>
        public static float GetCooldownProgress(float remainingSeconds, float totalCooldown)
        {
            if (totalCooldown <= 0f || remainingSeconds <= 0f)
            {
                return 0f;
            }

            return RuleService.GetCooldownProgress(remainingSeconds, totalCooldown);
        }
    }
}
