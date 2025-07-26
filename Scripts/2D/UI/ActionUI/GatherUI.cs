namespace LAB2D
{
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
        /// 确定采集
        /// </summary>
        public void Onclick_Yes()
        {
            this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
            if (WorkerTaskManager.Instance.GatherPos.Contains(this.posMap))
            {
                return;
            }

            WorkerTaskManager.Instance.AddTask(new WorkerGatherTask.GatherTaskBuilder()
                .setTarget(this.posMap).setGatherName("Tree").build());
        }

        /// <summary>
        /// 取消采集
        /// </summary>
        public void Onclick_No()
        {
            this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
            if (!WorkerTaskManager.Instance.GatherPos.Contains(this.posMap))
            {
                return;
            }

            WorkerTaskManager.Instance.CancelGatherTask(this.posMap);
            GatherMap.Instance.CancelGather(this.posMap);
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Tool.GetComponentInChildren<Button>(this.gameObject, "Yes").onClick.AddListener(this.Onclick_Yes);
            Tool.GetComponentInChildren<Button>(this.gameObject, "No").onClick.AddListener(this.Onclick_No);
        }

        private void Update()
        {
            // 若是不在默认位置，则才返回默认位置
            if (Input.GetMouseButtonDown(1) && this.transform.position.x != ResourceConstant.VECTOR3_DEFAULT.x)
            {
                this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
            }
        }
    }
}
