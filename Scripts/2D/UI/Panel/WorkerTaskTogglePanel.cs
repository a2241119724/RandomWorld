namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Worker任务信息面板
    /// </summary>
    public class WorkerTaskTogglePanel : ABasePanel<WorkerTaskTogglePanel>
    {
        private static readonly Dictionary<AWorkerTask.WorkerTaskTypeEnum, string> TypeToChinese = new ()
        {
            { AWorkerTask.WorkerTaskTypeEnum.Build, "建造" },
            { AWorkerTask.WorkerTaskTypeEnum.Carry, "搬运" },
            { AWorkerTask.WorkerTaskTypeEnum.Gather, "采摘" },
            { AWorkerTask.WorkerTaskTypeEnum.Exercise, "锻炼" },
            { AWorkerTask.WorkerTaskTypeEnum.Eat, "吃饭" },
            { AWorkerTask.WorkerTaskTypeEnum.Wear, "穿戴" },
            { AWorkerTask.WorkerTaskTypeEnum.Sleep, "睡觉" },
            { AWorkerTask.WorkerTaskTypeEnum.Plant, "种植" },
        };

        public WorkerTaskTogglePanel()
        {
            this.Name = "WorkerTaskToggle";
            this.Init();
            Transform title = Tool.GetComponentInChildren<Transform>(this.Panel, "Title");
            foreach (KeyValuePair<AWorkerTask.WorkerTaskTypeEnum, string> pair in TypeToChinese)
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

        /// <inheritdoc/>
        public override void OnClick_Back()
        {
            this.Controller.Close();
        }
    }
}
