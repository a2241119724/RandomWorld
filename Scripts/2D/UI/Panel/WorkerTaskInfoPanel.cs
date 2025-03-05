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
            { TaskType.Build,"½¨Ôì"},
            { TaskType.Carry,"°áÔË"},
            { TaskType.Gather,"²ÉÕª"},
            { TaskType.Exercise,"¶ÍÁ¶"},
            { TaskType.Hungry,"³Ô·¹"},
            { TaskType.Wear,"´©´÷"},
            { TaskType.Sleep,"Ë¯¾õ"},
            { TaskType.Plant,"ÖÖÖ²"},
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
