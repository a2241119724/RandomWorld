namespace LAB2D.UI.Action
{
    using LAB2D;
    using LAB2D.Serializable;
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
        }

        public void Start()
        {
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.gameObject, "Yes").onClick.AddListener(this.Onclick_Yes);
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.gameObject, "No").onClick.AddListener(this.Onclick_No);
        }

        public void Update()
        {
            // 若是不在默认位置，则才返回默认位置
            if (Input.GetMouseButtonDown(1) && this.transform.position.x != ResourceConstant.VECTOR3_DEFAULT.x)
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
            this.transform.position = TileMap.Instance.MapPosToWorldPos(posMap);
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
            if (WorkerTaskManager.Instance.GatherPos.Contains(this.posMap))
            {
                return;
            }

            if (!ResourceMap.Instance.TryGetGatherResourceInfo(this.posMap, out ResourceInfo resourceInfo))
            {
                return;
            }

            WorkerTaskManager.Instance.AddTask(
                new WorkerGatherTask.GatherTaskBuilder()
                .SetTarget(this.posMap).SetResourceInfo(resourceInfo).Build(), Vector3IntLAB.ToVector3IntLAB(this.posMap));
        }

        /// <summary>
        /// 取消采集
        /// </summary>
        public void Onclick_No()
        {
            this.Hide();
            if (!WorkerTaskManager.Instance.GatherPos.Contains(this.posMap))
            {
                return;
            }

            WorkerTaskManager.Instance.CancelGatherTask(this.posMap);
            GatherMap.Instance.CancelGather(this.posMap);
        }
    }
}
