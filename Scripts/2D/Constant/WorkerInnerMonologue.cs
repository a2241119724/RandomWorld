namespace LAB2D.Constant
{
    using UnityEngine;

    /// <summary>
    /// Worker 闲逛漫游时的内心独白语料库。
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

        /// <summary>
        /// 获取一条随机内心独白。
        /// 会根据 Worker 当前状态选择合适的情境分类后随机选取。
        /// </summary>
        /// <param name="curHungry">当前饥饿值</param>
        /// <param name="maxHungry">最大饥饿值</param>
        /// <param name="curTired">当前疲劳值</param>
        /// <param name="maxTired">最大疲劳值</param>
        /// <param name="curSpirit">当前精气神</param>
        /// <param name="maxSpirit">最大精气神</param>
        /// <returns>随机内心独白字符串</returns>
        public static string GetRandom(
            float curHungry = 100f, float maxHungry = 100f,
            float curTired = 100f, float maxTired = 100f,
            float curSpirit = 100f, float maxSpirit = 100f)
        {
            // 根据状态确定候选池：80%通用 + 20%状态相关
            float hungryRatio = maxHungry > 0 ? curHungry / maxHungry : 1f;
            float tiredRatio = maxTired > 0 ? curTired / maxTired : 1f;
            float spiritRatio = maxSpirit > 0 ? curSpirit / maxSpirit : 1f;

            // 基础权重：通用独白权重最高
            float generalWeight = 1.0f;
            float hungryWeight = hungryRatio < 0.4f ? 0.3f : 0.05f;   // 饥饿时概率提升
            float tiredWeight = tiredRatio < 0.4f ? 0.3f : 0.05f;     // 疲劳时概率提升
            float spiritLowWeight = spiritRatio < 0.4f ? 0.25f : 0.05f;
            float gatheringWeight = 0.1f;
            float buildingWeight = 0.1f;
            float socialWeight = 0.08f;

            float totalWeight = generalWeight + hungryWeight + tiredWeight
                + spiritLowWeight + gatheringWeight + buildingWeight + socialWeight;

            float roll = Random.value * totalWeight;
            float accumulator = 0f;

            accumulator += generalWeight;
            if (roll <= accumulator) return PickRandom(General);

            accumulator += hungryWeight;
            if (roll <= accumulator) return PickRandom(Hungry);

            accumulator += tiredWeight;
            if (roll <= accumulator) return PickRandom(Tired);

            accumulator += spiritLowWeight;
            if (roll <= accumulator) return PickRandom(SpiritLow);

            accumulator += gatheringWeight;
            if (roll <= accumulator) return PickRandom(Gathering);

            accumulator += buildingWeight;
            if (roll <= accumulator) return PickRandom(Building);

            return PickRandom(Social);
        }

        private static string PickRandom(string[] pool)
        {
            return pool[Random.Range(0, pool.Length)];
        }
    }
}
