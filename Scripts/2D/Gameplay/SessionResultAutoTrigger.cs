namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Gameplay;
    using System;
    using UnityEngine;

    /// <summary>
    /// 会话结算自动触发器 — 在关键游戏节点（玩家死亡、波次通关）自动采集结算数据。
    /// 桥接 SessionResultManager 和游戏生命周期，补齐结算系统缺失的自动触发链路。
    ///
    /// 触发节点：
    ///   1. 玩家死亡：Player.Death() → NotifyPlayerDeath() → CaptureResult()
    ///   2. 波次通关：WaveManager.OnAllWavesCleared → CaptureResult()
    ///
    /// 接入方式：
    ///   - 运行时自动：SessionResultAutoTrigger 在 Start 时订阅 WaveManager 事件
    ///   - Player.cs 需新增 1 行调用 NotifyPlayerDeath（Player.Death 方法内）
    ///   - Editor 菜单：工具 > 结算自动触发 系列
    ///   - 完全降级保护：所有依赖缺失时静默跳过，不抛异常
    ///
    /// 风险边界：不修改 Scene/Prefab/SO/存档/Photon，仅作为运行时事件桥接层。
    /// </summary>
    public class SessionResultAutoTrigger : MonoBehaviour
    {
        /// <summary>
        /// 自动采集完成事件 — 可供 HUD、成就系统、存档系统等外部模块监听
        /// </summary>
        public event Action<SessionResultData> OnAutoCaptureResult;

        /// <summary>是否启用玩家死亡时的自动采集</summary>
        public bool captureOnPlayerDeath = true;

        /// <summary>是否启用波次通关时的自动采集</summary>
        public bool captureOnAllWavesCleared = true;

        /// <summary>自动采集后是否显示 Tip 摘要</summary>
        public bool showResultTipOnCapture = true;

        /// <summary>单例实例引用（不依赖 Singleton 基类，由 GlobalInit 或手动挂载）</summary>
        private static SessionResultAutoTrigger instance;

        /// <summary>获取当前实例（可能为 null）</summary>
        public static SessionResultAutoTrigger Instance
        {
            get { return instance; }
        }

        /// <summary>是否有可用的 WaveManager 事件订阅</summary>
        private bool waveSubscribed = false;

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            this.TrySubscribeWaveEvents();
        }

        private void OnDestroy()
        {
            this.UnsubscribeWaveEvents();
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// 尝试订阅 WaveManager 的波次事件。
        /// WaveManager 可能尚未初始化，因此延迟到 Start 后仍可通过外部调用重试。
        /// </summary>
        public void TrySubscribeWaveEvents()
        {
            if (this.waveSubscribed)
            {
                return;
            }

            WaveManager wm = Core.ServiceLocator.Get<WaveManager>();
            if (wm == null)
            {
                return;
            }

            wm.OnAllWavesCleared += this.HandleAllWavesCleared;
            this.waveSubscribed = true;
        }

        /// <summary>
        /// 取消订阅 WaveManager 事件
        /// </summary>
        private void UnsubscribeWaveEvents()
        {
            if (!this.waveSubscribed)
            {
                return;
            }

            WaveManager wm = Core.ServiceLocator.Get<WaveManager>();
            if (wm != null)
            {
                wm.OnAllWavesCleared -= this.HandleAllWavesCleared;
            }

            this.waveSubscribed = false;
        }

        /// <summary>
        /// 玩家死亡时调用（由 Player.Death() 调用）。
        /// 无论依赖是否就绪，此方法安全可调用——所有依赖缺失时静默跳过。
        /// </summary>
        public static void NotifyPlayerDeath()
        {
            if (instance == null)
            {
                // SessionResultAutoTrigger 未挂载到场景中，直接调用 SessionResultManager
                TryCaptureDirect();
                return;
            }

            if (!instance.captureOnPlayerDeath)
            {
                return;
            }

            TryCaptureWithFeedback();
        }

        /// <summary>
        /// 波次全部清除时的事件处理器
        /// </summary>
        private void HandleAllWavesCleared(int totalWaves)
        {
            if (!this.captureOnAllWavesCleared)
            {
                return;
            }

            TryCaptureWithFeedback();
        }

        /// <summary>
        /// 尝试采集结算数据并反馈。
        /// 所有依赖缺失时静默降级，不抛异常。
        /// </summary>
        private static void TryCaptureWithFeedback()
        {
            SessionResultData result = TryCaptureDirect();
            if (result != null && instance != null)
            {
                instance.OnAutoCaptureResult?.Invoke(result);

                if (instance.showResultTipOnCapture)
                {
                    ShowCaptureTip(result);
                }
            }
        }

        /// <summary>
        /// 直接采集结算数据（静态方法，不依赖实例存在）
        /// </summary>
        /// <returns>采集到的结算数据，失败返回 null</returns>
        private static SessionResultData TryCaptureDirect()
        {
            if (!UnityEngine.Application.isPlaying)
            {
                return null;
            }

            SessionResultManager srm = Core.ServiceLocator.Get<SessionResultManager>();
            if (srm == null)
            {
                return null;
            }

            SessionResultData result = srm.CaptureResult();
            if (result == null)
            {
                return null;
            }

            // 输出结算摘要到控制台
            Debug.Log($"[SessionResultAutoTrigger] 自动采集会话结算数据 — 评分: {result.CombatScore}, " +
                      $"星级: {result.StarRating}/5, 等级: {result.GradeText}, " +
                      $"击杀: {result.TotalDefeatedEnemyCount}, 存活: {result.HasSurvived}");

            return result;
        }

        /// <summary>
        /// 显示结算摘要 Tip 提示。
        /// 如果 GlobalInit/TipUI 不可用则降级为 Debug.Log。
        /// </summary>
        private static void ShowCaptureTip(SessionResultData result)
        {
            if (result == null)
            {
                return;
            }

            // 使用星级判断替代硬编码分数阈值，星级阈值定义在 SessionResultRuleService 中
            string tipText = result.StarRating >= 4
                ? $"战斗结算: {result.CombatScore} 分 ({result.GradeText}) ★{result.StarRating} — 表现卓越!"
                : $"战斗结算: {result.CombatScore} 分 ({result.GradeText}) ★{result.StarRating}";

            try
            {
                AWorkerTask.ShowTipProvider(tipText);
                return;
            }
            catch (System.Exception exception)
            {
                AWorkerTask.LogProvider(
                    $"SessionResultAutoTrigger.ShowCaptureTip failed.\n{exception}",
                    LogManager.LogLevelEnum.Error);

                // Tip 显示失败，降级到 Debug.Log
            }

            Debug.Log($"[SessionResultAutoTrigger] {tipText}");
        }

        /// <summary>
        /// 获取当前订阅状态的文本摘要（用于 Editor 调试）
        /// </summary>
        /// <returns>状态描述文本</returns>
        public string GetStatusText()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(128);
            sb.AppendLine($"SessionResultAutoTrigger 状态:");
            sb.AppendLine($"  实例存在: {(instance != null ? "是" : "否")}");
            sb.AppendLine($"  玩家死亡采集: {(this.captureOnPlayerDeath ? "启用" : "禁用")}");
            sb.AppendLine($"  波次通关采集: {(this.captureOnAllWavesCleared ? "启用" : "禁用")}");
            sb.AppendLine($"  Tip 反馈: {(this.showResultTipOnCapture ? "启用" : "禁用")}");
            sb.AppendLine($"  WaveManager 已订阅: {(this.waveSubscribed ? "是" : "否")}");

            WaveManager wm = Core.ServiceLocator.Get<WaveManager>();
            sb.AppendLine($"  WaveManager 存在: {(wm != null ? "是" : "否")}");
            if (wm != null)
            {
                sb.AppendLine($"  当前波次: {wm.CurrentWaveIndex}, 波次活跃: {wm.IsWaveActive}");
            }

            SessionResultManager srm = Core.ServiceLocator.Get<SessionResultManager>();
            sb.AppendLine($"  SessionResultManager 存在: {(srm != null ? "是" : "否")}");
            if (srm != null)
            {
                sb.AppendLine($"  结算历史数: {srm.HistoryCount}");
                SessionResultData latest = srm.LatestResult;
                if (latest != null)
                {
                    sb.AppendLine($"  最新结算: 评分 {latest.CombatScore}, {latest.GradeText}, ★{latest.StarRating}");
                }
            }

            return sb.ToString();
        }
    }
}
