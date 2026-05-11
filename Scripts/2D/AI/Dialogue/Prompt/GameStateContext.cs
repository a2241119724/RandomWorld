namespace LAB2D
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 当前游戏状态上下文（注入 Prompt）
    /// </summary>
    [Serializable]
    public class GameStateContext
    {
        /// <summary>
        /// 玩家名称
        /// </summary>
        public string playerName = "冒险者";

        /// <summary>
        /// 玩家等级
        /// </summary>
        public int playerLevel;

        /// <summary>
        /// 玩家当前血量
        /// </summary>
        public float playerHp;

        /// <summary>
        /// 玩家最大血量
        /// </summary>
        public float playerMaxHp = 100;

        /// <summary>
        /// 当前地点
        /// </summary>
        public string currentLocation = "边境小镇";

        /// <summary>
        /// 当前天气
        /// </summary>
        public string currentWeather = "晴天";

        /// <summary>
        /// 当前时段
        /// </summary>
        public string timeOfDay = "白天";

        /// <summary>
        /// 当前季节
        /// </summary>
        public string season = "春天";

        /// <summary>
        /// 当前波次
        /// </summary>
        public int waveNumber;

        /// <summary>
        /// 殖民地工人数量
        /// </summary>
        public int colonyWorkerCount;

        /// <summary>
        /// NPC 对玩家的好感度
        /// </summary>
        public float npcFavorability = 50;

        /// <summary>
        /// 当前活跃任务
        /// </summary>
        public string activeQuest = "无";

        /// <summary>
        /// 玩家阵营
        /// </summary>
        public string playerFaction = "无";

        /// <summary>
        /// 扩展字段
        /// </summary>
        public Dictionary<string, string> customFlags = new Dictionary<string, string>();

        /// <summary>
        /// 格式化为 Prompt 文本
        /// </summary>
        public string ToPromptText()
        {
            return "玩家{playerName}，{playerLevel}级，生命{hp}/{maxHp}，位于{location}，天气{weather}{time}，活跃任务{quest}，好感度{favor}。"
                .Replace("{playerName}", this.playerName)
                .Replace("{playerLevel}", this.playerLevel.ToString())
                .Replace("{hp}", this.playerHp.ToString("F0"))
                .Replace("{maxHp}", this.playerMaxHp.ToString("F0"))
                .Replace("{location}", this.currentLocation)
                .Replace("{weather}", this.currentWeather)
                .Replace("{time}", this.timeOfDay)
                .Replace("{quest}", string.IsNullOrEmpty(this.activeQuest) ? "无" : this.activeQuest)
                .Replace("{favor}", this.npcFavorability.ToString("F0"));
        }
    }
}
