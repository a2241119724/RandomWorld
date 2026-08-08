namespace LAB2D.UI.Action
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using LAB2D.Serializable;
    using LAB2D.UnityAdapter;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 拆除建筑对应的 UI
    /// </summary>
    public class DemolishUI : MonoBehaviour
    {
        private Vector3Int posMap;

        /// <summary>
        /// 单例
        /// </summary>
        public static DemolishUI Instance { get; private set; }

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
        /// 设置拆除 UI 的位置
        /// </summary>
        /// <param name="posMap">位置</param>
        public void SetPosition(Vector3Int posMap)
        {
            this.posMap = posMap;
            this.transform.position = ServiceLocator.Get<TileMap>().MapPosToWorldPos(posMap);
        }

        /// <summary>
        /// 隐藏拆除 UI。
        /// </summary>
        public void Hide()
        {
            this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
        }

        /// <summary>
        /// 确定拆除
        /// </summary>
        public void Onclick_Yes()
        {
            this.Hide();
            GameGridPosition gridPos = UnityVectorAdapter.ToGameGridPosition(this.posMap);
            if (ServiceLocator.Get<WorkerTaskManager>().DemolishPositions.Contains(gridPos))
            {
                return;
            }

            ServiceLocator.Get<WorkerTaskManager>().AddTask(
                new WorkerDemolishTask.DemolishTaskBuilder()
                .SetTarget(this.posMap).Build(), new GameGridPosition(this.posMap.x, this.posMap.y, this.posMap.z));
        }

        /// <summary>
        /// 取消拆除
        /// </summary>
        public void Onclick_No()
        {
            this.Hide();
            GameGridPosition gridPos = UnityVectorAdapter.ToGameGridPosition(this.posMap);
            if (!ServiceLocator.Get<WorkerTaskManager>().DemolishPositions.Contains(gridPos))
            {
                return;
            }

            ServiceLocator.Get<WorkerTaskManager>().CancelDemolishTask(gridPos);
            ServiceLocator.Get<GatherMap>().CancelDemolish(this.posMap);
        }
    }
}
