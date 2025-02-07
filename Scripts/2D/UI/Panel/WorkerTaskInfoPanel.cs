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
            { TaskType.Build,"Ω®‘Ï"},
            { TaskType.Carry,"∞·‘À"},
            { TaskType.Gather,"≤…’™"},
            { TaskType.Exercise,"∂Õ¡∂"},
            { TaskType.Hungry,"≥‘∑π"},
            { TaskType.Wear,"¥©¥˜"},
        };

        public WorkerTaskInfoPanel()
        {
            Name = "WorkerTaskInfo";
            setPanel();
            Transform title = Tool.GetComponentInChildren<Transform>(panel,"Title");
            foreach(KeyValuePair<TaskType, string> pair in typeToChinese)
            {
                title.GetChild((int)pair.Key + 1).GetComponent<Text>().text = pair.Value;
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
