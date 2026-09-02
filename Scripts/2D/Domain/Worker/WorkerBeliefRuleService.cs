namespace LAB2D.Domain.Worker
{
    /// <summary>单次事件的信念增量（四维，各自 clamp 到 [0,100]）。</summary>
    public struct BeliefDelta
    {
        public float TrustInWorld;
        public float TrustInPlayer;
        public float SelfEsteem;
        public float SenseOfBelonging;

        public bool IsZero => this.TrustInWorld == 0f && this.TrustInPlayer == 0f
            && this.SelfEsteem == 0f && this.SenseOfBelonging == 0f;

        public static BeliefDelta Zero => default;
    }

    /// <summary>
    /// Worker 信念演化纯规则服务 — 事件类型 → 四维信念增量，按强度线性缩放。
    /// 零 Unity 依赖，可单测。基准增量为 intensity=100 时的最大幅度，按 intensity/100 缩放。
    /// </summary>
    public static class WorkerBeliefRuleService
    {
        /// <summary>按事件类型计算信念增量（intensity∈[0,100]，自动 clamp）。</summary>
        public static BeliefDelta GetDelta(string typeKey, float intensity)
        {
            float scale = ClampIntensity(intensity) / 100f;

            BeliefDelta d = GetBaseDelta(typeKey);
            d.TrustInWorld *= scale;
            d.TrustInPlayer *= scale;
            d.SelfEsteem *= scale;
            d.SenseOfBelonging *= scale;
            return d;
        }

        /// <summary>将增量应用到 Mind，各信念 clamp 到 [0,100]。</summary>
        public static void Apply(WorkerMindData mind, BeliefDelta delta)
        {
            if (mind == null || delta.IsZero)
            {
                return;
            }

            mind.TrustInWorld = ClampBelief(mind.TrustInWorld + delta.TrustInWorld);
            mind.TrustInPlayer = ClampBelief(mind.TrustInPlayer + delta.TrustInPlayer);
            mind.SelfEsteem = ClampBelief(mind.SelfEsteem + delta.SelfEsteem);
            mind.SenseOfBelonging = ClampBelief(mind.SenseOfBelonging + delta.SenseOfBelonging);
        }

        private static BeliefDelta GetBaseDelta(string typeKey)
        {
            switch (typeKey)
            {
                case WorkerMindConstant.EVT_PLAYER_HELP:
                    return D(trustWorld: 3f, trustPlayer: 8f, belonging: 2f);
                case WorkerMindConstant.EVT_PLAYER_ATTACK:
                    return D(trustWorld: -5f, trustPlayer: -10f);
                case WorkerMindConstant.EVT_PLAYER_KILL:
                    return D(trustWorld: -12f, trustPlayer: -20f, esteem: -5f);
                case WorkerMindConstant.EVT_WORKER_ATTACK:
                    return D(trustWorld: -6f);
                case WorkerMindConstant.EVT_BOUNTY_COMPLETED:
                    return D(trustWorld: 2f, esteem: 8f);
                case WorkerMindConstant.EVT_BOUNTY_ACCEPTED:
                    return D(trustPlayer: 2f, esteem: 2f);
                case WorkerMindConstant.EVT_BOUNTY_REFUSED:
                    return D(trustPlayer: -2f, esteem: -2f);
                case WorkerMindConstant.EVT_TRADE_SUCCESS:
                    return D(trustWorld: 2f, belonging: 1f);
                case WorkerMindConstant.EVT_TRADE_REJECTED:
                    return D(trustWorld: -4f);
                case WorkerMindConstant.EVT_CONVERSATION:
                    return D(trustWorld: 2f, belonging: 4f);
                case WorkerMindConstant.EVT_TASK_COMPLETED:
                    return D(esteem: 4f);
                case WorkerMindConstant.EVT_STAGE_UP:
                    return D(esteem: 6f, belonging: 4f);
                case WorkerMindConstant.EVT_NEAR_DEATH:
                    return D(trustWorld: -10f, esteem: -6f);
                case WorkerMindConstant.EVT_GROUND_SLEEP:
                    return D(esteem: -2f);
                case WorkerMindConstant.EVT_FOUND_ITEM:
                    return D(esteem: 2f);
                case WorkerMindConstant.EVT_WIND_FALL:
                    return D(trustWorld: 4f, esteem: 6f);
                case WorkerMindConstant.EVT_ILLNESS:
                    return D(trustWorld: -6f);
                case WorkerMindConstant.EVT_INSIGHT:
                    return D(trustWorld: 4f, esteem: 4f);
                case WorkerMindConstant.EVT_MISFORTUNE:
                    return D(trustWorld: -8f, esteem: -6f);
                case WorkerMindConstant.EVT_ENLIGHTENMENT:
                    return D(trustWorld: 6f, trustPlayer: 4f, esteem: 6f, belonging: 6f);
                case WorkerMindConstant.EVT_SMALL_JOY:
                    return D(trustWorld: 1f, esteem: 2f, belonging: 2f);
                case WorkerMindConstant.EVT_NIGHTMARE:
                    return D(trustWorld: -3f, esteem: -2f);
                case WorkerMindConstant.EVT_CULTIVATION_BREAKTHROUGH:
                    return D(esteem: 8f, belonging: 4f);
                case WorkerMindConstant.EVT_POWER_AWAKEN:
                    return D(trustWorld: 3f, esteem: 10f);
                case WorkerMindConstant.EVT_FELLOW_BREAKTHROUGH:
                    return D(belonging: 2f);
                case WorkerMindConstant.EVT_FELLOW_BREAKTHROUGH_ENVY:
                    return D(esteem: -3f, belonging: -2f);
                default:
                    return BeliefDelta.Zero;
            }
        }

        private static BeliefDelta D(
            float trustWorld = 0f, float trustPlayer = 0f,
            float esteem = 0f, float belonging = 0f)
        {
            return new BeliefDelta
            {
                TrustInWorld = trustWorld,
                TrustInPlayer = trustPlayer,
                SelfEsteem = esteem,
                SenseOfBelonging = belonging,
            };
        }

        private static float ClampBelief(float v)
        {
            if (v < 0f) return 0f;
            if (v > 100f) return 100f;
            return v;
        }

        private static float ClampIntensity(float intensity)
        {
            if (intensity < 0f) return 0f;
            if (intensity > 100f) return 100f;
            return intensity;
        }
    }
}
