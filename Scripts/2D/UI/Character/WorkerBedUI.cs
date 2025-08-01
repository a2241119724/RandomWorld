namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 工作者的床 UI
    /// </summary>
    public class WorkerBedUI : MonoBehaviour
    {
        private Transform content;
        private Text curWorker;

        /// <summary>
        /// 单例
        /// </summary>
        public static WorkerBedUI Instance { get; private set; }

        /// <summary>
        /// 展示工作者床 UI
        /// </summary>
        /// <param name="posMap">位置</param>
        public void ShowWorkerBed(Vector3Int posMap)
        {
            Worker worker = FurnitureManager.Instance.GetWorkerByBed(posMap);
            this.curWorker.text = $"当前: " + (worker != null ? worker.name : "没人");

            this.transform.position = TileMap.Instance.MapPosToWorldPos(posMap);
            List<Worker> workers = WorkerManager.Instance.Characters;
            for (int i = 0; i < workers.Count; i++)
            {
                // 若没有对应的物体，先创建
                if (i > this.content.childCount - 1)
                {
                    GameObject g = GameObject.Instantiate(ResourceManager.Instance.GetPrefab("WorkerBedItem"));
                    g.transform.SetParent(this.content);
                    g.transform.localScale = Vector3.one;
                }

                Tool.GetComponentInChildren<Text>(this.content.GetChild(i).gameObject, "Name").text = workers[i].name;
                Button button = this.content.GetChild(i).gameObject.GetComponent<Button>();
                button.onClick.RemoveAllListeners();

                // 默认为引用传递，变为固定值
                int index = i;
                button.onClick.AddListener(() =>
                {
                    WorkerTaskManager.Instance.AddTask(new WorkerSleepTask.SleepTaskBuilder().SetTarget(posMap).Build(), 1);
                    this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
                    FurnitureManager.Instance.AddWorkerToBed(posMap, workers[index]);
                });
            }
        }

        private void Awake()
        {
            Instance = this;
            this.content = Tool.GetComponentInChildren<Transform>(this.gameObject, "Content");
            this.curWorker = Tool.GetComponentInChildren<Text>(this.gameObject, "CurWorker");
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2) || Input.GetKeyDown(KeyCode.Escape))
            {
                // transform.position = ResourceConstant.VECTOR3_DEFAULT;
            }
        }
    }
}
