using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class WorkerTaskInfoPanel : BasePanel<WorkerTaskInfoPanel>
    {
        private static readonly Dictionary<TaskType, string> typeToChinese = new Dictionary<TaskType, string>
        {
            { TaskType.Build,"����"},
            { TaskType.Carry,"����"},
            { TaskType.Gather,"��ժ"},
            { TaskType.Exercise,"����"},
            { TaskType.Hungry,"�Է�"},
            { TaskType.Wear,"����"},
            { TaskType.Sleep,"˯��"},
            { TaskType.Plant,"��ֲ"},
        };

        public WorkerTaskInfoPanel()
        {
            Name = "WorkerTaskInfo";
            setPanel();
            Transform title = Tool.GetComponentInChildren<Transform>(panel,"Title");
            foreach(KeyValuePair<TaskType, string> pair in typeToChinese)
            {
                Tool.GetComponentInChildren<Text>(title.GetChild((int)pair.Key + 1).gameObject, "Text").text = pair.Value;
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
        }
    }
}
