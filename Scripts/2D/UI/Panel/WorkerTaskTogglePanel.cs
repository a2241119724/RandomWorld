namespace LAB2D.UI.Panel
{
    using LAB2D.Enum;
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
        private static readonly Dictionary<WorkerTaskType, string> TypeToChinese = new ()
        {
            { WorkerTaskType.Build, "建造" },
            { WorkerTaskType.Carry, "搬运" },
            { WorkerTaskType.Gather, "采摘" },
            { WorkerTaskType.Exercise, "锻炼" },
            { WorkerTaskType.Eat, "吃饭" },
            { WorkerTaskType.Wear, "穿戴" },
            { WorkerTaskType.Sleep, "睡觉" },
            { WorkerTaskType.Plant, "种植" },
            { WorkerTaskType.Demolish, "拆除" },
        };

        public WorkerTaskTogglePanel()
        {
            this.Name = "WorkerTaskToggle";
            this.Init();
            Transform title = LAB2D.Tool.Tool.GetComponentInChildren<Transform>(this.Panel, "Title");
            foreach (KeyValuePair<WorkerTaskType, string> pair in TypeToChinese)
            {
                int childIndex = (int)pair.Key + 1;
                if (childIndex >= title.childCount)
                {
                    continue;
                }

                LAB2D.Tool.Tool.GetComponentInChildren<Text>(title.GetChild(childIndex).gameObject, "Text").text = pair.Value;
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
