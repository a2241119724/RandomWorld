namespace LAB2D
{
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
        /// 在面板中挂在在所有TaskToggle上
        /// </summary>
        /// <param name="toggle">开关UI</param>
        public void TaskToggle(Toggle toggle)
        {
            int x = toggle.transform.parent.GetSiblingIndex() - 1;
            int y = toggle.transform.GetSiblingIndex() - 1;
            List<Worker> workers = WorkerManager.Instance.Characters;
            Worker.WorkerData workerData = workers[x].CharacterDataLAB as Worker.WorkerData;
            workerData.TaskToggle[y] = toggle.isOn;
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
            List<Worker> workers = WorkerManager.Instance.Characters;

            // UI不够,创建
            int count = workers.Count - (this.transform.childCount - 1);
            if (count > 0)
            {
                for (int i = count; i > 0; i--)
                {
                    GameObject g = GameObject.Instantiate(ResourceManager.Instance.GetPrefab("TaskItem"), this.transform, false);
                    g.name = "TaskItem";
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
            foreach (Worker worker in workers)
            {
                this.TaskItems[index].SetActive(true);
                Tool.GetComponentInChildren<Text>(this.TaskItems[index].transform.GetChild(0).gameObject, "Text").text = worker.name;
                Worker.WorkerData workerData = worker.CharacterDataLAB as Worker.WorkerData;
                for (int i = 1; i < this.TaskItems[index].transform.childCount; i++)
                {
                    this.TaskItems[index].transform.GetChild(i).GetComponent<Toggle>().isOn = workerData.TaskToggle[i - 1];
                }

                index++;
            }
        }
    }
}
