namespace LAB2D.UI.Panel.PanelUI.ForegroundUI
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Worker;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 调试 UI — 通过 EventBus 订阅 WorkerTaskQueueChangedEvent 实现解耦更新。
    /// </summary>
    public class DebugUI : MonoBehaviour
    {
        private Text text;

        /// <summary>
        /// 单例
        /// </summary>
        public static DebugUI Instance { get; private set; }

        /// <summary>
        /// 更新信息（保留兼容旧调用方，如 InventoryManager）。
        /// </summary>
        /// <param name="text">信息</param>
        public void UpdateInfo(string text)
        {
            if (this.text != null)
            {
                this.text.text = text;
            }
        }

        public void Awake()
        {
            this.text = Tool.Tool.GetComponentInChildren<Text>(this.gameObject, "Info");
            Instance = this;
            EventBus.Instance.Subscribe<WorkerTaskQueueChangedEvent>(this.OnTaskQueueChanged);
        }

        public void OnDestroy()
        {
            EventBus.Instance.Unsubscribe<WorkerTaskQueueChangedEvent>(this.OnTaskQueueChanged);
        }

        private void OnTaskQueueChanged(WorkerTaskQueueChangedEvent e)
        {
            this.UpdateInfo(e.TaskInfo);
        }
    }
}
