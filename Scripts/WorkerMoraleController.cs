using UnityEngine;
using UnityEngine.Events;

namespace LAB2D.AgentGenerated
{
    /// <summary>
    /// 工人实体的独立运行时士气组件。
    /// 处理士气随时间衰减/恢复、基于阈值的事件和任务就绪状态。
    /// 手动附加到代表工人的 GameObject 上。
    /// </summary>
    public class WorkerMoraleController : MonoBehaviour
    {
        [Header("Morale Settings")]
        [Tooltip("最大可能的士气值。")]
        [Range(1f, 1000f)]
        [SerializeField] private float maxMorale = 100f;

        [Tooltip("当前士气值。范围在 0 到 maxMorale 之间。")]
        [Range(0f, 1000f)]
        [SerializeField] private float currentMorale = 100f;

        [Tooltip("无恢复激活时每秒损失的士气值。")]
        [Range(0f, 100f)]
        [SerializeField] private float decayRate = 1f;

        [Header("Thresholds")]
        [Tooltip("士气低于此值时触发 OnLowMorale 事件并禁用任务就绪状态。")]
        [Range(0f, 1000f)]
        [SerializeField] private float lowMoraleThreshold = 30f;

        [Tooltip("士气低于此值时触发 OnCriticalMorale 事件。应低于 lowMoraleThreshold。")]
        [Range(0f, 1000f)]
        [SerializeField] private float criticalMoraleThreshold = 10f;

        [Header("Events")]
        [Tooltip("当士气降至 lowMoraleThreshold 以下时触发。")]
        public UnityEvent OnLowMorale;

        [Tooltip("当士气降至 criticalMoraleThreshold 以下时触发。")]
        public UnityEvent OnCriticalMorale;

        [Tooltip("当士气从 lowMoraleThreshold 以下恢复到该值或以上时触发。")]
        public UnityEvent OnMoraleRecovered;

        // 跟踪低/危急事件是否已被触发，以避免重复调用。
        private bool lowMoraleFired;
        private bool criticalMoraleFired;

        /// <summary>
        /// 为外部系统提供简单的任务就绪状态检查。
        /// 当士气达到或高于低阈值时，工人被视为就绪。
        /// </summary>
        public bool IsReadyForTasks()
        {
            return currentMorale >= lowMoraleThreshold;
        }

        /// <summary>
        /// 当前士气的公共访问器（只读）。
        /// </summary>
        public float CurrentMorale => currentMorale;

        private void Update()
        {
            ApplyChanges();
            ClampMorale();
            CheckThresholds();
        }

        private void ApplyChanges()
        {
            currentMorale -= decayRate * Time.deltaTime;
        }

        private void ClampMorale()
        {
            currentMorale = Mathf.Clamp(currentMorale, 0f, maxMorale);
        }

        private void CheckThresholds()
        {
            // 处理危急阈值
            if (currentMorale < criticalMoraleThreshold && !criticalMoraleFired)
            {
                criticalMoraleFired = true;
                OnCriticalMorale?.Invoke();
            }
            else if (currentMorale >= criticalMoraleThreshold && criticalMoraleFired)
            {
                criticalMoraleFired = false;
            }

            // 处理低阈值和恢复
            if (currentMorale < lowMoraleThreshold && !lowMoraleFired)
            {
                lowMoraleFired = true;
                OnLowMorale?.Invoke();
            }
            else if (currentMorale >= lowMoraleThreshold && lowMoraleFired)
            {
                lowMoraleFired = false;
                OnMoraleRecovered?.Invoke();
            }
        }
    }
}