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
        private static readonly Dictionary<WorkerTask.WorkerTaskTypeEnum, string> TypeToChinese = new ()
        {
            { WorkerTask.WorkerTaskTypeEnum.Build, "建造" },
            { WorkerTask.WorkerTaskTypeEnum.Carry, "搬运" },
            { WorkerTask.WorkerTaskTypeEnum.Gather, "采摘" },
            { WorkerTask.WorkerTaskTypeEnum.Exercise, "锻炼" },
            { WorkerTask.WorkerTaskTypeEnum.Eat, "吃饭" },
            { WorkerTask.WorkerTaskTypeEnum.Wear, "穿戴" },
            { WorkerTask.WorkerTaskTypeEnum.Sleep, "睡觉" },
            { WorkerTask.WorkerTaskTypeEnum.Plant, "种植" },
        };

        public WorkerTaskInfoPanel()
        {
            this.Name = "WorkerTaskInfo";
            this.OpenPanel();
            Transform title = Tool.GetComponentInChildren<Transform>(this.Panel, "Title");
            foreach (KeyValuePair<WorkerTask.WorkerTaskTypeEnum, string> pair in TypeToChinese)
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
