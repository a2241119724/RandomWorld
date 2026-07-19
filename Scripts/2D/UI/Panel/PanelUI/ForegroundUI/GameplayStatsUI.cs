namespace LAB2D.UI.Panel.PanelUI.ForegroundUI
{
    using LAB2D;
    using LAB2D.Gameplay;
    using LAB2D.UnityAdapter;
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
            this.statsText = LAB2D.Tool.Tool.GetComponentInChildren<Text>(this.gameObject, "StatsText");
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
            if (UnityGlobalInputAdapter.GetHudToggleDown(InputKeyConstant.ToggleGameplayStatsHud))
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
            sb.AppendLine("<color=" + PixelUITheme.RichGold + ">=== 会话统计 ===</color>");
            sb.AppendLine($"<color={PixelUITheme.RichSky}>击杀敌人:</color> {s.TotalDefeatedEnemyCount}");
            sb.AppendLine($"<color={PixelUITheme.RichSky}>最大连杀:</color> {s.MaxCombo}");
            sb.AppendLine($"<color={PixelUITheme.RichSky}>当前连杀:</color> {s.CurrentCombo}");
            sb.AppendLine($"<color={PixelUITheme.RichSky}>造成伤害:</color> {s.TotalDamageDealt}");
            sb.AppendLine($"<color={PixelUITheme.RichSky}>受到伤害:</color> {s.TotalDamageTaken}");
            sb.AppendLine($"<color={PixelUITheme.RichSky}>暴击次数:</color> {s.CriticalHitCount}");
            sb.AppendLine($"<color={PixelUITheme.RichSky}>获得经验:</color> {s.TotalExperienceGained}");
            sb.AppendLine($"<color={PixelUITheme.RichSky}>收集物品:</color> {s.TotalCollectedItemCount}");
            sb.AppendLine($"<color={PixelUITheme.RichSky}>完成任务:</color> {s.TotalWorkerTaskCompletedCount}");
            sb.AppendLine($"<color={PixelUITheme.RichSky}>玩家死亡:</color> {s.PlayerDeathCount}");
            sb.AppendLine($"<color={PixelUITheme.RichSky}>工人死亡:</color> {s.TotalWorkerDeathCount}");
            return sb.ToString();
        }
    }
}
