namespace LAB2D.Render
{
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Time;
    using LAB2D.Manager;
    using UnityEngine;
    using UnityEngine.Rendering.Universal;

    /// <summary>
    /// 玩家夜间随身光晕 — 订阅 <see cref="GamePhaseChangedEvent"/>：入夜/黄昏淡入（0.55）、
    /// 白天/黎明淡出（0），线性过渡约 2s。Point 暖光（1,0.85,0.6）Additive、半径 ~2.8 格，
    /// 保证夜间玩家周边经营可视。挂玩家子 GO 运行时创建（不碰 prefab，无 AB 重打包）。
    /// 夜里进游戏时按 <see cref="GameTimeManager.CurrentPhase"/> 直接亮起，不等相位切换。
    /// </summary>
    public class PlayerNightGlow : MonoBehaviour
    {
        /// <summary>全局开关（false 时强制熄灭且不再淡入）。</summary>
        public static bool Enabled = true;

        private const float NightIntensity = 0.55f;
        private const float DayIntensity = 0f;
        private const float FadeDuration = 2f; // 秒（0→0.55 全程）
        private const float GlowRadius = 2.8f;

        private Light2D glow;
        private float target;

        private void Awake()
        {
            this.glow = this.gameObject.AddComponent<Light2D>();
            this.glow.lightType = Light2D.LightType.Point;
            this.glow.blendStyleIndex = 1; // Additive：暖光叠加在全局光之上
            this.glow.color = new Color(1f, 0.85f, 0.6f, 1f);
            this.glow.pointLightOuterRadius = GlowRadius;
            this.glow.pointLightInnerRadius = GlowRadius * 0.4f;
            this.glow.falloffIntensity = 0.5f;
            this.glow.intensity = DayIntensity;
            this.target = DayIntensity;
        }

        private void Start()
        {
            // 夜里进游戏：按当前相位直接点亮（相位切换事件只在变化时才来）
            GamePhase phase = Core.ServiceLocator.Get<GameTimeManager>().CurrentPhase;
            this.OnPhaseChanged(new GamePhaseChangedEvent { NewPhase = phase });
            if (this.target > 0f && Enabled)
            {
                this.glow.intensity = this.target; // 初始即亮，不再从 0 淡入
            }
        }

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<GamePhaseChangedEvent>(this.OnPhaseChanged);
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<GamePhaseChangedEvent>(this.OnPhaseChanged);
        }

        private void OnPhaseChanged(GamePhaseChangedEvent e)
        {
            this.target = e.NewPhase == GamePhase.Night || e.NewPhase == GamePhase.Dusk
                ? NightIntensity
                : DayIntensity;
        }

        private void Update()
        {
            float goal = Enabled ? this.target : DayIntensity;
            if (!Mathf.Approximately(this.glow.intensity, goal))
            {
                this.glow.intensity = Mathf.MoveTowards(
                    this.glow.intensity, goal, (NightIntensity / FadeDuration) * Time.deltaTime);
            }
        }
    }
}
