namespace LAB2D.UI.Panel.PanelUI
{
    using LAB2D;
    using LAB2D.Enum;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 挂载在 TaskToggle 按钮上，标记该 Toggle 对应的 WorkerTaskType。
    /// 供 WorkerTaskToggleUI 在 Toggle 值变更时查找对应字典键，解除 UI 层级顺序依赖。
    /// </summary>
    public class TaskToggleBinding : MonoBehaviour
    {
        /// <summary>
        /// 该 Toggle 对应的任务类型。
        /// </summary>
        public WorkerTaskType TaskType;
    }
}
