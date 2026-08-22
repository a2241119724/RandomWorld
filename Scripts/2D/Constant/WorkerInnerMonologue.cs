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
