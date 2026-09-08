namespace LAB2D.Tool
{
    using LAB2D;
    using LAB2D.Character.Enemy;
    using LAB2D.Data;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay;
    using LAB2D.Map;
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
        /// 查询指定半径内所有存活敌人。
        /// 用于 SelfAOE 类型技能的伤害目标选取。
        /// 走 EnemyManager 空间网格（O(局部敌人数)，替代原全列表线性扫描）；
        /// filter 在查询时刻实时判活，与原逐个扫描语义一致。
        /// 返回新 List（调用方遍历中可能触发重入查询，勿改为共享缓冲）。
        /// </summary>
        /// <param name="center">查询中心世界坐标</param>
        /// <param name="radius">查询半径（世界单位）</param>
        /// <returns>半径内的存活敌人列表，无敌人时返回空列表</returns>
        public static List<AEnemy> GetEnemiesInRadius(Vector3 center, float radius)
        {
            List<AEnemy> result = new List<AEnemy>();
            if (!Core.ServiceLocator.TryGet(out EnemyManager enemyManager))
            {
                return result;
            }

            enemyManager.EnsureEnemyGridRebuilt();
            enemyManager.EnemyGrid.QueryRange(
                new GameVector2(center.x, center.y),
                radius,
                result,
                e => e != null && e.CharacterDataLAB != null && e.CharacterDataLAB.Hp > 0);
            return result;
        }

        /// <summary>
        /// 查询指定半径内最近的存活敌人（零 List 分配）。
        /// 防守索敌/箭塔开火等高频调用方使用。
        /// 注意：本查询是纯距离语义（不含视线遮挡），需要"看不见墙后敌人"的
        /// 探测方（Worker 索敌等）应再用 <see cref="HasLineOfSight"/> 过滤命中结果。
        /// </summary>
        /// <param name="center">查询中心世界坐标</param>
        /// <param name="radius">查询半径（世界单位）</param>
        /// <param name="sqrDistance">最近敌人的距离平方；无候选为 float.MaxValue</param>
        /// <returns>最近的存活敌人；半径内无存活敌人返回 null</returns>
        public static AEnemy GetNearestEnemyInRadius(Vector3 center, float radius, out float sqrDistance)
        {
            sqrDistance = float.MaxValue;
            if (!Core.ServiceLocator.TryGet(out EnemyManager enemyManager))
            {
                return null;
            }

            enemyManager.EnsureEnemyGridRebuilt();
            return enemyManager.EnemyGrid.QueryNearest(
                new GameVector2(center.x, center.y),
                radius,
                out sqrDistance,
                e => e != null && e.CharacterDataLAB != null && e.CharacterDataLAB.Hp > 0);
        }

        /// <summary>
        /// 网格级视线检查（Bresenham）— 遍历两点之间的格子，检测挡视线的建筑与地形。
        /// 建筑用视线专用查询（GetBuildData + SO IsPass，不论建没建完都挡——物理射线
        /// 检测不到建造中墙的无碰撞体格，必须查数据模型）；不复用寻路 IsCanReach：
        /// 它对建造中墙放行（未完工无碰撞体 + PosMap 分支要求 IsComplete），LOS 复用
        /// 会穿半成品墙 = Worker 隔墙索敌（2026-09-08 用户报告"检测穿墙"）。
        /// 从 AEnemy.HasGridLineOfSight 抽出共用（Enemy 感知 / Worker 索敌统一语义）。
        /// </summary>
        /// <param name="fromWorld">起点世界坐标（探测者）。</param>
        /// <param name="toWorld">终点世界坐标（目标）。</param>
        /// <returns>两点之间视线畅通（无墙/地形阻挡）返回 true。</returns>
        public static bool HasLineOfSight(Vector3 fromWorld, Vector3 toWorld)
        {
            TileMap tileMap = Core.ServiceLocator.Get<TileMap>();
            if (tileMap == null)
            {
                return true; // 无 TileMap 时不做网格检测，回退纯距离语义
            }

            BuildMap buildMap = Core.ServiceLocator.Get<BuildMap>();
            Vector3Int fromMap = tileMap.WorldPosToMapPos(fromWorld);
            Vector3Int toMap = tileMap.WorldPosToMapPos(toWorld);

            int x0 = fromMap.x;
            int y0 = fromMap.y;
            int x1 = toMap.x;
            int y1 = toMap.y;

            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int x = x0;
            int y = y0;
            int steps = 0;
            const int maxSteps = 200; // 安全上限，防止无限循环

            while (steps++ < maxSteps)
            {
                // 跳过起点和终点（起点是探测者自身，终点是目标）
                if ((x != x0 || y != y0) && (x != x1 || y != y1))
                {
                    Vector3Int checkPos = new Vector3Int(x, y, 0);

                    // BuildMap：格上有不可通行建筑即挡视线（含建造中——IsPass 在 SO 条目上，
                    // BuildTileData 无此字段须按名回查；PosMap 无条目=空地不挡）
                    if (buildMap != null)
                    {
                        BuildMap.BuildTileData buildData = buildMap.GetBuildData(checkPos);
                        if (buildData != null
                            && Core.ServiceLocator.TryGet(out ItemDataManager itemDataManager))
                        {
                            BuildItemData buildItemData = itemDataManager.GetBuildItemDataByName(buildData.Name);
                            if (buildItemData != null && !buildItemData.IsPass)
                            {
                                return false;
                            }
                        }
                    }

                    // TileMap：不可通过的地形瓦片阻挡视线
                    if (!tileMap.IsCanReach(checkPos))
                    {
                        return false;
                    }
                }

                if (x == x1 && y == y1)
                {
                    break;
                }

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }

            return true;
        }

        /// <summary>物理视线遮挡层（Tile/BuildTile 静态墙）——惰性初始化：LayerMask.GetMask
        /// 禁止在静态字段初始化器调用（实例化触发 .cctor 即抛 UnityException，见 bug-fixes.md 2026-09-07）。</summary>
        private static LayerMask senseOcclusionLayers;
        private static bool senseOcclusionLayersReady;

        /// <summary>
        /// 物理级视线遮挡（单条 Physics2D.Raycast 查静态墙层）：网格 LOS 的补强，覆盖
        /// 网格漏判的边角（Bresenham 对角缝隙正对物理碰撞体等）。注意对无碰撞体的
        /// 建造中墙无效（射线无物可撞）——未完工墙由 <see cref="HasLineOfSight"/> 的
        /// 数据模型查询覆盖，两层各管一半（对齐 AEnemy.SenseNearby 的双重 LOS）。
        /// </summary>
        /// <param name="fromWorld">起点世界坐标（探测者）。</param>
        /// <param name="toWorld">终点世界坐标（目标）。</param>
        /// <returns>true = 视线被物理碰撞体挡住。</returns>
        public static bool IsPhysicallyOccluded(Vector3 fromWorld, Vector3 toWorld)
        {
            if (!senseOcclusionLayersReady)
            {
                senseOcclusionLayers = LayerMask.GetMask("Tile", "BuildTile");
                senseOcclusionLayersReady = true;
            }

            Vector3 dir = toWorld - fromWorld;
            float dist = dir.magnitude;
            if (dist < 0.001f)
            {
                return false;
            }

            return Physics2D.Raycast(fromWorld, dir.normalized, dist, senseOcclusionLayers).collider != null;
        }

        /// <summary>
        /// 组合视线检查（网格 + 物理）：索敌入口统一用本方法——网格挡（含建造中墙）
        /// 或物理挡（碰撞体边角）均不可见。AEnemy.SenseNearby 保持自己的双重实现不变。
        /// </summary>
        public static bool HasClearSight(Vector3 fromWorld, Vector3 toWorld)
        {
            return HasLineOfSight(fromWorld, toWorld) && !IsPhysicallyOccluded(fromWorld, toWorld);
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
        /// <param name="skillSlotIndex">技能槽位索引（0-7）</param>
        /// <returns>快捷键文本，如 "Q"、"E"、"R"、"F"、"Z"、"X"、"C"、"V"</returns>
        public static string GetHotkeyDisplayText(int skillSlotIndex)
        {
            KeyCode[] hotkeys =
            {
                InputKeyConstant.SkillHotkey1,
                InputKeyConstant.SkillHotkey2,
                InputKeyConstant.SkillHotkey3,
                InputKeyConstant.SkillHotkey4,
                InputKeyConstant.SkillHotkey5,
                InputKeyConstant.SkillHotkey6,
                InputKeyConstant.SkillHotkey7,
                InputKeyConstant.SkillHotkey8,
            };
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
