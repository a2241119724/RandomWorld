namespace LAB2D.AI.Dialogue.Prompt
{
    using LAB2D;
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// 当前游戏状态上下文（注入 Prompt）
    /// </summary>
    [Serializable]
    public class GameStateContext
    {
        // ---- Worker 明细 ----
        public List<WorkerPromptInfo> workerDetails = new List<WorkerPromptInfo>();

        // ---- 扩展 ----
        public Dictionary<string, string> customFlags = new Dictionary<string, string>();

        /// <summary>
        /// 格式化为世界/环境 Prompt 文本。Worker 对话不注入外部环境信息。
        /// </summary>
        public string ToWorldInfo()
        {
            return string.Empty;
        }

        /// <summary>
        /// 格式化为当前对话 Worker 的自身 Prompt 文本
        /// </summary>
        public string ToPromptText()
        {
            if (this.workerDetails != null && this.workerDetails.Count > 0)
            {
                return this.workerDetails[0].ToPromptText();
            }

            return "- 位置在 未知\n"
                + "- 生命 未知\n"
                + "- 疲劳 未知\n"
                + "- 饥饿 未知\n"
                + "- 任务：空闲\n"
                + "- 装备：无。";
        }

        [Serializable]
        public class WorkerPromptInfo
        {
            public string workerName;
            public int level;
            public float hp;
            public float maxHp;
            public string positionText;
            public string conditionText;
            public string stateText;
            public float hungry;
            public float maxHungry;
            public float tired;
            public float maxTired;
            public bool hasBed;
            public string taskText;
            public string equipmentText;
            public bool isSeeking;
            public string seekTargetText;
            public string enabledTaskText;

            public string ToPromptText()
            {
                var sb = new StringBuilder();
                sb.Append("- 位置在 ");
                sb.Append(string.IsNullOrEmpty(this.positionText) ? "未知" : this.positionText);
                sb.Append('\n');

                sb.Append("- 生命 ");
                sb.Append(this.hp.ToString("F0"));
                sb.Append('/');
                sb.Append(this.maxHp.ToString("F0"));
                sb.Append('\n');

                sb.Append("- 疲劳 ");
                sb.Append(this.tired.ToString("F0"));
                sb.Append('/');
                sb.Append(this.maxTired.ToString("F0"));
                sb.Append('\n');

                sb.Append("- 饥饿 ");
                sb.Append(this.hungry.ToString("F0"));
                sb.Append('/');
                sb.Append(this.maxHungry.ToString("F0"));
                sb.Append('\n');

                sb.Append("- 任务：");
                sb.Append(string.IsNullOrEmpty(this.taskText) ? "空闲" : this.taskText);
                sb.Append('\n');

                sb.Append("- 装备：");
                sb.Append(string.IsNullOrEmpty(this.equipmentText) ? "无" : this.equipmentText);
                sb.Append('。');

                return sb.ToString();
            }
        }
    }
}
