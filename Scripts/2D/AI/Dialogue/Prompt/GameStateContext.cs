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
        // ---- 玩家 ----
        public string playerName = "冒险者";
        public int playerLevel;
        public float playerHp;
        public float playerMaxHp = 100;

        // ---- 位置/地图 ----
        public string currentLocation = "边境小镇";
        public string mapRegion = "边境之地";
        /// <summary>地图宽度（Tile数）</summary>
        public int mapWidth;
        /// <summary>地图高度（Tile数）</summary>
        public int mapHeight;

        // ---- 天气/时间/季节 ----
        public string currentWeather = "晴天";
        public string timeOfDay = "白天";
        public string season = "春天";

        // ---- 环境（EnvironmentManager） ----
        public float environmentTemperature;
        public float environmentHumidity;
        public float environmentEnergy;
        public float environmentMaxEnergy = 100;

        // ---- 波次 ----
        public int waveNumber;

        // ---- 阵营/任务 ----
        public string playerFaction = "无";
        public string activeQuest = "无";

        // ---- NPC 好感度 ----
        public float npcFavorability = 50;

        // ---- 殖民地工人聚合 ----
        /// <summary>工人总数</summary>
        public int totalWorkerCount;
        /// <summary>空闲工人数</summary>
        public int idleWorkerCount;
        /// <summary>忙碌工人数</summary>
        public int busyWorkerCount;
        /// <summary>饥饿工人数</summary>
        public int hungryWorkerCount;
        /// <summary>疲劳工人数</summary>
        public int tiredWorkerCount;
        /// <summary>临界状态工人数</summary>
        public int criticalWorkerCount;
        /// <summary>缺床位工人数</summary>
        public int workerWithoutBedCount;
        /// <summary>任务总数</summary>
        public int totalTaskCount;
        /// <summary>阻塞任务数</summary>
        public int blockedTaskCount;

        // ---- 扩展 ----
        public Dictionary<string, string> customFlags = new Dictionary<string, string>();

        /// <summary>
        /// 格式化为世界/环境 Prompt 文本
        /// </summary>
        public string ToWorldInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("世界:");
            sb.Append(this.mapRegion);
            sb.Append(' ');
            sb.Append(this.currentLocation);
            sb.Append(' ');
            sb.Append(this.currentWeather);
            sb.Append(' ');
            sb.Append(this.timeOfDay);
            sb.Append(' ');
            sb.Append(this.season);
            // 环境数值
            sb.Append(" 温度");
            sb.Append(this.environmentTemperature.ToString("F0"));
            sb.Append(" 湿度");
            sb.Append(this.environmentHumidity.ToString("F0"));
            sb.Append(" 灵气");
            sb.Append(this.environmentEnergy.ToString("F0"));
            sb.Append('/');
            sb.Append(this.environmentMaxEnergy.ToString("F0"));
            // 地图尺寸
            if (this.mapWidth > 0 && this.mapHeight > 0)
            {
                sb.Append(" 地图");
                sb.Append(this.mapWidth);
                sb.Append('x');
                sb.Append(this.mapHeight);
            }
            sb.Append('。');
            return sb.ToString();
        }

        /// <summary>
        /// 格式化为玩家状态 + 工人聚合 Prompt 文本
        /// </summary>
        public string ToPromptText()
        {
            var sb = new System.Text.StringBuilder();

            // 玩家
            sb.Append("玩家:");
            sb.Append(this.playerName);
            sb.Append(" Lv");
            sb.Append(this.playerLevel);
            sb.Append(" HP");
            sb.Append(this.playerHp.ToString("F0"));
            sb.Append('/');
            sb.Append(this.playerMaxHp.ToString("F0"));

            if (!string.IsNullOrEmpty(this.playerFaction) && this.playerFaction != "无")
            {
                sb.Append(" 阵营:");
                sb.Append(this.playerFaction);
            }

            if (!string.IsNullOrEmpty(this.activeQuest) && this.activeQuest != "无")
            {
                sb.Append(" 任务:");
                sb.Append(this.activeQuest);
            }

            sb.Append(" 好感:");
            sb.Append(this.npcFavorability.ToString("F0"));

            if (this.waveNumber > 0)
            {
                sb.Append(" 波次:");
                sb.Append(this.waveNumber);
            }

            sb.Append("。");

            // 工人聚合（有人时才输出）
            if (this.totalWorkerCount > 0)
            {
                sb.Append("工人:");
                sb.Append(this.totalWorkerCount);
                sb.Append("人 空闲");
                sb.Append(this.idleWorkerCount);
                sb.Append(" 忙碌");
                sb.Append(this.busyWorkerCount);
                if (this.hungryWorkerCount > 0)
                {
                    sb.Append(" 饥饿");
                    sb.Append(this.hungryWorkerCount);
                }
                if (this.tiredWorkerCount > 0)
                {
                    sb.Append(" 疲劳");
                    sb.Append(this.tiredWorkerCount);
                }
                if (this.criticalWorkerCount > 0)
                {
                    sb.Append(" 临界");
                    sb.Append(this.criticalWorkerCount);
                }
                if (this.workerWithoutBedCount > 0)
                {
                    sb.Append(" 缺床");
                    sb.Append(this.workerWithoutBedCount);
                }
                if (this.totalTaskCount > 0)
                {
                    sb.Append(" 任务");
                    sb.Append(this.totalTaskCount);
                    if (this.blockedTaskCount > 0)
                    {
                        sb.Append("(阻塞");
                        sb.Append(this.blockedTaskCount);
                        sb.Append(')');
                    }
                }
                sb.Append('。');
            }

            return sb.ToString();
        }
    }
}
