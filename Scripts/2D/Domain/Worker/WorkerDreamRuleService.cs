namespace LAB2D.Domain.Worker
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Worker 执念（个人梦想）纯规则服务 — 诞生 / 热情消退 / 追求强化 / 目标映射。
    /// 执念是超越现有 4 类固定目标（WorkerGoal）的个人梦想，玩家不可直接控制；
    /// 由正/中人生事件概率催生，热情随日消退，追求时回补。零 Unity 依赖，可单测。
    /// </summary>
    public static class WorkerDreamRuleService
    {
        /// <summary>正/中人生事件催生执念的概率。</summary>
        public const float BirthChance = 0.2f;

        /// <summary>每次追求执念的热情回补（持续追求让执念更坚定，封顶 100）。</summary>
        public const float PursuePassionGain = 2f;

        /// <summary>初始热情。</summary>
        public const float InitialPassion = 70f;

        /// <summary>执念类型池（不含 None）。</summary>
        private static readonly WorkerDreamType[] DreamTypes =
        {
            WorkerDreamType.BuildBigHome,
            WorkerDreamType.BecomeRich,
            WorkerDreamType.GainRespect,
            WorkerDreamType.ProtectColony,
            WorkerDreamType.WanderWorld,
            WorkerDreamType.MasterCraft,
            WorkerDreamType.FindFamily,
        };

        /// <summary>
        /// 尝试催生一个执念（正/中人生事件触发）。
        /// 条件：当前无 ActiveDream、历史上限未满、randomValue&lt;BirthChance。
        /// 类型由 randomValue 确定性派生（保持可单测）。
        /// </summary>
        /// <returns>是否成功催生。</returns>
        public static bool TryBirth(WorkerMindData mind, int day, float randomValue)
        {
            if (mind == null || !mind.ActiveDream.IsEmpty)
            {
                return false;
            }

            if (mind.DreamHistory.Count >= WorkerMindConstant.DreamHistoryCap)
            {
                return false;
            }

            if (randomValue < 0f || randomValue >= BirthChance)
            {
                return false;
            }

            int index = ((int)(randomValue * 1000)) % DreamTypes.Length;
            WorkerDreamType type = DreamTypes[index];
            mind.ActiveDream = new WorkerDream
            {
                Type = type,
                Description = GetDescription(type),
                BornDay = day,
                LastPursuedDay = day,
                Passion = InitialPassion,
            };
            return true;
        }

        /// <summary>执念是否可执行（非空且热情 ≥ DreamBornThreshold）。</summary>
        public static bool IsPursuable(WorkerDream dream)
        {
            return !dream.IsEmpty && dream.Passion >= WorkerMindConstant.DreamBornThreshold;
        }

        /// <summary>追求执念：刷新最近追求日 + 热情回补（持续追求强化执念）。</summary>
        public static void Pursue(WorkerMindData mind, int day)
        {
            if (mind == null || mind.ActiveDream.IsEmpty)
            {
                return;
            }

            WorkerDream d = mind.ActiveDream;
            d.LastPursuedDay = day;
            d.Passion = Math.Min(100f, d.Passion + PursuePassionGain);
            mind.ActiveDream = d;
        }

        /// <summary>逐日热情消退；热情耗尽则归档到 DreamHistory（上限内）并清空 ActiveDream。</summary>
        public static void Decay(WorkerMindData mind, int day)
        {
            if (mind == null || mind.ActiveDream.IsEmpty)
            {
                return;
            }

            WorkerDream d = mind.ActiveDream;
            d.Passion -= WorkerMindConstant.DreamPassionDecayPerDay;
            if (d.Passion <= 0f)
            {
                d.Passion = 0f;
                if (mind.DreamHistory.Count < WorkerMindConstant.DreamHistoryCap)
                {
                    mind.DreamHistory.Add(d);
                }

                mind.ActiveDream = WorkerDream.None;
            }
            else
            {
                mind.ActiveDream = d;
            }
        }

        /// <summary>
        /// 执念 → 可执行目标映射。BuildStructure/CraftEquipment 需要真实材料清单
        /// （物品 ID→数量），由调用方（WorkerBrain.RefreshGoal）解析后传入；其余类型忽略 materials。
        /// </summary>
        public static WorkerGoal MapToGoal(WorkerDream dream, Dictionary<int, int> materials)
        {
            switch (dream.Type)
            {
                case WorkerDreamType.BuildBigHome:
                    return WorkerGoal.BuildStructure("想盖一座大房子", materials);
                case WorkerDreamType.BecomeRich:
                    return WorkerGoal.EarnMoney();
                case WorkerDreamType.GainRespect:
                    return WorkerGoal.StockFood(5);
                case WorkerDreamType.ProtectColony:
                    return WorkerGoal.StockFood(5);
                case WorkerDreamType.WanderWorld:
                    return WorkerGoal.EarnMoney();
                case WorkerDreamType.MasterCraft:
                    return WorkerGoal.CraftEquipment("想成为手艺大师", materials);
                case WorkerDreamType.FindFamily:
                    return WorkerGoal.BuildStructure("想建一个家", materials);
                default:
                    return WorkerGoal.EarnMoney();
            }
        }

        /// <summary>执念类型的中文描述（气泡 / 日志 / 存档）。</summary>
        public static string GetDescription(WorkerDreamType type)
        {
            switch (type)
            {
                case WorkerDreamType.BuildBigHome: return "盖一座大房子";
                case WorkerDreamType.BecomeRich: return "成为最富有的人";
                case WorkerDreamType.GainRespect: return "得到大家的敬重";
                case WorkerDreamType.ProtectColony: return "守护家园";
                case WorkerDreamType.WanderWorld: return "走遍世界";
                case WorkerDreamType.MasterCraft: return "成为手艺大师";
                case WorkerDreamType.FindFamily: return "找到归属和家人";
                default: return string.Empty;
            }
        }
    }
}
