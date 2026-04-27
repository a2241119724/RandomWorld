namespace LAB2D
{
    using System.Text;
    using UnityEngine;
    using UnityEngine.UI;

    public class GameplayStatsUI : MonoBehaviour
    {
        private Text statsText;
        private CanvasGroup canvasGroup;

        public static GameplayStatsUI Instance { get; private set; }

        public void Awake()
        {
            Instance = this;
            this.statsText = Tool.GetComponentInChildren<Text>(this.gameObject, "StatsText");
            this.canvasGroup = this.gameObject.GetComponent<CanvasGroup>();
            if (this.canvasGroup == null)
            {
                this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void OnEnable()
        {
            GameplaySessionStats.Instance.StatsChanged += this.OnStatsChanged;
            this.Refresh();
        }

        public void OnDisable()
        {
            GameplaySessionStats.Instance.StatsChanged -= this.OnStatsChanged;
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                bool show = this.canvasGroup.alpha < 0.5f;
                this.canvasGroup.alpha = show ? 1f : 0f;
                this.canvasGroup.interactable = show;
                this.canvasGroup.blocksRaycasts = show;
            }
        }

        private void OnStatsChanged(GameplaySessionStatsSnapshot snapshot)
        {
            this.UpdateDisplay(snapshot);
        }

        private void Refresh()
        {
            this.UpdateDisplay(GameplaySessionStats.Instance.CreateSnapshot());
        }

        private void UpdateDisplay(GameplaySessionStatsSnapshot snapshot)
        {
            if (this.statsText == null)
            {
                return;
            }

            this.statsText.text = this.FormatStats(snapshot);
        }

        private string FormatStats(GameplaySessionStatsSnapshot s)
        {
            StringBuilder sb = new StringBuilder(512);
            sb.AppendLine("<color=yellow>=== 会话统计 ===</color>");
            sb.AppendLine($"<color=cyan>击杀敌人:</color> {s.TotalDefeatedEnemyCount}");
            sb.AppendLine($"<color=cyan>最大连杀:</color> {s.MaxCombo}");
            sb.AppendLine($"<color=cyan>当前连杀:</color> {s.CurrentCombo}");
            sb.AppendLine($"<color=cyan>造成伤害:</color> {s.TotalDamageDealt}");
            sb.AppendLine($"<color=cyan>受到伤害:</color> {s.TotalDamageTaken}");
            sb.AppendLine($"<color=cyan>暴击次数:</color> {s.CriticalHitCount}");
            sb.AppendLine($"<color=cyan>获得经验:</color> {s.TotalExperienceGained}");
            sb.AppendLine($"<color=cyan>收集物品:</color> {s.TotalCollectedItemCount}");
            sb.AppendLine($"<color=cyan>完成任务:</color> {s.TotalWorkerTaskCompletedCount}");
            sb.AppendLine($"<color=cyan>玩家死亡:</color> {s.PlayerDeathCount}");
            sb.AppendLine($"<color=cyan>工人死亡:</color> {s.TotalWorkerDeathCount}");
            return sb.ToString();
        }
    }
}
