using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class WorkerInfoUI : MonoBehaviour
    {
        public static WorkerInfoUI Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void addWorkerItem(Worker worker) {
            GameObject g = GameObject.Instantiate(ResourcesManager.Instance.getPrefab("WorkerItem"));
            g.transform.SetParent(transform);
            g.transform.localScale = Vector3.one;
            Tool.GetComponentInChildren<Text>(g, "Name").text = worker.name;
            g.GetComponent<Button>().onClick.AddListener(()=> {
                Camera.main.GetComponent<CameraMove>().Character = worker;
                GameObject.FindGameObjectWithTag(ResourceConstant.MINIMAP_TAG).GetComponent<CameraMove>().Character = worker;
            });
        }
    }
}
