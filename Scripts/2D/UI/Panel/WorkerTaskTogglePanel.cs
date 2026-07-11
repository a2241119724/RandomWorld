namespace LAB2D.UI.Panel
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
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
            Transform title = LAB2D.Tool.Tool.GetComponentInChildren<Transform>(this.Panel, "Title");
            foreach (KeyValuePair<AWorkerTask.WorkerTaskTypeEnum, string> pair in TypeToChinese)
            {
                LAB2D.Tool.Tool.GetComponentInChildren<Text>(title.GetChild((int)pair.Key + 1).gameObject, "Text").text = pair.Value;
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
