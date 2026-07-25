namespace LAB2D.UI.Action
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using LAB2D.Serializable;
    using LAB2D.UnityAdapter;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    /// 增加穿戴任务UI
    /// </summary>
    public class AddWearTaskUI : MonoBehaviour
    {
        private Transform content;

        /// <summary>
        /// 单例
        /// </summary>
        public static AddWearTaskUI Instance { get; private set; }

        /// <summary>
        /// 展示穿戴任务
        /// </summary>
        /// <param name="posMap">位置</param>
        public void ShowWearTask(Vector3Int posMap)
        {
            this.transform.position = ServiceLocator.Get<TileMap>().MapPosToWorldPos(posMap);
            List<AWorker> workers = ServiceLocator.Get<WorkerManager>().Characters;
            ResourceInfo resourceInfo = ServiceLocator.Get<InventoryManager>().GetResourceByPos(posMap);

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
                    GameObject g = ServiceLocator.Get<ResourceManager>().Instantiate(PrefabConstant.ADD_WEAR_TASK_ITEM, true);
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
                    ServiceLocator.Get<WorkerTaskManager>().AddTask(
                        new WorkerWearTask.WearTaskBuilder()
                        .SetWorker(workers[index]).SetTarget(posMap).SetEquipmentId(resourceInfo.Id).Build(), new GameGridPosition(posMap.x, posMap.y, posMap.z),
                        1);
                    this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
                    Dictionary<int, ResourceInfo> dict = new ();
                    dict.Add(resourceInfo.Id, resourceInfo);
                    ServiceLocator.Get<InventoryManager>().IsEnoughAndPreTake(workers[index], new Dictionary<int, ResourceInfo>(dict), true);
                });
            }
        }

        public void Awake()
        {
            Instance = this;
            ServiceLocator.Register(this);
            this.content = LAB2D.Tool.Tool.GetComponentInChildren<Transform>(this.gameObject, "Content");
        }

        public void Update()
        {
            if (UnityGlobalInputAdapter.GetMiddleMouseDown() || UnityGlobalInputAdapter.GetCloseOrBuildMenuDown())
            {
                 this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
            }
            else if (UnityGlobalInputAdapter.GetPrimaryMouseDown())
            {
                List<RaycastResult> results = LAB2D.Tool.Tool.GetUIByMousePos(TagConstant.ACTION_UI_TAG);

                // 未点击到options UI, 则关闭options UI
                if (results.Count == 0)
                {
                    this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
                }
            }
        }
    }
}
