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
        /// 心智层「强制命令」按钮的节点名（列计数需排除它，避免被当作 Toggle 列）。
        /// </summary>
        private const string ForceButtonName = "ForceCommand";

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

                // 心智层：每行末尾追加「强制命令」按钮
                // （玩家兜底逃生阀：强制放行 60s 必接玩家悬赏，代价是怨恨/信任/好感）
                this.EnsureForceCommandButton(taskItem, worker);

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
            // 列计数排除末尾的「强制命令」按钮（ForceCommand 非 Toggle 列）
            int currentToggleCount = CountToggleColumns(taskItem);

            if (currentToggleCount <= 0 && neededCount <= 0)
            {
                return;
            }

            // 保存模板（第一个 Toggle，若存在）
            GameObject template = currentToggleCount > 0
                ? taskItem.transform.GetChild(1).gameObject
                : null;

            // 销毁多余的 Toggle 子对象（从末尾往前删，只删 Toggle 列，不影响末尾按钮）
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

                // 插入到正确列位（i+1），避免落在末尾「强制命令」按钮之后破坏列连续性
                newToggle.transform.SetSiblingIndex(i + 1);
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
            int currentToggleCount = CountToggleColumns(taskItem);

            if (currentToggleCount != neededCount)
            {
                this.SyncToggleColumns(taskItem, taskTypeOrder);
            }
        }

        /// <summary>
        /// 统计 TaskItem 的 Toggle 列数：child 0 是名称，末尾可能有一个「强制命令」按钮，
        /// 两者都不算 Toggle 列。
        /// </summary>
        private static int CountToggleColumns(GameObject taskItem)
        {
            int count = 0;
            for (int i = 1; i < taskItem.transform.childCount; i++)
            {
                if (taskItem.transform.GetChild(i).name != ForceButtonName)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 心智层：每行末尾追加「强制命令」按钮（幂等，以节点名标识）。
        /// 点击对该 Worker 执行 ForceCommand：放行窗口内必接玩家悬赏，代价是怨恨/信任/好感。
        /// </summary>
        private void EnsureForceCommandButton(GameObject taskItem, AWorker worker)
        {
            if (taskItem == null || worker == null)
            {
                return;
            }

            if (taskItem.transform.Find(ForceButtonName) != null)
            {
                return;
            }

            GameObject buttonGo = ServiceLocator.Get<ResourceManager>().Instantiate(
                PrefabConstant.BUTTON_ITEM, taskItem.transform, false);
            if (buttonGo == null)
            {
                return;
            }

            buttonGo.name = ForceButtonName;

            Text text = buttonGo.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = "强制";
            }

            RectTransform rect = buttonGo.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 50f);
            }

            Button button = buttonGo.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => this.OnForceCommandClicked(worker));
            }
        }

        /// <summary>
        /// 强制命令按钮点击：强制该 Worker 必接玩家悬赏（60s 窗口），怨恨/信任/好感受损。
        /// </summary>
        private void OnForceCommandClicked(AWorker worker)
        {
            if (worker == null)
            {
                return;
            }

            if (Core.ServiceLocator.TryGet<WorkerMindService>(out WorkerMindService mindService))
            {
                mindService.ForceCommand(worker);
            }
            else
            {
                AWorkerTask.LogProvider(
                    "[MindDiag] WorkerMindService 未注册，强制命令不可用",
                    LogManager.LogLevelEnum.Warning);
            }
        }
    }
}
