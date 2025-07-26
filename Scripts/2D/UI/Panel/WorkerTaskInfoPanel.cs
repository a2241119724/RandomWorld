namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Worker任务信息面板
    /// </summary>
    public class WorkerTaskInfoPanel : BasePanel<WorkerTaskInfoPanel>
    {
        private static readonly Dictionary<TaskType, string> TypeToChinese = new Dictionary<TaskType, string>
        {
            { TaskType.Build, "建造" },
            { TaskType.Carry, "搬运" },
            { TaskType.Gather, "采摘" },
            { TaskType.Exercise, "锻炼" },
            { TaskType.Hungry, "吃饭" },
            { TaskType.Wear, "穿戴" },
            { TaskType.Sleep, "睡觉" },
            { TaskType.Plant, "种植" },
        };

        public WorkerTaskInfoPanel()
        {
            this.Name = "WorkerTaskInfo";
            this.OpenPanel();
            Transform title = Tool.GetComponentInChildren<Transform>(this.Panel, "Title");
            foreach (KeyValuePair<TaskType, string> pair in TypeToChinese)
            {
                Tool.GetComponentInChildren<Text>(title.GetChild((int)pair.Key + 1).gameObject, "Text").text = pair.Value;
            }
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }
    }
}
