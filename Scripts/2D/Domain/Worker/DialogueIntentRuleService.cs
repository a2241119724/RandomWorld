namespace LAB2D.Domain.Worker
{
    using LAB2D.Constant;

    /// <summary>对话预设意图类型（M3 包2.4 LLM 对话结算）。</summary>
    [System.Serializable]
    public enum DialogueIntentKind
    {
        /// <summary>求教功法：境界高于玩家且好感达标的 Worker 传授心得，玩家灵气增长。</summary>
        TeachSeek,

        /// <summary>安抚：Worker 压力高时效果最佳（压力-15/士气回升），低压时弱效果。</summary>
        Comfort,

        /// <summary>道歉：有怨恨或好感不足时化解怨气，无事可歉时轻微回应。</summary>
        Apology,

        /// <summary>赠礼：花费玩家金币换好感与感恩。</summary>
        Gift,
    }

    /// <summary>意图结算输入（纯值，调用方从各 Manager 收集）。</summary>
    [System.Serializable]
    public class DialogueIntentInput
    {
        /// <summary>对话 Worker 对玩家的好感 [0,100]。</summary>
        public float Favorability;

        /// <summary>对玩家的怨恨 [0,100]。</summary>
        public float Resentment;

        /// <summary>Worker 压力比 CurStress/MaxStress [0,1]。</summary>
        public float NpcStressRatio;

        /// <summary>Worker 境界索引。</summary>
        public int NpcRealmIndex;

        /// <summary>玩家境界索引。</summary>
        public int PlayerRealmIndex;

        /// <summary>该意图今日已用次数。</summary>
        public int UsedCountToday;

        /// <summary>玩家金币是否够赠礼花费。</summary>
        public bool PlayerCoinsEnough;

        /// <summary>对话 Worker 名（拼 PlayerActionText 用）。</summary>
        public string NpcName;
    }

    /// <summary>意图结算结果（确定性数值变化 + 展示文本）。</summary>
    [System.Serializable]
    public class DialogueIntentResult
    {
        /// <summary>是否可用（日限未到/金币足够）。不可用时不产生任何效果。</summary>
        public bool Available;

        /// <summary>是否成功生效（婉拒也算 Available，只是无主要收益）。</summary>
        public bool Success;

        /// <summary>结算分支键（日志/UI 调试用）。</summary>
        public string OutcomeKey;

        /// <summary>好感变化（FavorabilityManager.ModifyWithPlayer）。</summary>
        public float FavorDelta;

        /// <summary>怨恨变化（负数=化解）。</summary>
        public float ResentDelta;

        /// <summary>感恩变化。</summary>
        public float GratitudeDelta;

        /// <summary>对玩家信任变化（信念轴）。</summary>
        public float TrustDelta;

        /// <summary>Worker 压力变化（负数=减压）。</summary>
        public float StressDelta;

        /// <summary>Worker 士气变化。</summary>
        public float MoraleDelta;

        /// <summary>玩家灵气增长（0=无）。</summary>
        public float PlayerQiGain;

        /// <summary>玩家金币花费（0=无）。</summary>
        public int CoinCost;

        /// <summary>事件记忆强度（0=不记事件）。</summary>
        public float EventIntensity;

        /// <summary>事件类型键（WorkerMindConstant.EVT_*，空=不记）。</summary>
        public string EventKey;

        /// <summary>发给 LLM 的玩家消息（结算描述 + 角色扮演引导）。</summary>
        public string PlayerActionText;

        /// <summary>玩家气泡显示的短句（无数值细节、无角色扮演引导）。</summary>
        public string PlayerDisplayText;

        /// <summary>LLM 不可用时的兜底台词（Worker 头顶气泡）。</summary>
        public string FallbackReply;
    }

    /// <summary>
    /// 对话预设意图确定性结算规则（M3 包2.4）——零 Unity 依赖，可在 Editor 单测中直接使用。
    /// 设计原则：数值结算 100% 本地纯规则；LLM 仅根据 PlayerActionText 增强 NPC 回复措辞。
    /// </summary>
    public static class DialogueIntentRuleService
    {
        // ---- 求教功法 ----
        /// <summary>求教功法好感门控。</summary>
        public const float TeachFavorabilityThreshold = 60f;

        /// <summary>求教功法日限。</summary>
        public const int TeachDailyCap = 1;

        /// <summary>求教成功基础灵气收益（另加好感加成）。</summary>
        public const float TeachQiBase = 25f;

        /// <summary>求教成功灵气好感系数。</summary>
        public const float TeachQiPerFavor = 0.15f;

        // ---- 安抚 ----
        /// <summary>安抚高压阈值（压力比超过此值全额效果）。</summary>
        public const float ComfortHighStressRatio = 0.6f;

        /// <summary>安抚日限。</summary>
        public const int ComfortDailyCap = 1;

        // ---- 道歉 ----
        /// <summary>道歉有效好感线（低于此值道歉全额效果）。</summary>
        public const float ApologyLowFavorLine = 50f;

        /// <summary>道歉日限。</summary>
        public const int ApologyDailyCap = 1;

        // ---- 赠礼 ----
        /// <summary>赠礼金币花费。</summary>
        public const int GiftCoinCost = 20;

        /// <summary>赠礼日限。</summary>
        public const int GiftDailyCap = 2;

        /// <summary>各意图日限查表。</summary>
        public static int GetDailyCap(DialogueIntentKind kind)
        {
            switch (kind)
            {
                case DialogueIntentKind.TeachSeek: return TeachDailyCap;
                case DialogueIntentKind.Comfort: return ComfortDailyCap;
                case DialogueIntentKind.Apology: return ApologyDailyCap;
                case DialogueIntentKind.Gift: return GiftDailyCap;
                default: return 0;
            }
        }

        /// <summary>意图显示名（日志/reason 用）。</summary>
        public static string GetDisplayName(DialogueIntentKind kind)
        {
            switch (kind)
            {
                case DialogueIntentKind.TeachSeek: return "求教功法";
                case DialogueIntentKind.Comfort: return "安抚";
                case DialogueIntentKind.Apology: return "道歉";
                case DialogueIntentKind.Gift: return "赠礼";
                default: return kind.ToString();
            }
        }

        /// <summary>意图的事件类型键（与 WorkerMindConstant.EVT_* 对应）。</summary>
        public static string GetEventKey(DialogueIntentKind kind)
        {
            switch (kind)
            {
                case DialogueIntentKind.TeachSeek: return WorkerMindConstant.EVT_TEACH_SEEK;
                case DialogueIntentKind.Comfort: return WorkerMindConstant.EVT_COMFORTED;
                case DialogueIntentKind.Apology: return WorkerMindConstant.EVT_APOLOGY;
                case DialogueIntentKind.Gift: return WorkerMindConstant.EVT_GIFT_PLAYER;
                default: return string.Empty;
            }
        }

        /// <summary>
        /// 确定性结算入口。不可用（日限/金币不足）返回 Available=false 且全零副作用。
        /// </summary>
        public static DialogueIntentResult Evaluate(DialogueIntentKind kind, DialogueIntentInput input)
        {
            DialogueIntentResult result = new DialogueIntentResult();

            if (input == null)
            {
                result.OutcomeKey = "invalid_input";
                return result;
            }

            int cap = GetDailyCap(kind);
            if (input.UsedCountToday >= cap)
            {
                result.OutcomeKey = "daily_cap";
                result.FallbackReply = "（今日已不可再" + GetDisplayName(kind) + "）";
                return result;
            }

            if (kind == DialogueIntentKind.Gift && !input.PlayerCoinsEnough)
            {
                result.OutcomeKey = "no_coins";
                result.FallbackReply = "（金币不足）";
                return result;
            }

            switch (kind)
            {
                case DialogueIntentKind.TeachSeek:
                    EvaluateTeach(input, result);
                    break;
                case DialogueIntentKind.Comfort:
                    EvaluateComfort(input, result);
                    break;
                case DialogueIntentKind.Apology:
                    EvaluateApology(input, result);
                    break;
                case DialogueIntentKind.Gift:
                    EvaluateGift(input, result);
                    break;
            }

            result.Available = true;
            result.EventKey = result.EventIntensity > 0f ? GetEventKey(kind) : string.Empty;
            return result;
        }

        private static void EvaluateTeach(DialogueIntentInput input, DialogueIntentResult result)
        {
            string npc = input.NpcName;

            if (input.NpcRealmIndex <= input.PlayerRealmIndex)
            {
                result.OutcomeKey = "teach_refused_realm";
                result.Success = false;
                result.PlayerActionText =
                    $"（你向{npc}请教修炼功法，但{npc}的修为并不比你高深，笑着说不如一起切磋。请以{npc}的身份自然回应，不要复述括号里的系统信息。）";
                result.PlayerDisplayText = $"你向{npc}请教功法，{npc}笑说修为相仿，不如切磋。";
                result.FallbackReply = "你我修为相仿，不如互相切磋切磋。";
                return;
            }

            if (input.Favorability < TeachFavorabilityThreshold)
            {
                result.OutcomeKey = "teach_refused_favor";
                result.Success = false;
                result.PlayerActionText =
                    $"（你向{npc}请教修炼功法，但你们交情尚浅，{npc}不欲轻易外传，婉言谢绝了。请以{npc}的身份自然回应，不要复述括号里的系统信息。）";
                result.PlayerDisplayText = $"你向{npc}请教功法，但交情尚浅，被婉拒了。";
                result.FallbackReply = "功法传承非同小可，等你我更熟络些再说吧。";
                return;
            }

            result.Success = true;
            result.OutcomeKey = "taught";
            result.PlayerQiGain = TeachQiBase + input.Favorability * TeachQiPerFavor;
            result.FavorDelta = 3f;
            result.GratitudeDelta = 5f;
            result.EventIntensity = 45f;
            result.PlayerActionText =
                $"（你虚心向{npc}请教修炼功法，{npc}见你诚心，指点了一段吐纳心得，你只觉灵气有所增长。{npc}对你好感+3。请以{npc}的身份自然回应，不要复述括号里的系统信息。）";
            result.PlayerDisplayText = $"你虚心向{npc}请教功法，得了一段吐纳心得，灵气有所增长。";
            result.FallbackReply = "修行贵在坚持，这点心得就当抛砖引玉吧。";
        }

        private static void EvaluateComfort(DialogueIntentInput input, DialogueIntentResult result)
        {
            string npc = input.NpcName;
            bool highStress = input.NpcStressRatio > ComfortHighStressRatio;

            result.Success = true;
            result.OutcomeKey = highStress ? "comforted_high" : "comforted_low";
            result.EventIntensity = highStress ? 40f : 15f;

            if (highStress)
            {
                result.StressDelta = -15f;
                result.MoraleDelta = 5f;
                result.FavorDelta = 3f;
                result.TrustDelta = 3f;
                result.PlayerActionText =
                    $"（你注意到{npc}近来压力很大，好言安抚了几句。{npc}紧绷的神色缓和了下来，压力-15、士气+5，对你好感+3。请以{npc}的身份自然回应，不要复述括号里的系统信息。）";
                result.PlayerDisplayText = $"你好言安抚了压力很大的{npc}，{npc}的神色缓和了许多。";
                result.FallbackReply = "听你这么一说，心里松快多了。";
            }
            else
            {
                result.StressDelta = -5f;
                result.FavorDelta = 1f;
                result.PlayerActionText =
                    $"（你和{npc}聊了聊近况，顺便宽慰了几句。{npc}心情不错，对你好感+1。请以{npc}的身份自然回应，不要复述括号里的系统信息。）";
                result.PlayerDisplayText = $"你和{npc}聊了聊近况，顺便宽慰了几句。";
                result.FallbackReply = "我没事，多谢挂心。";
            }
        }

        private static void EvaluateApology(DialogueIntentInput input, DialogueIntentResult result)
        {
            string npc = input.NpcName;
            bool heavy = input.Resentment > 0f;
            bool medium = !heavy && input.Favorability < ApologyLowFavorLine;

            result.Success = true;

            if (heavy || medium)
            {
                result.OutcomeKey = heavy ? "apology_full_resent" : "apology_full_favor";
                result.ResentDelta = -(8f + input.Favorability * 0.06f);
                result.FavorDelta = 4f;
                result.TrustDelta = 5f;
                result.EventIntensity = 35f;
                string cause = heavy ? "之前积累的怨气" : "之前的生分";
                result.PlayerActionText =
                    $"（你为{cause}郑重向{npc}道歉。{npc}神色松动，怨气消解了不少，对你好感+4。请以{npc}的身份自然回应，不要复述括号里的系统信息。）";
                result.PlayerDisplayText = $"你郑重向{npc}道歉，{npc}的怨气消解了不少。";
                result.FallbackReply = "过去的事就让它过去吧。";
            }
            else
            {
                result.OutcomeKey = "apology_light";
                result.FavorDelta = 1f;
                result.PlayerActionText =
                    $"（你向{npc}道歉，但{npc}并不觉得你做过什么亏心事，反而有些不好意思，对你好感+1。请以{npc}的身份自然回应，不要复述括号里的系统信息。）";
                result.PlayerDisplayText = $"你向{npc}道歉，{npc}反倒觉得有些不好意思。";
                result.FallbackReply = "你何错之有？莫要折煞我了。";
            }
        }

        private static void EvaluateGift(DialogueIntentInput input, DialogueIntentResult result)
        {
            string npc = input.NpcName;

            result.Success = true;
            result.OutcomeKey = "gift_done";
            result.CoinCost = GiftCoinCost;
            result.FavorDelta = 8f;
            result.GratitudeDelta = 10f;
            result.EventIntensity = 50f;
            result.PlayerActionText =
                $"（你送给{npc} {GiftCoinCost}金币作为心意。{npc}十分感动，对你好感+8。请以{npc}的身份自然回应，不要复述括号里的系统信息。）";
            result.PlayerDisplayText = $"你送给{npc} {GiftCoinCost}金币作为心意，{npc}十分感动。";
            result.FallbackReply = "这怎么好意思……那我就收下了，多谢！";
        }

        /// <summary>意图顺序表（UI 按钮构建用）。</summary>
        public static readonly DialogueIntentKind[] AllKinds =
        {
            DialogueIntentKind.TeachSeek,
            DialogueIntentKind.Comfort,
            DialogueIntentKind.Apology,
            DialogueIntentKind.Gift,
        };

        /// <summary>clamp [min,max]（零 Unity 依赖）。</summary>
        public static float Clamp(float v, float min, float max)
        {
            return v < min ? min : (v > max ? max : v);
        }
    }
}
