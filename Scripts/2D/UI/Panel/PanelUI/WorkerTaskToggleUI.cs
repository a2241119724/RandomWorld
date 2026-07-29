namespace LAB2D.UI.Panel.PanelUI
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Enum;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Worker可做任务开关 UI
    /// </summary>
    public class WorkerTaskToggleUI : MonoBehaviour
    {
        /// <summary>
        /// 任务项
        /// </summary>
        public List<GameObject> TaskItems { get; set; }

        /// <summary>
        /// 在面板中挂在在所有TaskToggle上。
        /// 通过 TaskToggleBinding 组件获取 Toggle 对应的 WorkerTaskType，
        /// 不再依赖 UI 层级顺序隐式映射。
        /// </summary>
        /// <param name="toggle">开关UI</param>
        public void TaskToggle(Toggle toggle)
        {
            TaskToggleBinding binding = toggle.GetComponent<TaskToggleBinding>();
            if (binding == null)
            {
                return;
            }

            int x = toggle.transform.parent.GetSiblingIndex() - 1;
            List<AWorker> workers = ServiceLocator.Get<WorkerManager>().Characters;
            if (x < 0 || x >= workers.Count)
            {
                return;
            }

            AWorker.WorkerData workerData = workers[x].CharacterDataLAB as AWorker.WorkerData;
            if (workerData != null && workerData.TaskToggle != null)
            {
                workerData.TaskToggle[binding.TaskType] = toggle.isOn;
            }
        }

        public void Awake()
        {
            this.TaskItems = new List<GameObject>();
            for (int i = 0; i < this.transform.childCount - 1; i++)
            {
                this.TaskItems.Add(this.transform.GetChild(i + 1).gameObject);
            }
        }

        private void OnEnable()
        {
            List<AWorker> workers = ServiceLocator.Get<WorkerManager>().Characters;

            // UI不够,创建
            int count = workers.Count - (this.transform.childCount - 1);
            if (count > 0)
            {
                for (int i = count; i > 0; i--)
                {
                    GameObject g = ServiceLocator.Get<ResourceManager>().Instantiate(PrefabConstant.TASK_ITEM, this.transform, false);
                    this.TaskItems.Add(g);

                    // 添加事件
                    for (int j = 1; j < g.transform.childCount; j++)
                    {
                        Toggle toggle = g.transform.GetChild(j).GetComponent<Toggle>();
                        toggle.onValueChanged.AddListener((bool isOn) =>
                        {
                            this.TaskToggle(toggle);
                        });
                    }
                }
            }

            // 清空TaskItem
            for (int i = 0; i < this.transform.childCount - 1; i++)
            {
                this.TaskItems[i].SetActive(false);
            }

            int index = 0;
            foreach (AWorker worker in workers)
            {
                this.TaskItems[index].SetActive(true);
                LAB2D.Tool.Tool.GetComponentInChildren<Text>(this.TaskItems[index].transform.GetChild(0).gameObject, "Text").text = worker.name;
                AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
                for (int i = 1; i < this.TaskItems[index].transform.childCount; i++)
                {
                    Transform toggleTransform = this.TaskItems[index].transform.GetChild(i);
                    Toggle toggle = toggleTransform.GetComponent<Toggle>();
                    TaskToggleBinding binding = toggleTransform.GetComponent<TaskToggleBinding>();
                    if (binding != null && workerData.TaskToggle != null)
                    {
                        // 字典中没有记录的任务类型默认为开启（与 IsCanWork 的 opt-out 语义一致）
                        if (workerData.TaskToggle.TryGetValue(binding.TaskType, out bool enabled))
                        {
                            toggle.isOn = enabled;
                        }
                        else
                        {
                            toggle.isOn = true;
                        }
                    }
                }

                index++;
            }
        }
    }
}
