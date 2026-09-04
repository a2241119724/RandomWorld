namespace LAB2D.Domain.Gameplay
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 每局修饰符效果通道 — 在对应系统的既有乘数链上再叠一层「按局」系数（docs/游戏大纲.md 包 4「每局不一样」）。
    /// 与事件天气正交：各自独立叠乘，互不感知。
    /// </summary>
    public enum ModifierChannel
    {
        /// <summary>灵气浓度（LingQiManager 合成乘数，影响修炼/灵气产出）。</summary>
        LingQiRecovery = 0,

        /// <summary>敌方波次强度（数量与难度同时缩放）。</summary>
        EnemyStrength = 1,

        /// <summary>Worker 任务进度（天气乘数同款叠乘点）。</summary>
        WorkerWorkSpeed = 2,

        /// <summary>敌方掉落概率（通用物品 + 装备两处 roll）。</summary>
        EnemyLoot = 3,
    }

    /// <summary>单个修饰符定义（不可变数据，无行为）。</summary>
    public class SessionModifierDefinition
    {
        /// <summary>稳定标识（存档持久化用，禁止改动已发布 id）。</summary>
        public string Id;

        /// <summary>中文名（HUD/开局 Tip 展示）。</summary>
        public string Name;

        /// <summary>一句风味描述（HUD 展示）。</summary>
        public string Description;

        /// <summary>主效果通道。</summary>
        public ModifierChannel Channel;

        /// <summary>主通道乘数（1.15~1.30 / 0.75~0.85 档位，可感知不破坏平衡）。</summary>
        public float Multiplier;

        /// <summary>补偿通道（敌方强化类配对收益用，如妖兽凶猛+战利品），无则 null。</summary>
        public ModifierChannel? BonusChannel;

        /// <summary>补偿通道乘数。</summary>
        public float BonusMultiplier = 1f;
    }

    /// <summary>
    /// 每局修饰符规则服务（Domain 纯函数，零 Unity 依赖）。
    /// 修饰符池 + 确定性 Roll + 通道乘数合成；宿主为 Gameplay.SessionModifierManager（存档/Tip）。
    /// </summary>
    public static class SessionModifierRuleService
    {
        /// <summary>
        /// 首版修饰符池（8 个，4 通道各 2）。敌方强化自带战利品补偿，避免纯负面体验。
        /// </summary>
        public static readonly SessionModifierDefinition[] Pool =
        {
            new SessionModifierDefinition
            {
                Id = "lingqi_surge", Name = "灵气潮汐", Description = "天地灵气如潮涌动，吐纳修炼事半功倍。",
                Channel = ModifierChannel.LingQiRecovery, Multiplier = 1.25f,
            },
            new SessionModifierDefinition
            {
                Id = "lingqi_dry", Name = "灵气枯潮", Description = "灵气沉寂，大地吐纳迟缓。",
                Channel = ModifierChannel.LingQiRecovery, Multiplier = 0.80f,
            },
            new SessionModifierDefinition
            {
                Id = "enemy_ferocious", Name = "妖兽凶猛", Description = "来袭妖兽格外凶悍，但浑身是宝。",
                Channel = ModifierChannel.EnemyStrength, Multiplier = 1.25f,
                BonusChannel = ModifierChannel.EnemyLoot, BonusMultiplier = 1.40f,
            },
            new SessionModifierDefinition
            {
                Id = "enemy_weak", Name = "妖兽羸弱", Description = "今夜妖兽萎靡不振，攻势疲软。",
                Channel = ModifierChannel.EnemyStrength, Multiplier = 0.80f,
            },
            new SessionModifierDefinition
            {
                Id = "worker_diligent", Name = "工匠勤勉", Description = "Workers 干劲十足，干活麻利。",
                Channel = ModifierChannel.WorkerWorkSpeed, Multiplier = 1.20f,
            },
            new SessionModifierDefinition
            {
                Id = "worker_lazy", Name = "工匠倦怠", Description = "Workers 提不起劲，干活拖沓。",
                Channel = ModifierChannel.WorkerWorkSpeed, Multiplier = 0.85f,
            },
            new SessionModifierDefinition
            {
                Id = "loot_rich", Name = "腰缠万贯", Description = "妖兽们不知为何都揣着家当。",
                Channel = ModifierChannel.EnemyLoot, Multiplier = 1.40f,
            },
            new SessionModifierDefinition
            {
                Id = "loot_poor", Name = "报酬微薄", Description = "妖兽们穷得叮当响。",
                Channel = ModifierChannel.EnemyLoot, Multiplier = 0.75f,
            },
        };

        /// <summary>按 id 查找定义，未知 id 返回 null（读档容错：未知条目按无效果处理）。</summary>
        public static SessionModifierDefinition GetById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (SessionModifierDefinition definition in Pool)
            {
                if (definition.Id == id)
                {
                    return definition;
                }
            }

            return null;
        }

        /// <summary>
        /// 确定性抽取：Fisher-Yates 洗牌取前 count 个（不重复），按池定义序输出保证同 seed 稳定。
        /// count 钳到 [0, Pool.Length]。
        /// </summary>
        /// <param name="seed">随机种子（每局开局由宿主生成并入档）。</param>
        /// <param name="count">抽取数量。</param>
        /// <returns>选中的修饰符 id 数组（可能为空）。</returns>
        public static string[] Roll(int seed, int count)
        {
            int safeCount = MathHelper.ClampMin(Math.Min(count, Pool.Length), 0);
            if (safeCount == 0)
            {
                return Array.Empty<string>();
            }

            // Fisher-Yates：只洗前 safeCount 个位置即可（部分洗牌），其余不动
            SessionModifierDefinition[] shuffled = (SessionModifierDefinition[])Pool.Clone();
            var random = new Random(seed);
            for (int i = 0; i < safeCount; i++)
            {
                int j = random.Next(i, Pool.Length);
                SessionModifierDefinition temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            string[] picked = new string[safeCount];
            for (int i = 0; i < safeCount; i++)
            {
                picked[i] = shuffled[i].Id;
            }

            // 洗牌结果已是确定性的；再按池序排序，令同 seed 输出与展示顺序都稳定
            Array.Sort(picked, PoolIndexComparer);
            return picked;
        }

        /// <summary>合成通道乘数：激活列表中所有命中该通道（含补偿通道）的乘数累乘，无命中为 1。</summary>
        public static float GetChannelMultiplier(IReadOnlyList<string> activeIds, ModifierChannel channel)
        {
            if (activeIds == null || activeIds.Count == 0)
            {
                return 1f;
            }

            float composed = 1f;
            foreach (string id in activeIds)
            {
                SessionModifierDefinition definition = GetById(id);
                if (definition == null)
                {
                    continue;
                }

                if (definition.Channel == channel)
                {
                    composed *= definition.Multiplier;
                }

                if (definition.BonusChannel == channel)
                {
                    composed *= definition.BonusMultiplier;
                }
            }

            return composed;
        }

        /// <summary>开局 Tip 摘要：「妖兽凶猛、工匠勤勉」。</summary>
        public static string FormatSummary(IReadOnlyList<string> activeIds)
        {
            if (activeIds == null || activeIds.Count == 0)
            {
                return "无";
            }

            var names = new List<string>(activeIds.Count);
            foreach (string id in activeIds)
            {
                SessionModifierDefinition definition = GetById(id);
                if (definition != null)
                {
                    names.Add(definition.Name);
                }
            }

            return string.Join("、", names);
        }

        /// <summary>HUD 单行：「妖兽凶猛 敌方×1.25 战利品×1.40 — 描述」。</summary>
        public static string FormatModifierLine(SessionModifierDefinition definition)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            string effect = $"{ChannelShortName(definition.Channel)}×{definition.Multiplier:0.00}";
            if (definition.BonusChannel.HasValue)
            {
                effect += $" {ChannelShortName(definition.BonusChannel.Value)}×{definition.BonusMultiplier:0.00}";
            }

            return $"{definition.Name} {effect} — {definition.Description}";
        }

        /// <summary>通道短名（HUD 展示用）。</summary>
        public static string ChannelShortName(ModifierChannel channel)
        {
            switch (channel)
            {
                case ModifierChannel.LingQiRecovery: return "灵气";
                case ModifierChannel.EnemyStrength: return "敌方";
                case ModifierChannel.WorkerWorkSpeed: return "工作";
                case ModifierChannel.EnemyLoot: return "战利品";
                default: return channel.ToString();
            }
        }

        /// <summary>Array.Sort 比较器：按定义在池中的序号排（Roll 输出稳定化）。</summary>
        private static int PoolIndexComparer(string x, string y)
        {
            return Array.IndexOf(Pool, GetById(x)) - Array.IndexOf(Pool, GetById(y));
        }
    }
}
