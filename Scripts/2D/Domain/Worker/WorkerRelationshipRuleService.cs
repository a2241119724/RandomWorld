namespace LAB2D.Domain.Worker
{
    using LAB2D.Character.Worker;
    using System;

    /// <summary>
    /// Worker 自发关系系统纯规则服务 — 数据在 Mind.Relations（name→WorkerRelationEntry）。
    /// 关系是低频定性状态，独立于 FavorabilityManager（高频数值好感度）：
    /// 亲密度 Affinity（-100..100）由互动累积决定友谊/敌意；记仇 GrudgeLevel 由被拒交易/被攻击触发；
    /// 爱慕 AdmirationLevel 由旁观高额悬赏触发。零 Unity 依赖，可单测。
    /// Kind 判定优先级：Grudge > Enmity > Admiration > Friendship > None。
    /// </summary>
    public static class WorkerRelationshipRuleService
    {
        /// <summary>按目标名查找关系条目，无则返回 null。</summary>
        public static WorkerRelationEntry Find(WorkerMindData mind, string targetName)
        {
            if (mind?.Relations == null || string.IsNullOrEmpty(targetName))
            {
                return null;
            }

            foreach (WorkerRelationEntry entry in mind.Relations)
            {
                if (entry.TargetName == targetName)
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>按目标名查找或创建关系条目。</summary>
        public static WorkerRelationEntry GetOrCreate(WorkerMindData mind, string targetName)
        {
            WorkerRelationEntry existing = Find(mind, targetName);
            if (existing != null)
            {
                return existing;
            }

            var entry = new WorkerRelationEntry { TargetName = targetName };
            mind.Relations.Add(entry);
            return entry;
        }

        /// <summary>
        /// 事件喂食（RecordEvent 对 Worker-Worker 事件调用）：按事件类型驱动亲密度/记仇/爱慕变化。
        /// 玩家（"PLAYER"）关系由好感度系统管理，调用方已过滤。
        /// </summary>
        /// <returns>是否发生关系等级（Kind）变化（调用方弹气泡）。</returns>
        public static bool Feed(WorkerMindData mind, string targetName, string typeKey, float intensity, int day)
        {
            switch (typeKey)
            {
                case WorkerMindConstant.EVT_TRADE_SUCCESS:
                    return ModifyAffinity(mind, targetName, intensity * WorkerMindConstant.RelationTradeSuccessAffinityScale, day);

                case WorkerMindConstant.EVT_BOUNTY_COMPLETED:
                    return ModifyAffinity(mind, targetName, WorkerMindConstant.RelationBountyCompleteAffinity, day);

                case WorkerMindConstant.EVT_BOUNTY_ACCEPTED:
                    return ModifyAffinity(mind, targetName, WorkerMindConstant.RelationBountyAcceptAffinity, day);

                case WorkerMindConstant.EVT_CONVERSATION:
                    return ModifyAffinity(mind, targetName, WorkerMindConstant.RelationConversationAffinity, day);

                case WorkerMindConstant.EVT_TRADE_REJECTED:
                    return AddGrudge(mind, targetName, WorkerMindConstant.RelationTradeRejectGrudge, day);

                case WorkerMindConstant.EVT_WORKER_ATTACK:
                    return AddGrudge(mind, targetName, WorkerMindConstant.RelationAttackGrudge, day);

                default:
                    return false;
            }
        }

        /// <summary>累积亲密度并重新判定 Kind。返回是否发生 Kind 变化。</summary>
        public static bool ModifyAffinity(WorkerMindData mind, string targetName, float delta, int day)
        {
            if (mind == null || string.IsNullOrEmpty(targetName))
            {
                return false;
            }

            WorkerRelationEntry entry = GetOrCreate(mind, targetName);
            RelationKind oldKind = entry.Kind;
            entry.Affinity = Clamp(entry.Affinity + delta,
                -WorkerMindConstant.RelationAffinityAbsCap, WorkerMindConstant.RelationAffinityAbsCap);
            entry.LastInteractionDay = day;
            entry.Kind = Classify(entry);
            return entry.Kind != oldKind;
        }

        /// <summary>记仇（被拒交易/被攻击）：GrudgeLevel 上升 + 亲密度惩罚。返回是否发生 Kind 变化。</summary>
        public static bool AddGrudge(WorkerMindData mind, string targetName, float amount, int day)
        {
            if (mind == null || string.IsNullOrEmpty(targetName))
            {
                return false;
            }

            WorkerRelationEntry entry = GetOrCreate(mind, targetName);
            RelationKind oldKind = entry.Kind;
            entry.GrudgeLevel = Clamp(entry.GrudgeLevel + amount, 0f, WorkerMindConstant.RelationLevelAbsCap);
            entry.Affinity = Clamp(entry.Affinity - WorkerMindConstant.RelationGrudgeAffinityPenalty,
                -WorkerMindConstant.RelationAffinityAbsCap, WorkerMindConstant.RelationAffinityAbsCap);
            entry.LastInteractionDay = day;
            entry.Kind = Classify(entry);
            return entry.Kind != oldKind;
        }

        /// <summary>爱慕（旁观高额悬赏，Sociality&gt;60 触发）：AdmirationLevel 上升 + 亲密度微升。返回是否发生 Kind 变化。</summary>
        public static bool AddAdmiration(WorkerMindData mind, string targetName, float amount, int day)
        {
            if (mind == null || string.IsNullOrEmpty(targetName))
            {
                return false;
            }

            WorkerRelationEntry entry = GetOrCreate(mind, targetName);
            RelationKind oldKind = entry.Kind;
            entry.AdmirationLevel = Clamp(entry.AdmirationLevel + amount, 0f, WorkerMindConstant.RelationLevelAbsCap);
            entry.Affinity = Clamp(entry.Affinity + (amount * 0.3f),
                -WorkerMindConstant.RelationAffinityAbsCap, WorkerMindConstant.RelationAffinityAbsCap);
            entry.LastInteractionDay = day;
            entry.Kind = Classify(entry);
            return entry.Kind != oldKind;
        }

        /// <summary>
        /// 每日维护（WorkerMindManager 游戏日切换调用）：记仇/爱慕衰减、亲密度向 0 回归；
        /// 全归零的关系条目移除。低频定性状态的自然缓和。
        /// </summary>
        public static void Decay(WorkerMindData mind, int day)
        {
            if (mind?.Relations == null)
            {
                return;
            }

            for (int i = mind.Relations.Count - 1; i >= 0; i--)
            {
                WorkerRelationEntry e = mind.Relations[i];
                if (e.GrudgeLevel > 0f)
                {
                    e.GrudgeLevel = Math.Max(0f, e.GrudgeLevel - WorkerMindConstant.RelationGrudgeDecayPerDay);
                }

                if (e.Affinity > 0f)
                {
                    e.Affinity = Math.Max(0f, e.Affinity - WorkerMindConstant.RelationAffinityDecayPerDay);
                }
                else if (e.Affinity < 0f)
                {
                    e.Affinity = Math.Min(0f, e.Affinity + WorkerMindConstant.RelationAffinityDecayPerDay);
                }

                if (e.AdmirationLevel > 0f)
                {
                    e.AdmirationLevel = Math.Max(0f, e.AdmirationLevel - WorkerMindConstant.RelationAdmirationDecayPerDay);
                }

                e.Kind = Classify(e);
                if (e.Affinity == 0f && e.GrudgeLevel == 0f && e.AdmirationLevel == 0f)
                {
                    mind.Relations.RemoveAt(i);
                }
            }
        }

        /// <summary>移除对某个目标的关系（死亡 Worker 清理）。返回是否移除成功。</summary>
        public static bool Remove(WorkerMindData mind, string deadName)
        {
            if (mind?.Relations == null || string.IsNullOrEmpty(deadName))
            {
                return false;
            }

            for (int i = mind.Relations.Count - 1; i >= 0; i--)
            {
                if (mind.Relations[i].TargetName == deadName)
                {
                    mind.Relations.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>互助判定：目标是否为朋友/爱慕对象（悬赏必接豁免、送礼候选）。</summary>
        public static bool WouldHelp(WorkerMindData mind, string targetName)
        {
            WorkerRelationEntry e = Find(mind, targetName);
            return e != null
                && (e.Kind == RelationKind.Friendship || e.Kind == RelationKind.Admiration);
        }

        /// <summary>回避判定：目标是否为敌意/记仇对象（拒其悬赏、拒卖）。</summary>
        public static bool WouldRefuse(WorkerMindData mind, string targetName)
        {
            WorkerRelationEntry e = Find(mind, targetName);
            return e != null
                && (e.Kind == RelationKind.Enmity || e.Kind == RelationKind.Grudge);
        }

        /// <summary>找一个可送礼目标（朋友/爱慕对象），无则返回 null。送礼用，节流由 LastInteractionDay 承担。</summary>
        public static WorkerRelationEntry FindGiftTarget(WorkerMindData mind)
        {
            if (mind?.Relations == null)
            {
                return null;
            }

            foreach (WorkerRelationEntry e in mind.Relations)
            {
                if (e.Kind == RelationKind.Friendship || e.Kind == RelationKind.Admiration)
                {
                    return e;
                }
            }

            return null;
        }

        /// <summary>Kind 判定优先级：Grudge &gt; Enmity &gt; Admiration &gt; Friendship &gt; None。</summary>
        private static RelationKind Classify(WorkerRelationEntry entry)
        {
            if (entry.GrudgeLevel > 0f)
            {
                return RelationKind.Grudge;
            }

            if (entry.Affinity <= WorkerMindConstant.RelationEnmityThreshold)
            {
                return RelationKind.Enmity;
            }

            if (entry.AdmirationLevel >= WorkerMindConstant.RelationAdmirationThreshold)
            {
                return RelationKind.Admiration;
            }

            if (entry.Affinity >= WorkerMindConstant.RelationFriendshipThreshold)
            {
                return RelationKind.Friendship;
            }

            return RelationKind.None;
        }

        private static float Clamp(float v, float min, float max)
        {
            return Math.Max(min, Math.Min(max, v));
        }
    }
}
