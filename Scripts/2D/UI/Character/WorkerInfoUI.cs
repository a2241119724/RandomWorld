namespace LAB2D
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Worker 信息UI
    /// 最顶端的按钮
    /// </summary>
    public class WorkerInfoUI : MonoBehaviour
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static WorkerInfoUI Instance { get; private set; }

        /// <summary>
        /// 添加Worker按钮
        /// </summary>
        /// <param name="worker">Worker</param>
        public void AddWorkerItem(Worker worker)
        {
            GameObject g = GameObject.Instantiate(ResourceManager.Instance.GetPrefab("WorkerItem"));
            g.transform.SetParent(this.transform);
            g.transform.localScale = Vector3.one;
            Tool.GetComponentInChildren<Text>(g, "Name").text = worker.name;
            g.GetComponent<Button>().onClick.AddListener(() =>
            {
                Camera.main.GetComponent<CameraMove>().Character = worker;
                GameObject.FindGameObjectWithTag(ResourceConstant.MINIMAP_TAG).GetComponent<CameraMove>().Character = worker;
            });
        }

        private void Awake()
        {
            Instance = this;
        }
    }
}
