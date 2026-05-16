namespace LAB2D
{
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
            var sb = new StringBuilder();

            if (this.workerDetails != null && this.workerDetails.Count > 0)
            {
                sb.Append("自身:");
                sb.Append(this.workerDetails[0].ToPromptText());
                sb.Append('。');
            }

            return sb.ToString();
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
                if (!string.IsNullOrEmpty(this.workerName))
                {
                    sb.Append(this.workerName);
                }
                else
                {
                    sb.Append("Worker");
                }

                sb.Append(" Lv");
                sb.Append(this.level);
                sb.Append(" HP");
                sb.Append(this.hp.ToString("F0"));
                sb.Append('/');
                sb.Append(this.maxHp.ToString("F0"));

                if (!string.IsNullOrEmpty(this.positionText))
                {
                    sb.Append(" 位置");
                    sb.Append(this.positionText);
                }

                if (!string.IsNullOrEmpty(this.conditionText))
                {
                    sb.Append(" 状态");
                    sb.Append(TrimLeadingLabel(this.conditionText, "状态"));
                }

                if (!string.IsNullOrEmpty(this.stateText))
                {
                    sb.Append('/');
                    sb.Append(this.stateText);
                }

                sb.Append(" 饥饿");
                sb.Append(this.hungry.ToString("F0"));
                sb.Append('/');
                sb.Append(this.maxHungry.ToString("F0"));
                sb.Append(" 疲劳");
                sb.Append(this.tired.ToString("F0"));
                sb.Append('/');
                sb.Append(this.maxTired.ToString("F0"));
                sb.Append(" 床");
                sb.Append(this.hasBed ? "有" : "无");

                sb.Append(" 任务:");
                sb.Append(string.IsNullOrEmpty(this.taskText) ? "空闲" : this.taskText);

                if (!string.IsNullOrEmpty(this.equipmentText))
                {
                    sb.Append(" 装备:");
                    sb.Append(this.equipmentText);
                }

                if (this.isSeeking)
                {
                    sb.Append(" 寻路->");
                    sb.Append(string.IsNullOrEmpty(this.seekTargetText) ? "未知" : this.seekTargetText);
                }

                if (!string.IsNullOrEmpty(this.enabledTaskText))
                {
                    sb.Append(" 开关:");
                    sb.Append(this.enabledTaskText);
                }

                return sb.ToString();
            }

            private static string TrimLeadingLabel(string text, string label)
            {
                if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(label))
                {
                    return text;
                }

                return text.StartsWith(label) ? text.Substring(label.Length) : text;
            }
        }
    }
}
