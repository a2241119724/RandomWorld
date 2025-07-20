using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class GatherUI : MonoBehaviour
    {
        public static GatherUI Instance { get; private set; }

        private Vector3Int posMap;

        private void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            Tool.GetComponentInChildren<Button>(gameObject, "Yes").onClick.AddListener(Onclick_Yes);
            Tool.GetComponentInChildren<Button>(gameObject, "No").onClick.AddListener(Onclick_No);
        }

        private void Update()
        {
            // 若是不在默认位置，则才返回默认位置
            if (Input.GetMouseButtonDown(1) && transform.position.x != ResourceConstant.VECTOR3_DEFAULT.x)
            {
                transform.position = ResourceConstant.VECTOR3_DEFAULT;
            }
        }

        public void setPostion(Vector3Int posMap)
        {
            this.posMap = posMap;
            transform.position = TileMap.Instance.MapPosToWorldPos(posMap);
        }

        public void Onclick_Yes()
        {
            transform.position = ResourceConstant.VECTOR3_DEFAULT;
            if (WorkerTaskManager.Instance.GatherPos.Contains(posMap)) return;
            WorkerTaskManager.Instance.AddTask(new WorkerGatherTask.GatherTaskBuilder()
                .setTarget(posMap).setGatherName("Tree").build());
        }

        public void Onclick_No()
        {
            transform.position = ResourceConstant.VECTOR3_DEFAULT;
            if (!WorkerTaskManager.Instance.GatherPos.Contains(posMap)) return;
            WorkerTaskManager.Instance.CancelGatherTask(posMap);
            GatherMap.Instance.CancelGather(posMap);
        }
    }
}
