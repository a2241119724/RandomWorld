using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class WorkerBedUI : MonoBehaviour
    {
        public static WorkerBedUI Instance { get; private set; }

        private Transform content;
        private Text curWorker;

        private void Awake()
        {
            Instance = this;
            content = Tool.GetComponentInChildren<Transform>(gameObject, "Content");
            curWorker = Tool.GetComponentInChildren<Text>(gameObject, "CurWorker");
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2) || Input.GetKeyDown(KeyCode.Escape))
            {
                //transform.position = ResourceConstant.VECTOR3_DEFAULT;
            }
        }

        public void showWorkerBed(Vector3Int posMap)
        {
            Worker worker = FurnitureManager.Instance.getByBed(posMap);
            curWorker.text = $"��ǰ: " + (worker != null ? worker.name : "û��");
            //
            transform.position = TileMap.Instance.mapPosToWorldPos(posMap);
            List<Worker> workers = WorkerManager.Instance.Characters;
            for (int i = 0; i < workers.Count; i++)
            {
                // ��û�ж�Ӧ�����壬�ȴ���
                if (i > content.childCount - 1)
                {
                    GameObject g = GameObject.Instantiate(ResourcesManager.Instance.getPrefab("WorkerItem"));
                    g.transform.SetParent(content);
                    g.transform.localScale = Vector3.one;
                }
                Tool.GetComponentInChildren<Text>(content.GetChild(i).gameObject, "Name").text = workers[i].name;
                Button button = content.GetChild(i).gameObject.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                // Ĭ��Ϊ���ô��ݣ���Ϊ�̶�ֵ
                int index = i;
                button.onClick.AddListener(() => {
                    WorkerTaskManager.Instance.addTask(new WorkerSleepTask.SleepTaskBuilder().setTarget(posMap).build(), 1);
                    transform.position = ResourceConstant.VECTOR3_DEFAULT;
                    FurnitureManager.Instance.addWorkerToBed(posMap, workers[index]);
                });
            }
        }
    }
}
