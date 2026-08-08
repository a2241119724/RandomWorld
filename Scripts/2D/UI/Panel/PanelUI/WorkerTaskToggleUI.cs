namespace LAB2D.UI.Panel.PanelUI
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Enum;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Worker可做任务开关 UI
    /// </summary>
    public class WorkerTaskToggleUI : MonoBehaviour
    {
        /// <summary>
        /// 任务项
        /// </summary>
        public List<GameObject> TaskItems { get; set; }

        /// <summary>
        /// 在面板中挂在在所有TaskToggle上。
        /// 通过 TaskToggleBinding 组件获取 Toggle 对应的 WorkerTaskType，
        /// 不再依赖 UI 层级顺序隐式映射。
        /// </summary>
        /// <param name="toggle">开关UI</param>
        public void TaskToggle(Toggle toggle)
        {
            TaskToggleBinding binding = toggle.GetComponent<TaskToggleBinding>();
            if (binding == null)
            {
                return;
            }

            int x = toggle.transform.parent.GetSiblingIndex() - 1;
            List<AWorker> workers = ServiceLocator.Get<WorkerManager>().Characters;
            if (x < 0 || x >= workers.Count)
            {
                return;
            }

            AWorker.WorkerData workerData = workers[x].CharacterDataLAB as AWorker.WorkerData;
            if (workerData != null && workerData.TaskToggle != null)
            {
                workerData.TaskToggle[binding.TaskType] = toggle.isOn;
            }
        }

        public void Awake()
        {
            this.TaskItems = new List<GameObject>();
            for (int i = 0; i < this.transform.childCount - 1; i++)
            {
                this.TaskItems.Add(this.transform.GetChild(i + 1).gameObject);
            }
        }

        private void OnEnable()
        {
            List<AWorker> workers = ServiceLocator.Get<WorkerManager>().Characters;
            List<WorkerTaskType> taskTypeOrder = WorkerTaskTogglePanel.TaskTypeOrder;

            // UI不够,创建
            int count = workers.Count - (this.transform.childCount - 1);
            if (count > 0)
            {
                for (int i = count; i > 0; i--)
                {
                    GameObject g = ServiceLocator.Get<ResourceManager>().Instantiate(PrefabConstant.TASK_ITEM, this.transform, false);
                    this.TaskItems.Add(g);

                    // 动态生成 Toggle 列（而非依赖 prefab 中预放置的子对象）
                    this.SyncToggleColumns(g, taskTypeOrder);
                }
            }

            // 清空TaskItem
            for (int i = 0; i < this.transform.childCount - 1; i++)
            {
                this.TaskItems[i].SetActive(false);
            }

            int index = 0;
            foreach (AWorker worker in workers)
            {
                GameObject taskItem = this.TaskItems[index];
                taskItem.SetActive(true);
                LAB2D.Tool.Tool.GetComponentInChildren<Text>(taskItem.transform.GetChild(0).gameObject, "Text").text = worker.name;
                AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;

                // 确保 Toggle 列与 TaskTypeOrder 一致（处理旧 TaskItem 列数不匹配的情况）
                this.EnsureToggleColumnsSynced(taskItem, taskTypeOrder);

                // 按 TaskTypeOrder 顺序设置每个 Toggle 的值
                for (int i = 0; i < taskTypeOrder.Count; i++)
                {
                    int childIndex = i + 1; // child 0 是 Worker 名称
                    if (childIndex >= taskItem.transform.childCount)
                    {
                        break;
                    }

                    Transform toggleTransform = taskItem.transform.GetChild(childIndex);
                    Toggle toggle = toggleTransform.GetComponent<Toggle>();
                    TaskToggleBinding binding = toggleTransform.GetComponent<TaskToggleBinding>();
                    if (binding != null)
                    {
                        // 所有任务类型默认为开启，只有玩家手动关闭的才会显示为关闭
                        bool enabled = true;
                        if (workerData.TaskToggle != null)
                        {
                            workerData.TaskToggle.TryGetValue(binding.TaskType, out enabled);
                        }

                        toggle.isOn = enabled;
                    }
                }

                index++;
            }
        }

        /// <summary>
        /// 动态同步 TaskItem 的 Toggle 子对象：确保数量与 TaskTypeOrder 一致，
        /// 每个 Toggle 绑定正确的 TaskToggleBinding.TaskType 和 onValueChanged 监听器。
        /// 多余列从末尾销毁，不足列从模板 Instantiate 补齐。
        /// </summary>
        /// <param name="taskItem">TaskItem GameObject</param>
        /// <param name="taskTypeOrder">任务类型顺序列表</param>
        private void SyncToggleColumns(GameObject taskItem, List<WorkerTaskType> taskTypeOrder)
        {
            int neededCount = taskTypeOrder.Count;
            int currentToggleCount = taskItem.transform.childCount - 1; // 减去名称列

            if (currentToggleCount <= 0 && neededCount <= 0)
            {
                return;
            }

            // 保存模板（第一个 Toggle，若存在）
            GameObject template = currentToggleCount > 0
                ? taskItem.transform.GetChild(1).gameObject
                : null;

            // 销毁多余的 Toggle 子对象（从末尾往前删）
            for (int i = currentToggleCount - 1; i >= neededCount; i--)
            {
                Object.Destroy(taskItem.transform.GetChild(i + 1).gameObject);
            }

            // 创建不足的 Toggle 子对象
            for (int i = currentToggleCount; i < neededCount; i++)
            {
                GameObject newToggle;
                if (template != null)
                {
                    newToggle = Object.Instantiate(template, taskItem.transform);
                }
                else
                {
                    // 无模板时的兜底：从零创建基础 Toggle
                    newToggle = new GameObject("Toggle");
                    newToggle.AddComponent<Toggle>();
                    newToggle.transform.SetParent(taskItem.transform);
                    newToggle.transform.localScale = Vector3.one;
                }

                newToggle.name = "Toggle_" + taskTypeOrder[i].ToString();
            }

            // 设置每个 Toggle 的绑定和事件监听
            for (int i = 0; i < neededCount; i++)
            {
                Transform t = taskItem.transform.GetChild(i + 1);
                TaskToggleBinding binding = t.GetComponent<TaskToggleBinding>();
                if (binding == null)
                {
                    binding = t.gameObject.AddComponent<TaskToggleBinding>();
                }

                binding.TaskType = taskTypeOrder[i];

                // 任务开关列宽度设为50（第一列名称列保持原宽度100）
                RectTransform toggleRect = t.GetComponent<RectTransform>();
                if (toggleRect != null)
                {
                    toggleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 50f);
                }

                Toggle toggle = t.GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.onValueChanged.RemoveAllListeners();
                    Toggle capturedToggle = toggle;
                    toggle.onValueChanged.AddListener((bool isOn) =>
                    {
                        this.TaskToggle(capturedToggle);
                    });
                }
            }
        }

        /// <summary>
        /// 轻量检查：若 Toggle 列数与 TaskTypeOrder 不一致则触发完整同步。
        /// </summary>
        private void EnsureToggleColumnsSynced(GameObject taskItem, List<WorkerTaskType> taskTypeOrder)
        {
            int neededCount = taskTypeOrder.Count;
            int currentToggleCount = taskItem.transform.childCount - 1;

            if (currentToggleCount != neededCount)
            {
                this.SyncToggleColumns(taskItem, taskTypeOrder);
            }
        }
    }
}
