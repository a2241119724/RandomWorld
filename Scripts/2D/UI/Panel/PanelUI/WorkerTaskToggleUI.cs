using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LAB2D
{
    public class WorkerTaskToggleUI : MonoBehaviour
    {
        public List<GameObject> taskItems;

        private void Awake()
        {
            taskItems = new List<GameObject>();
            for (int i = 0; i < transform.childCount - 1; i++)
            {
                taskItems.Add(transform.GetChild(i + 1).gameObject);
            }
        }

        private void OnEnable()
        {
            List<Worker> workers = WorkerManager.Instance.Characters;
            // UI不够,创建
            int count = workers.Count - (transform.childCount - 1);
            if (count>0)
            {
                for(int i = count; i > 0; i--)
                {
                    GameObject g = GameObject.Instantiate(ResourcesManager.Instance.getPrefab("TaskItem"), transform ,false);
                    g.name = "TaskItem";
                    taskItems.Add(g);
                    // 添加事件
                    for(int j=1;j< g.transform.childCount; j++)
                    {
                        Toggle toggle = g.transform.GetChild(j).GetComponent<Toggle>();
                        toggle.onValueChanged.AddListener((bool isOn) =>
                        {
                            taskToggle(toggle);
                        });
                    }
                }
            }
            // 清空TaskItem
            for (int i=0; i < transform.childCount-1; i++)
            {
                taskItems[i].SetActive(false);
            }
            int index = 0;
            foreach(Worker worker in workers)
            {
                taskItems[index].SetActive(true);
                Tool.GetComponentInChildren<Text>(taskItems[index].transform.GetChild(0).gameObject, "Text").text = worker.name;
                for (int i = 1; i < taskItems[index].transform.childCount; i++)
                {
                    taskItems[index].transform.GetChild(i).GetComponent<Toggle>().isOn = worker.TaskToggle[i - 1];
                }
                index++;
            }
        }

        /// <summary>
        /// 在面板中挂在在所有TaskToggle上
        /// </summary>
        /// <param name="toggle"></param>
        public void taskToggle(Toggle toggle) {
            int x = toggle.transform.parent.GetSiblingIndex()-1;
            int y = toggle.transform.GetSiblingIndex()-1;
            List<Worker> workers = WorkerManager.Instance.Characters;
            workers[x].TaskToggle[y] = toggle.isOn;
        }
    }
}
