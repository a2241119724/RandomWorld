namespace LAB2D.UI.Character
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Core;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Worker 信息UI
    /// 最顶端的按钮
    /// </summary>
    public class LocateWorkerUI : MonoBehaviour
    {
        private Dictionary<AWorker, GameObject> allItems;

        /// <summary>
        /// 单例
        /// </summary>
        public static LocateWorkerUI Instance { get; private set; }

        public void Awake()
        {
            Instance = this;
            ServiceLocator.Register(this);
            this.allItems = new Dictionary<AWorker, GameObject>();
        }

        /// <summary>
        /// 添加Worker按钮
        /// </summary>
        /// <param name="worker">Worker</param>
        public void AddWorkerItem(AWorker worker)
        {
            GameObject g = ResourceManager.Instance.Instantiate(PrefabConstant.LOCATE_WORKER_ITEM, true);
            this.allItems.Add(worker, g);
            g.transform.SetParent(this.transform);
            g.transform.localScale = Vector3.one;
            LAB2D.Tool.Tool.GetComponentInChildren<Text>(g, "Name").text = worker.name;
            g.GetComponent<Button>().onClick.AddListener(() =>
            {
                Camera.main.GetComponent<CameraMove>().Character = worker;
                GameObject.FindGameObjectWithTag(TagConstant.MINIMAP_TAG).GetComponent<CameraMove>().Character = worker;
            });
        }

        /// <summary>
        /// 销毁Worker按钮
        /// </summary>
        /// <param name="worker">Worker</param>
        public void RemoveWorkerItem(AWorker worker)
        {
            if (this.allItems.ContainsKey(worker))
            {
                GameObject.Destroy(this.allItems[worker]);
                this.allItems.Remove(worker);
            }
        }
    }
}
