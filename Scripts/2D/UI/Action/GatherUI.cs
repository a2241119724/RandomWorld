namespace LAB2D.UI.Action
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using LAB2D.Serializable;
    using LAB2D.UnityAdapter;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 采集对应的UI
    /// </summary>
    public class GatherUI : MonoBehaviour
    {
        private Vector3Int posMap;

        /// <summary>
        /// 单例
        /// </summary>
        public static GatherUI Instance { get; private set; }

        public void Awake()
        {
            Instance = this;
            ServiceLocator.Register(this);
        }

        public void Start()
        {
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.gameObject, "Yes").onClick.AddListener(this.Onclick_Yes);
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.gameObject, "No").onClick.AddListener(this.Onclick_No);
        }

        public void Update()
        {
            // 若是不在默认位置，则才返回默认位置
            if (UnityGlobalInputAdapter.GetSecondaryMouseDown() && this.transform.position.x != ResourceConstant.VECTOR3_DEFAULT.x)
            {
                this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
            }
        }

        /// <summary>
        /// 设置采集UI的位置
        /// </summary>
        /// <param name="posMap">位置</param>
        public void SetPostion(Vector3Int posMap)
        {
            this.posMap = posMap;
            this.transform.position = ServiceLocator.Get<TileMap>().MapPosToWorldPos(posMap);
        }

        /// <summary>
        /// 隐藏采集UI。
        /// </summary>
        public void Hide()
        {
            this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
        }

        /// <summary>
        /// 确定采集
        /// </summary>
        public void Onclick_Yes()
        {
            this.Hide();
            GameGridPosition gridPos = UnityVectorAdapter.ToGameGridPosition(this.posMap);
            if (ServiceLocator.Get<WorkerTaskManager>().GatherPositions.Contains(gridPos))
            {
                return;
            }

            if (!ServiceLocator.Get<ResourceMap>().TryGetGatherResourceInfo(this.posMap, out ResourceInfo resourceInfo))
            {
                return;
            }

            ServiceLocator.Get<WorkerTaskManager>().AddTask(
                new WorkerGatherTask.GatherTaskBuilder()
                .SetTarget(this.posMap).SetResourceInfo(resourceInfo).Build(), new GameGridPosition(this.posMap.x, this.posMap.y, this.posMap.z));
        }

        /// <summary>
        /// 取消采集
        /// </summary>
        public void Onclick_No()
        {
            this.Hide();
            GameGridPosition gridPos = UnityVectorAdapter.ToGameGridPosition(this.posMap);
            if (!ServiceLocator.Get<WorkerTaskManager>().GatherPositions.Contains(gridPos))
            {
                return;
            }

            ServiceLocator.Get<WorkerTaskManager>().CancelGatherTask(gridPos);
            ServiceLocator.Get<GatherMap>().CancelGather(this.posMap);
        }
    }
}
