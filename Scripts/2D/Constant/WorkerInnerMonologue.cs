namespace LAB2D.Constant
{
    using LAB2D.Enum;
    using UnityEngine;

    /// <summary>
    /// Worker 闲逛漫游/执行任务时的内心独白语料库。
    /// 按情境分类，支持按条件筛选后随机选取。
    /// </summary>
    public static class WorkerInnerMonologue
    {
        /// <summary>
        /// 通用漫游独白 — 任何时候都可能出现的想法。
        /// </summary>
        private static readonly string[] General = new[]
        {
            "今天天气不错，适合走走...",
            "这条路好像走过？",
            "嗯...待会儿该做点什么呢？",
            "有点无聊，四处逛逛吧。",
            "世界这么大，我想去看看。",
            "走一走，放松一下。",
            "这地方风景还行。",
            "偶尔摸鱼也挺好的。",
            "人生不止眼前的苟且，还有诗和远方。",
            "今天的云真好看。",
            "这儿的花开得不错。",
            "感觉自己充满了活力！",
            "走累了就歇会儿。",
            "四处转转总没坏处。",
            "也许会发现什么有趣的东西。",
        };

        /// <summary>
        /// 饥饿相关的独白 — Worker 饥饿值较低时出现。
        /// </summary>
        private static readonly string[] Hungry = new[]
        {
            "肚子有点饿了...",
            "好想吃顿大餐啊。",
            "饿得走不动了，得找点吃的。",
            "哪里有野果可以摘呢？",
            "不知道今晚能吃上什么。",
            "肚子咕咕叫...",
            "要是现在有块烤肉就好了。",
            "好饿...先找点食物吧。",
            "有什么能吃的吗？",
            "听说附近有苹果树？",
        };

        /// <summary>
        /// 疲劳相关的独白 — Worker 疲劳值较低时出现。
        /// </summary>
        private static readonly string[] Tired = new[]
        {
            "好累啊，想躺一会儿...",
            "腿都走酸了。",
            "要是现在有张床就好了。",
            "精力不太够了...",
            "该找个地方歇歇脚。",
            "困得眼皮打架...",
            "今晚一定要好好睡一觉。",
            "感觉身体被掏空...",
            "什么时候能回基地休息啊。",
            "好想我的小床...",
        };

        /// <summary>
        /// 采集/资源相关的独白。
        /// </summary>
        private static readonly string[] Gathering = new[]
        {
            "这棵树看起来能砍。",
            "那边好像有矿石？",
            "多囤点资源总没错。",
            "材料不够用啊，得再采点。",
            "手痒了，想砍点什么。",
            "资源就是力量！",
            "也许该去看看有没有新资源。",
            "这片森林资源真丰富。",
            "收集资源是生存的第一步。",
            "什么都能捡，说不定有用呢。",
        };

        /// <summary>
        /// 建造/家园相关的独白。
        /// </summary>
        private static readonly string[] Building = new[]
        {
            "啥时候能有个自己的小窝啊...",
            "房子还得再加固一下。",
            "想盖个大房子！",
            "家是最温暖的地方。",
            "围墙还得再加高一点。",
            "有个遮风挡雨的地方真好。",
            "要不要再扩建一下？",
            "家里还需要添置点家具。",
        };

        /// <summary>
        /// 社交/悬赏相关的独白。
        /// </summary>
        private static readonly string[] Social = new[]
        {
            "其他人都在忙什么呢？",
            "有没有悬赏任务可以接？",
            "一个人干活好孤单...",
            "要是有伙伴一起就好了。",
            "不知道队长在做什么。",
            "发个悬赏找人帮忙吧。",
            "团队合作效率最高。",
            "朋友们最近怎么样？",
        };

        /// <summary>
        /// 精气神低落时的独白 — 需要放松。
        /// </summary>
        private static readonly string[] SpiritLow = new[]
        {
            "心好累...想出去走走。",
            "漫无目的地逛逛也不错。",
            "换个环境转换心情。",
            "大自然是最好的疗愈。",
            "什么都不想，就这样走着。",
            "有时候，放空自己也很重要。",
            "远离喧嚣，享受片刻宁静。",
            "走一走，心情好多了。",
        };

        // ===== 任务相关独白 =====

        /// <summary>建造时的独白。</summary>
        private static readonly string[] TaskBuild = new[]
        {
            "搬砖ing...",
            "这墙得砌结实点。",
            "一砖一瓦，筑起家园。",
            "锤子在手，天下我有。",
            "这个角度得再对齐一点。",
            "盖房子是门手艺活。",
            "再加把劲，快完工了！",
        };

        /// <summary>采集时的独白。</summary>
        private static readonly string[] TaskGather = new[]
        {
            "砍砍砍！",
            "这棵树不错，木质很好。",
            "多囤点，冬天不愁。",
            "嘿咻嘿咻...",
            "这块矿石纯度挺高。",
            "采集是生存的基本功。",
            "手起刀落，资源到手。",
        };

        /// <summary>搬运时的独白。</summary>
        private static readonly string[] TaskCarry = new[]
        {
            "搬东西真累...",
            "好重啊，但得搬完。",
            "蚂蚁搬家，一趟一趟来。",
            "搬完这趟能歇会儿吗？",
            "物流是经济的命脉啊。",
            "加油，快到目的地了！",
        };

        /// <summary>吃饭时的独白。</summary>
        private static readonly string[] TaskEat = new[]
        {
            "终于能吃饭了！",
            "好香啊～",
            "人是铁，饭是钢。",
            "吃饱了才有力气干活。",
            "这一餐来得正是时候。",
        };

        /// <summary>锻炼时的独白。</summary>
        private static readonly string[] TaskExercise = new[]
        {
            "一二三四、二二三四...",
            "锻炼身体，建设家园。",
            "身体是革命的本钱。",
            "活动活动筋骨。",
            "保持好身材！",
        };

        /// <summary>种植时的独白。</summary>
        private static readonly string[] TaskPlant = new[]
        {
            "小苗苗快快长大～",
            "种瓜得瓜，种豆得豆。",
            "浇点水，晒晒太阳。",
            "农耕最治愈了。",
            "希望今年有个好收成。",
            "这片地土质不错。",
        };

        /// <summary>睡觉时的独白。</summary>
        private static readonly string[] TaskSleep = new[]
        {
            "zzZ... 呼...",
            "好困，终于能睡了。",
            "梦里啥都有。",
            "充电五分钟，干活两小时。",
        };

        /// <summary>悬赏/任务栏相关独白。</summary>
        private static readonly string[] TaskBounty = new[]
        {
            "这悬赏金还行。",
            "帮人打工，赚点外快。",
            "拿了钱就得把活干好。",
            "悬赏任务效率优先。",
            "看看有什么好任务...",
        };

        /// <summary>穿戴时的独白。</summary>
        private static readonly string[] TaskWear = new[]
        {
            "新装备真不错！",
            "穿上这个厉害多了。",
            "工欲善其事，必先利其器。",
            "这装备手感真好。",
        };

        // ===== 心智层反馈语料（自主意志/人生事件/关系变化）=====

        /// <summary>怨恨导致的拒绝（与玩家有积怨）。</summary>
        private static readonly string[] RefuseResentment = new[]
        {
            "我不想接你的活。",
            "上次的事，我可没忘。",
            "你凭什么使唤我？",
            "我不会帮你干活的。",
            "你心里没点数吗？",
        };

        /// <summary>好感过低导致的拒绝（还不熟/疏远）。</summary>
        private static readonly string[] RefuseFavorability = new[]
        {
            "我跟你还不熟。",
            "我们不熟，别指望我。",
            "你的忙，我不想帮。",
        };

        /// <summary>意愿低落导致的拖延（没心情/没干劲）。</summary>
        private static readonly string[] DelayWillingness = new[]
        {
            "今天没心情干活。",
            "让我歇会儿，等会儿再说。",
            "现在不太想动。",
            "再等等吧……",
        };

        /// <summary>随机个人因素导致的拖延（摸鱼）。</summary>
        private static readonly string[] DelayRandomMood = new[]
        {
            "嗯……等会儿再说吧。",
            "突然不想干了。",
            "今天犯懒，先逛逛。",
            "反正也不差这一会儿。",
        };

        /// <summary>被强制命令时的怨恨反馈。</summary>
        private static readonly string[] Forced = new[]
        {
            "你逼我……",
            "我记住这次了。",
            "好，我干，但别想我高兴。",
            "当牛做马的日子我记着呢。",
        };

        /// <summary>被玩家/他人攻击时的反馈。</summary>
        private static readonly string[] EvtAttack = new[]
        {
            "你打我？！",
            "这一下我记下了。",
            "为什么要打我……",
            "疼……这笔账记下了。",
        };

        /// <summary>被击杀时的反馈。</summary>
        private static readonly string[] EvtKill = new[]
        {
            "我……就这样死了吗……",
            "原来我一直活在恐惧里。",
            "世界，真是残酷啊。",
        };

        /// <summary>濒死经历（极端饥饿等）的恐惧反馈。</summary>
        private static readonly string[] EvtNearDeath = new[]
        {
            "刚才差点死了……",
            "太可怕了，我不想死。",
            "活着真好……",
            "下次可不能这么狼狈了。",
        };

        /// <summary>交易被拒的反馈。</summary>
        private static readonly string[] EvtTradeRejected = new[]
        {
            "居然不卖给我……",
            "太不给面子了。",
            "我自己想办法。",
        };

        /// <summary>完成悬赏的反馈。</summary>
        private static readonly string[] EvtBountyCompleted = new[]
        {
            "活儿干完了，钱到手。",
            "我挺能干的嘛。",
            "搞定！",
        };

        /// <summary>阶段升级/成就反馈。</summary>
        private static readonly string[] EvtStageUp = new[]
        {
            "我好像更有底气了。",
            "这里越来越像家了。",
            "感觉自己在成长。",
        };

        /// <summary>灵光一闪（正事件）。</summary>
        private static readonly string[] EvtInsight = new[]
        {
            "突然有了个好点子！",
            "灵感像闪电一样击中了我。",
            "我好像明白了什么……",
        };

        /// <summary>横财入袋（正事件）。</summary>
        private static readonly string[] EvtWindFall = new[]
        {
            "居然捡到一笔钱！",
            "财运来了挡都挡不住。",
            "天上掉馅饼啦！",
        };

        /// <summary>小确幸（正事件）。</summary>
        private static readonly string[] EvtSmallJoy = new[]
        {
            "今天的小日子真不错。",
            "夕阳真美……",
            "小小的幸福也值得珍惜。",
        };

        /// <summary>顿悟（中事件）。</summary>
        private static readonly string[] EvtEnlightenment = new[]
        {
            "我想通了一些事。",
            "人生好像有了新的意义。",
        };

        /// <summary>遭遇变故（负事件）。</summary>
        private static readonly string[] EvtMisfortune = new[]
        {
            "真是倒霉的一天……",
            "屋漏偏逢连夜雨。",
            "怎么什么坏事都让我碰上。",
        };

        /// <summary>染上小病（负事件）。</summary>
        private static readonly string[] EvtIllness = new[]
        {
            "咳咳……有点不舒服。",
            "身体好像在抗议了。",
            "得好好休息才行。",
        };

        /// <summary>自己突破境界的狂喜（M2A 修仙事件接心智层）。</summary>
        private static readonly string[] EvtBreakthrough = new[]
        {
            "突破了！灵气在体内奔涌！",
            "瓶颈碎了，从此海阔天空！",
            "这就是更高境界的感觉吗……",
        };

        /// <summary>自己觉醒异能的悸动。</summary>
        private static readonly string[] EvtPowerAwaken = new[]
        {
            "体内涌起陌生的力量……",
            "这就是血脉觉醒的感觉？！",
            "我好像……变得不一样了。",
        };

        /// <summary>工友突破的敬仰（{0}=突破者名）。</summary>
        private static readonly string[] EvtFellowBreakthrough = new[]
        {
            "{0}都突破了……我也要加紧修炼。",
            "不愧是{0}，修为又精进了！",
            "什么时候我也能像{0}一样……",
        };

        /// <summary>工友突破的嫉妒（{0}=突破者名）。</summary>
        private static readonly string[] EvtFellowBreakthroughEnvy = new[]
        {
            "凭什么{0}突破了我没有……",
            "哼，{0}有什么了不起的。",
            "又是{0}出风头……不甘心。",
        };

        /// <summary>噩梦缠身（负事件）。</summary>
        private static readonly string[] EvtNightmare = new[]
        {
            "昨晚做了个可怕的梦……",
            "梦里的阴影还在心头。",
            "为什么总会梦到那种事……",
        };

        /// <summary>玩家求教功法成功（正向）。</summary>
        private static readonly string[] EvtTeachSeek = new[]
        {
            "能帮到道友一二，也算缘分。",
            "传功于人，自己的领悟也深了三分。",
            "希望这点心得对他有用。",
        };

        /// <summary>被玩家安抚（正向）。</summary>
        private static readonly string[] EvtComforted = new[]
        {
            "心里的疙瘩解开了些。",
            "有人关心，感觉没那么累了。",
            "聊完之后，松了口气。",
        };

        /// <summary>玩家道歉（关系修复）。</summary>
        private static readonly string[] EvtApology = new[]
        {
            "既然他认了错，就算了吧。",
            "能屈能伸，此人可交。",
            "心结打开了一半。",
        };

        /// <summary>收到玩家赠礼（感恩）。</summary>
        private static readonly string[] EvtGiftPlayer = new[]
        {
            "这份心意我记下了。",
            "得人恩惠，日后必报。",
            "他出手真大方……",
        };

        /// <summary>关系升级为友谊。</summary>
        private static readonly string[] RelFriendship = new[]
        {
            "我们成了好朋友！",
            "有个朋友真好。",
            "和他越来越有默契了。",
        };

        /// <summary>关系恶化为敌意。</summary>
        private static readonly string[] RelEnmity = new[]
        {
            "我们成冤家了……",
            "跟他势不两立。",
            "以后再也不会帮他了。",
        };

        /// <summary>记仇。</summary>
        private static readonly string[] RelGrudge = new[]
        {
            "这笔账我记下了。",
            "我不会原谅他的。",
            "等着瞧。",
        };

        /// <summary>产生爱慕。</summary>
        private static readonly string[] RelAdmiration = new[]
        {
            "他真了不起！",
            "有点崇拜他了。",
            "要是能像他一样厉害就好了。",
        };

        /// <summary>
        /// 获取一条随机内心独白（漫游时使用）。
        /// </summary>
        public static string GetRandom(
            float curHungry = 100f, float maxHungry = 100f,
            float curTired = 0f, float maxTired = 100f,
            float curSpirit = 100f, float maxSpirit = 100f)
        {
            float hungryRatio = maxHungry > 0 ? curHungry / maxHungry : 1f;
            float tiredRatio = maxTired > 0 ? curTired / maxTired : 1f;
            float spiritRatio = maxSpirit > 0 ? curSpirit / maxSpirit : 1f;

            var entries = new (string[] pool, float weight)[]
            {
                (General, 1.0f),
                (Hungry, hungryRatio < 0.4f ? 0.3f : 0.05f),
                (Tired, tiredRatio > 0.6f ? 0.3f : 0.05f),
                (SpiritLow, spiritRatio < 0.4f ? 0.25f : 0.05f),
                (Gathering, 0.1f),
                (Building, 0.1f),
                (Social, 0.08f),
            };

            return PickByWeight(entries);
        }

        /// <summary>
        /// 获取一条任务相关的随机内心独白（执行任务时使用）。
        /// 任务独白 65%，通用 20%，状态补充 15%。
        /// </summary>
        public static string GetRandomForTask(
            WorkerTaskType taskType,
            float curHungry = 100f, float maxHungry = 100f,
            float curTired = 0f, float maxTired = 100f)
        {
            string[] taskPool = GetTaskPool(taskType);
            float hungryRatio = maxHungry > 0 ? curHungry / maxHungry : 1f;
            float tiredRatio = maxTired > 0 ? curTired / maxTired : 1f;

            var entries = new (string[] pool, float weight)[]
            {
                (taskPool, 0.65f),
                (General, 0.20f),
                (Hungry, hungryRatio < 0.4f ? 0.08f : 0.02f),
                (Tired, tiredRatio > 0.6f ? 0.08f : 0.02f),
            };

            return PickByWeight(entries);
        }

        /// <summary>
        /// 获取拒绝/拖延玩家命令的气泡理由文本（心智层自主意志反馈）。
        /// </summary>
        /// <param name="acceptance">接受结果（Delay/Refuse）。</param>
        /// <param name="reasonKey">判定理由键（WorkerMindConstant.Reason*）。</param>
        public static string GetRefusalReason(LAB2D.Domain.Worker.CommandAcceptance acceptance, string reasonKey)
        {
            string[] pool;
            switch (reasonKey)
            {
                case WorkerMindConstant.ReasonResentment:
                    pool = RefuseResentment;
                    break;
                case WorkerMindConstant.ReasonFavorability:
                    pool = RefuseFavorability;
                    break;
                case WorkerMindConstant.ReasonWillingness:
                    pool = DelayWillingness;
                    break;
                default: // ReasonRandomMood 等
                    pool = DelayRandomMood;
                    break;
            }

            return PickRandom(pool);
        }

        /// <summary>
        /// 获取被玩家强制命令时的怨恨气泡文本。
        /// </summary>
        public static string GetForcedReason()
        {
            return PickRandom(Forced);
        }

        /// <summary>
        /// 获取事件（被攻击/濒死/完成悬赏等）对应的气泡文本（心智层事件反馈）。
        /// 未配置语料的类型返回空串（调用方自行决定是否弹气泡）。
        /// </summary>
        public static string GetEventThought(string typeKey, string targetName)
        {
            string[] pool;
            switch (typeKey)
            {
                case WorkerMindConstant.EVT_PLAYER_ATTACK:
                case WorkerMindConstant.EVT_WORKER_ATTACK:
                    pool = EvtAttack;
                    break;
                case WorkerMindConstant.EVT_PLAYER_KILL:
                    pool = EvtKill;
                    break;
                case WorkerMindConstant.EVT_NEAR_DEATH:
                    pool = EvtNearDeath;
                    break;
                case WorkerMindConstant.EVT_TRADE_REJECTED:
                    pool = EvtTradeRejected;
                    break;
                case WorkerMindConstant.EVT_BOUNTY_COMPLETED:
                    pool = EvtBountyCompleted;
                    break;
                case WorkerMindConstant.EVT_STAGE_UP:
                    pool = EvtStageUp;
                    break;
                case WorkerMindConstant.EVT_INSIGHT:
                    pool = EvtInsight;
                    break;
                case WorkerMindConstant.EVT_WIND_FALL:
                    pool = EvtWindFall;
                    break;
                case WorkerMindConstant.EVT_SMALL_JOY:
                    pool = EvtSmallJoy;
                    break;
                case WorkerMindConstant.EVT_ENLIGHTENMENT:
                    pool = EvtEnlightenment;
                    break;
                case WorkerMindConstant.EVT_MISFORTUNE:
                    pool = EvtMisfortune;
                    break;
                case WorkerMindConstant.EVT_ILLNESS:
                    pool = EvtIllness;
                    break;
                case WorkerMindConstant.EVT_NIGHTMARE:
                    pool = EvtNightmare;
                    break;
                case WorkerMindConstant.EVT_CULTIVATION_BREAKTHROUGH:
                    pool = EvtBreakthrough;
                    break;
                case WorkerMindConstant.EVT_POWER_AWAKEN:
                    pool = EvtPowerAwaken;
                    break;
                case WorkerMindConstant.EVT_FELLOW_BREAKTHROUGH:
                    pool = EvtFellowBreakthrough;
                    break;
                case WorkerMindConstant.EVT_FELLOW_BREAKTHROUGH_ENVY:
                    pool = EvtFellowBreakthroughEnvy;
                    break;
                case WorkerMindConstant.EVT_TEACH_SEEK:
                    pool = EvtTeachSeek;
                    break;
                case WorkerMindConstant.EVT_COMFORTED:
                    pool = EvtComforted;
                    break;
                case WorkerMindConstant.EVT_APOLOGY:
                    pool = EvtApology;
                    break;
                case WorkerMindConstant.EVT_GIFT_PLAYER:
                    pool = EvtGiftPlayer;
                    break;
                default:
                    return string.Empty;
            }

            string text = PickRandom(pool);
            if (!string.IsNullOrEmpty(targetName) && text.Contains("{0}"))
            {
                text = string.Format(text, targetName);
            }

            return text;
        }

        /// <summary>
        /// 获取关系等级变化的气泡文本（自发关系系统反馈：友谊建立/敌意/记仇/爱慕）。
        /// 未配置语料的类型返回空串。
        /// </summary>
        public static string GetRelationThought(LAB2D.Domain.Worker.RelationKind kind)
        {
            switch (kind)
            {
                case LAB2D.Domain.Worker.RelationKind.Friendship:
                    return PickRandom(RelFriendship);
                case LAB2D.Domain.Worker.RelationKind.Enmity:
                    return PickRandom(RelEnmity);
                case LAB2D.Domain.Worker.RelationKind.Grudge:
                    return PickRandom(RelGrudge);
                case LAB2D.Domain.Worker.RelationKind.Admiration:
                    return PickRandom(RelAdmiration);
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 根据 WorkerTaskType 返回对应的独白词库。
        /// </summary>
        private static string[] GetTaskPool(WorkerTaskType taskType)
        {
            switch (taskType)
            {
                case WorkerTaskType.Build: return TaskBuild;
                case WorkerTaskType.Gather: return TaskGather;
                case WorkerTaskType.Carry: return TaskCarry;
                case WorkerTaskType.Eat: return TaskEat;
                case WorkerTaskType.Exercise: return TaskExercise;
                case WorkerTaskType.Plant: return TaskPlant;
                case WorkerTaskType.Sleep:
                case WorkerTaskType.GroundSleep: return TaskSleep;
                case WorkerTaskType.Bounty:
                case WorkerTaskType.PickUp: return TaskBounty;
                case WorkerTaskType.Wear: return TaskWear;
                default: return General;
            }
        }

        /// <summary>
        /// 按权重从多个池中随机选取一条。
        /// </summary>
        private static string PickByWeight((string[] pool, float weight)[] entries)
        {
            float total = 0f;
            for (int i = 0; i < entries.Length; i++)
            {
                total += entries[i].weight;
            }

            float roll = Random.value * total;
            float acc = 0f;

            for (int i = 0; i < entries.Length; i++)
            {
                acc += entries[i].weight;
                if (roll <= acc)
                {
                    return PickRandom(entries[i].pool);
                }
            }

            // fallback: 返回最后一个池
            return PickRandom(entries[entries.Length - 1].pool);
        }

        private static string PickRandom(string[] pool)
        {
            return pool[Random.Range(0, pool.Length)];
        }
    }
}
