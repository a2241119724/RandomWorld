using System.Collections;
using System.Collections.Generic;
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
            if(Input.GetMouseButtonDown(1) && transform.position.x != -999)
            {
                transform.position = new Vector3(-999, 0, 0);
            }
        }

        public void setPostion(Vector3Int posMap)
        {
            this.posMap = posMap;
            transform.position = TileMap.Instance.mapPosToWorldPos(posMap);
        }

        public void Onclick_Yes()
        {
            transform.position = new Vector3(-999, 0, 0);
            if (WorkerTaskManager.Instance.GatherPos.Contains(posMap)) return;
            WorkerTaskManager.Instance.addTask(new WorkerGatherTask.GatherTaskBuilder()
                .setTarget(posMap).setGatherName("Tree").build());
        }

        public void Onclick_No()
        {
            transform.position = new Vector3(-999, 0, 0);
            if (!WorkerTaskManager.Instance.GatherPos.Contains(posMap)) return;
            WorkerTaskManager.Instance.cancelGatherTask(posMap);
            GatherMap.Instance.cancelGather(posMap);
        }
    }
}
