namespace LAB2D.UI.Character
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Serializable;
    using LAB2D.UnityAdapter;
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
            AWorker worker = FurnitureManager.Instance.GetWorkerByBed(posMap);
            this.curWorker.text = $"当前: " + (worker != null ? worker.name : "没人");

            this.transform.position = TileMap.Instance.MapPosToWorldPos(posMap);
            List<AWorker> workers = WorkerManager.Instance.Characters;
            for (int i = 0; i < workers.Count; i++)
            {
                // 若没有对应的物体，先创建
                if (i > this.content.childCount - 1)
                {
                    GameObject g = ResourceManager.Instance.Instantiate(PrefabConstant.WORKER_BED_ITEM);
                    g.transform.SetParent(this.content);
                    g.transform.localScale = Vector3.one;
                }

                LAB2D.Tool.Tool.GetComponentInChildren<Text>(this.content.GetChild(i).gameObject, "Name").text = workers[i].name;
                Button button = this.content.GetChild(i).gameObject.GetComponent<Button>();
                button.onClick.RemoveAllListeners();

                // 默认为引用传递，变为固定值
                int index = i;
                button.onClick.AddListener(() =>
                {
                    WorkerTaskManager.Instance.AddTask(
                        new WorkerSleepTask.SleepTaskBuilder()
                        .SetTarget(posMap).SetWorker(workers[index]).Build(), Vector3IntLAB.ToVector3IntLAB(posMap),
                        1);
                    this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
                    FurnitureManager.Instance.AddWorkerToBed(posMap, workers[index]);
                });
            }
        }

        public void Awake()
        {
            Instance = this;
            ServiceLocator.Register(this);
            this.content = LAB2D.Tool.Tool.GetComponentInChildren<Transform>(this.gameObject, "Content");
            this.curWorker = LAB2D.Tool.Tool.GetComponentInChildren<Text>(this.gameObject, "CurWorker");
        }

        public void Update()
        {
            if (UnityGlobalInputAdapter.GetWorkerBedDismissDown())
            {
                // transform.position = ResourceConstant.VECTOR3_DEFAULT;
            }
        }
    }
}
