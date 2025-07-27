namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 穿戴任务UI
    /// </summary>
    public class WearTaskUI : MonoBehaviour
    {
        private Transform content;

        /// <summary>
        /// 单例
        /// </summary>
        public static WearTaskUI Instance { get; private set; }

        /// <summary>
        /// 展示穿戴任务
        /// </summary>
        /// <param name="posMap">位置</param>
        public void ShowWearTask(Vector3Int posMap)
        {
            this.transform.position = TileMap.Instance.MapPosToWorldPos(posMap);
            List<Worker> workers = WorkerManager.Instance.Characters;
            ResourceInfo resourceInfo = InventoryManager.Instance.GetResourceByPos(posMap);

            // 该位置没有东西则不展示任何东西
            if (resourceInfo == null)
            {
                return;
            }

            for (int i = 0; i < workers.Count; i++)
            {
                // 若没有对应的物体，先创建
                if (i > this.content.childCount - 1)
                {
                    GameObject g = GameObject.Instantiate(ResourcesManager.Instance.GetPrefab("WorkerItem"));
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
                    WorkerTaskManager.Instance.AddTask(
                        new WorkerWearTask.WearTaskBuilder()
                        .SetWorker(workers[index]).SetTarget(posMap).SetEquipmentId(resourceInfo.Id).Build(), 1);
                    this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
                    Dictionary<int, ResourceInfo> dict = new ();
                    dict.Add(resourceInfo.Id, resourceInfo);
                    InventoryManager.Instance.IsEnoughAndPreTake(workers[index], new Dictionary<int, ResourceInfo>(dict), true);
                });
            }
        }

        private void Awake()
        {
            Instance = this;
            this.content = Tool.GetComponentInChildren<Transform>(this.gameObject, "Content");
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
