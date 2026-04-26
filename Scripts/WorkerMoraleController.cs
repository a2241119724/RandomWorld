using UnityEngine;
using UnityEngine.Events;

namespace LAB2D.AgentGenerated
{
    /// <summary>
    /// Standalone runtime morale component for worker entities.
    /// Handles morale decay/recovery over time, threshold‑based events, and task readiness.
    /// Attach manually to a GameObject representing a worker.
    /// </summary>
    public class WorkerMoraleController : MonoBehaviour
    {
        [Header("Morale Settings")]
        [Tooltip("Maximum possible morale value.")]
        [Range(1f, 1000f)]
        [SerializeField] private float maxMorale = 100f;

        [Tooltip("Current morale value. Bounded between 0 and maxMorale.")]
        [Range(0f, 1000f)]
        [SerializeField] private float currentMorale = 100f;

        [Tooltip("Morale lost per second when no recovery is active.")]
        [Range(0f, 100f)]
        [SerializeField] private float decayRate = 1f;

        [Tooltip("Morale gained per second when recovery conditions are met (e.g., resting).")]
        [Range(0f, 100f)]
        [SerializeField] private float recoveryRate = 2f;

        [Header("Thresholds")]
        [Tooltip("Morale below this value triggers the OnLowMorale event and disables task readiness.")]
        [Range(0f, 1000f)]
        [SerializeField] private float lowMoraleThreshold = 30f;

        [Tooltip("Morale below this value triggers the OnCriticalMorale event. Should be lower than lowMoraleThreshold.")]
        [Range(0f, 1000f)]
        [SerializeField] private float criticalMoraleThreshold = 10f;

        [Header("Events")]
        [Tooltip("Invoked when morale drops below lowMoraleThreshold.")]
        public UnityEvent OnLowMorale;

        [Tooltip("Invoked when morale drops below criticalMoraleThreshold.")]
        public UnityEvent OnCriticalMorale;

        [Tooltip("Invoked when morale recovers from below lowMoraleThreshold back to or above it.")]
        public UnityEvent OnMoraleRecovered;

        // Tracks whether the low/critical events have already been fired to avoid repeated calls.
        private bool lowMoraleFired;
        private bool criticalMoraleFired;

        /// <summary>
        /// Provides external systems with a simple check for task readiness.
        /// A worker is considered ready when morale is at or above the low threshold.
        /// </summary>
        public bool IsReadyForTasks()
        {
            return currentMorale >= lowMoraleThreshold;
        }

        /// <summary>
        /// Public accessor for current morale (read‑only).
        /// </summary>
        public float CurrentMorale => currentMorale;

        private void Update()
        {
            ApplyChanges();
            ClampMorale();
            CheckThresholds();
        }

        /// <summary>
        /// Apply decay or recovery. By default, morale always decays.
        /// Override or extend this method if you want custom recovery triggers.
        /// </summary>
        private void ApplyChanges()
        {
            // Simple always‑decay behaviour; toggle recovery via inspector or derived classes.
            // Example: if you want recovery when a worker is resting, control the sign externally
            // by changing recoveryRate or adding a separate 'isRecovering' field.
            float delta = -decayRate * Time.deltaTime; // Decay is default
            // Uncomment the line below if you want a simple toggle in the inspector for recovery.
            // (Add a [SerializeField] private bool isRecovering; field to the class.)
            // if (isRecovering) delta = recoveryRate * Time.deltaTime;
            
            currentMorale += delta;
        }

        private void ClampMorale()
        {
            currentMorale = Mathf.Clamp(currentMorale, 0f, maxMorale);
        }

        private void CheckThresholds()
        {
            // Handle critical threshold
            if (currentMorale < criticalMoraleThreshold && !criticalMoraleFired)
            {
                criticalMoraleFired = true;
                OnCriticalMorale?.Invoke();
            }
            else if (currentMorale >= criticalMoraleThreshold && criticalMoraleFired)
            {
                criticalMoraleFired = false;
            }

            // Handle low threshold and recovery
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