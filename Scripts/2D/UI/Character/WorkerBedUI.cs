namespace LAB2D.UI.Character
{
    using LAB2D;
    using LAB2D.Constant;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
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
            AWorker worker = ServiceLocator.Get<FurnitureManager>().GetWorkerByBed(posMap);
            this.curWorker.text = $"当前: " + (worker != null ? worker.name : "没人");

            this.transform.position = ServiceLocator.Get<TileMap>().MapPosToWorldPos(posMap);
            List<AWorker> workers = ServiceLocator.Get<WorkerManager>().Characters;
            for (int i = 0; i < workers.Count; i++)
            {
                // 若没有对应的物体，先创建
                if (i > this.content.childCount - 1)
                {
                    GameObject g = ServiceLocator.Get<ResourceManager>().Instantiate(PrefabConstant.WORKER_BED_ITEM);
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
                        new WorkerSleepTask.SleepTaskBuilder()
                        .SetTarget(posMap).SetWorker(workers[index]).Build(), new GameGridPosition(posMap.x, posMap.y, posMap.z),
                        WorkerTaskPriority.PlayerBounty);
                    this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
                    ServiceLocator.Get<FurnitureManager>().AddWorkerToBed(posMap, workers[index]);
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
            // 左键/右键点击空白处时将选择列表移到屏幕外（隐藏）
            // 点击在 UI 元素上时不隐藏，避免误吞按钮点击
            if ((UnityGlobalInputAdapter.GetPrimaryMouseDown() || UnityGlobalInputAdapter.GetSecondaryMouseDown())
                && this.transform.position.x != ResourceConstant.VECTOR3_DEFAULT.x
                && this.IsClickOnEmptySpace())
            {
                this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
            }
        }

        /// <summary>
        /// 检测当前鼠标点击是否在空白处（非 UI 元素上）
        /// </summary>
        private bool IsClickOnEmptySpace()
        {
            var uiResults = LAB2D.Tool.Tool.GetUIByMousePos(TagConstant.UI_TAG);
            if (uiResults.Count > 0 && uiResults[0].gameObject.name != "Foreground")
            {
                return false;
            }

            var actionResults = LAB2D.Tool.Tool.GetUIByMousePos(TagConstant.ACTION_UI_TAG);
            if (actionResults.Count > 0 && actionResults[0].gameObject.name != "Foreground")
            {
                return false;
            }

            return true;
        }
    }
}
