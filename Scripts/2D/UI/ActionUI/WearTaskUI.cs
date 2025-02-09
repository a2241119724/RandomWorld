using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class WearTaskUI : MonoBehaviour
    {
        public static WearTaskUI Instance { get; private set; }

        private Transform content;

        private void Awake()
        {
            Instance = this;
            content = Tool.GetComponentInChildren<Transform>(gameObject,"Content");
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2) || Input.GetKeyDown(KeyCode.Escape))
            {
                //transform.position = ResourceConstant.VECTOR3_DEFAULT;
            }
        }

        public void showWearTask(Vector3Int posMap) {
            transform.position = TileMap.Instance.mapPosToWorldPos(posMap);
            List<Worker> workers = WorkerManager.Instance.Characters;
            ResourceInfo resourceInfo = InventoryManager.Instance.getByPos(posMap);
            // ��λ��û�ж�����չʾ�κζ���
            if (resourceInfo == null) return;
            for (int i = 0;i < workers.Count; i++)
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
                    WorkerTaskManager.Instance.addTask(new WorkerWearTask.WearTaskBuilder()
                        .setWorker(workers[index]).setTarget(posMap).setEquipmentId(resourceInfo.id).build(), 1);
                    transform.position = ResourceConstant.VECTOR3_DEFAULT;
                    Dictionary<int, ResourceInfo> dict = new Dictionary<int, ResourceInfo>();
                    dict.Add(resourceInfo.id, resourceInfo);
                    InventoryManager.Instance.isEnoughAndPreTake(workers[index], new NeedResource(dict), true);
                });
            }
        }
    }
}
