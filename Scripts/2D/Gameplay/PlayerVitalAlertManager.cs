namespace LAB2D
{
    using System;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// 玩家生命危险提示管理器。
    /// 负责只读读取本地玩家血量，生成生命危险等级与玩家建议，并按冷却规则请求现有 Tip UI 展示。
    /// 本类不修改玩家属性、不改变死亡惩罚、不写入存档、不参与 Photon 同步。
    /// </summary>
    public class PlayerVitalAlertManager : Singleton<PlayerVitalAlertManager>
    {
        private PlayerVitalAlertReport currentReport;
        private string lastSignature = string.Empty;
        private float nextRefreshTime;
        private float lastTipTime = -999.0f;
        private bool enabled = true;
        private bool tipEnabled = true;

        /// <summary>
        /// 玩家生命报告变化事件。
        /// HUD、Editor 菜单或后续任务目标系统可订阅该事件刷新展示。
        /// </summary>
        public event Action<PlayerVitalAlertReport> OnPlayerVitalAlertChanged;

        /// <summary>
        /// 玩家生命 Tip 请求事件。
        /// 外部可订阅该事件接管提示展示方式。
        /// </summary>
        public event Action<string> OnPlayerVitalAlertTipRequested;

        /// <summary>
        /// 当前玩家生命报告。
        /// </summary>
        public PlayerVitalAlertReport CurrentReport
        {
            get
            {
                if (this.currentReport == null)
                {
                    this.Refresh(false);
                }

                return this.currentReport;
            }
        }

        /// <summary>
        /// 生命监控是否启用。
        /// </summary>
        public bool IsEnabled
        {
            get { return this.enabled; }
        }

        /// <summary>
        /// 启用玩家生命监控。
        /// </summary>
        public void Enable()
        {
            this.enabled = true;
            this.Refresh(false);
        }

        /// <summary>
        /// 禁用玩家生命监控。
        /// 禁用后仍保留最后一次报告，便于 Editor 查看。
        /// </summary>
        public void Disable()
        {
            this.enabled = false;
        }

        /// <summary>
        /// 设置是否允许显示生命危险 Tip。
        /// </summary>
        /// <param name="enabledTip">是否显示 Tip。</param>
        public void SetTipEnabled(bool enabledTip)
        {
            this.tipEnabled = enabledTip;
        }

        /// <summary>
        /// 按固定间隔刷新玩家生命报告。
        /// 可从 `GlobalInit.Update()` 每帧调用，内部会自行节流。
        /// </summary>
        public void Tick()
        {
            if (!this.enabled || Time.time < this.nextRefreshTime)
            {
                return;
            }

            this.nextRefreshTime = Time.time + Mathf.Max(0.1f, PlayerVitalAlertConstant.MonitorRefreshInterval);
            this.Refresh(true);
        }

        /// <summary>
        /// 立即刷新玩家生命报告。
        /// </summary>
        /// <param name="allowTip">是否允许在报告变化时显示 Tip。</param>
        /// <returns>新的玩家生命报告。</returns>
        public PlayerVitalAlertReport Refresh(bool allowTip)
        {
            PlayerVitalAlertLevel previousLevel = this.currentReport == null
                ? PlayerVitalAlertLevel.Safe
                : this.currentReport.Level;

            PlayerVitalAlertReport report = this.BuildReport();
            string signature = report.BuildSignature();
            bool changed = !signature.Equals(this.lastSignature);

            this.currentReport = report;
            if (changed)
            {
                this.lastSignature = signature;
                this.OnPlayerVitalAlertChanged?.Invoke(report);
            }

            if (allowTip && changed)
            {
                this.TryShowVitalTip(report, previousLevel);
            }

            return report;
        }

        /// <summary>
        /// 构建适合 HUD、Editor 菜单或日志展示的生命摘要。
        /// </summary>
        /// <returns>多行生命摘要文本。</returns>
        public string BuildSummaryText()
        {
            return this.CurrentReport == null
                ? PlayerVitalAlertConstant.PlayerUnavailableText
                : this.CurrentReport.ToSummaryText();
        }

        /// <summary>
        /// 手动触发一次当前生命危险 Tip。
        /// </summary>
        /// <returns>当前存在可提示生命危险时返回 true。</returns>
        public bool TryShowCurrentTip()
        {
            PlayerVitalAlertReport report = this.Refresh(false);
            if (report == null || !report.ShouldShowTip)
            {
                return false;
            }

            this.lastTipTime = Time.time;
            this.ShowTip(report.ToTipText());
            return true;
        }

        /// <summary>
        /// 生成当前玩家生命报告。
        /// </summary>
        /// <returns>只读报告数据。</returns>
        private PlayerVitalAlertReport BuildReport()
        {
            try
            {
                Player player = PlayerManager.Instance.Mine;
                if (player == null)
                {
                    return new PlayerVitalAlertReport
                    {
                        Level = PlayerVitalAlertLevel.Safe,
                        AdviceText = PlayerVitalAlertConstant.PlayerUnavailableText,
                        ErrorMessage = PlayerVitalAlertConstant.PlayerUnavailableText,
                    };
                }

                if (!PlayerVitalAlertTool.TryGetPlayerData(player, out Player.PlayerData playerData))
                {
                    return new PlayerVitalAlertReport
                    {
                        PlayerName = player.name,
                        Level = PlayerVitalAlertLevel.Safe,
                        AdviceText = PlayerVitalAlertConstant.PlayerDataUnavailableText,
                        ErrorMessage = PlayerVitalAlertConstant.PlayerDataUnavailableText,
                    };
                }

                bool isRespawning = DeathPenaltyManager.Instance.IsRespawning;
                PlayerVitalAlertLevel level = PlayerVitalAlertTool.GetLevel(
                    playerData.Hp,
                    playerData.MaxHp,
                    isRespawning);

                return new PlayerVitalAlertReport
                {
                    PlayerName = player.name,
                    CurrentHp = playerData.Hp,
                    MaxHp = playerData.MaxHp,
                    HpRatio = PlayerVitalAlertTool.GetSafeRatio(playerData.Hp, playerData.MaxHp),
                    Level = level,
                    IsRespawning = isRespawning,
                    AdviceText = PlayerVitalAlertTool.GetAdviceText(level),
                };
            }
            catch (Exception exception)
            {
                string errorMessage = PlayerVitalAlertConstant.ScanFailedPrefix + exception.Message;
                return new PlayerVitalAlertReport
                {
                    Level = PlayerVitalAlertLevel.Safe,
                    AdviceText = errorMessage,
                    ErrorMessage = errorMessage,
                };
            }
        }

        /// <summary>
        /// 在玩家生命状态变化时显示 Tip。
        /// </summary>
        /// <param name="report">玩家生命报告。</param>
        /// <param name="previousLevel">上一次生命提示等级。</param>
        private void TryShowVitalTip(PlayerVitalAlertReport report, PlayerVitalAlertLevel previousLevel)
        {
            if (!this.tipEnabled || report == null)
            {
                return;
            }

            bool recovered = IsDangerLevel(previousLevel) &&
                report.Level == PlayerVitalAlertLevel.Safe &&
                report.HpRatio >= PlayerVitalAlertConstant.RecoveryRatio;
            bool shouldWarn = report.ShouldShowTip;
            if (!recovered && !shouldWarn)
            {
                return;
            }

            float now = Time.time;
            bool escalated = IsMoreSevere(report.Level, previousLevel);
            if (!recovered && !escalated && now - this.lastTipTime < PlayerVitalAlertConstant.TipCooldownSeconds)
            {
                return;
            }

            this.lastTipTime = now;
            string message = recovered
                ? PlayerVitalAlertTool.BuildRecoveredTipText(report.PlayerName, report.CurrentHp, report.MaxHp)
                : report.ToTipText();
            this.ShowTip(message);
        }

        /// <summary>
        /// 判断是否属于需要恢复提示的危险等级。
        /// </summary>
        /// <param name="level">生命提示等级。</param>
        /// <returns>受伤、濒危或复活等待时返回 true。</returns>
        private static bool IsDangerLevel(PlayerVitalAlertLevel level)
        {
            return level == PlayerVitalAlertLevel.Wounded ||
                level == PlayerVitalAlertLevel.Critical ||
                level == PlayerVitalAlertLevel.Respawning;
        }

        /// <summary>
        /// 判断新等级是否比旧等级更严重。
        /// </summary>
        /// <param name="next">新等级。</param>
        /// <param name="previous">旧等级。</param>
        /// <returns>新等级严重度更高时返回 true。</returns>
        private static bool IsMoreSevere(PlayerVitalAlertLevel next, PlayerVitalAlertLevel previous)
        {
            return GetSeverity(next) > GetSeverity(previous);
        }

        /// <summary>
        /// 获取生命提示等级严重度。
        /// </summary>
        /// <param name="level">生命提示等级。</param>
        /// <returns>越大表示越严重。</returns>
        private static int GetSeverity(PlayerVitalAlertLevel level)
        {
            switch (level)
            {
                case PlayerVitalAlertLevel.Wounded:
                    return 1;
                case PlayerVitalAlertLevel.Critical:
                    return 2;
                case PlayerVitalAlertLevel.Respawning:
                    return 3;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// 显示玩家生命提示。
        /// 优先复用现有 Tip UI，不可用时降级为日志。
        /// </summary>
        /// <param name="message">提示内容。</param>
        private void ShowTip(string message)
        {
            this.OnPlayerVitalAlertTipRequested?.Invoke(message);

            try
            {
                if (GlobalInit.Instance != null)
                {
                    GlobalInit.Instance.ShowTip(message);
                    return;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(PlayerVitalAlertConstant.LogPrefix + " 显示 Tip 失败: " + exception.Message);
            }

            Debug.Log(PlayerVitalAlertConstant.LogPrefix + " " + message);
        }
    }

    /// <summary>
    /// 玩家生命危险报告。
    /// 由 PlayerVitalAlertManager 维护，供 Tip、HUD、Editor 菜单和后续任务目标系统只读查询。
    /// </summary>
    [Serializable]
    public class PlayerVitalAlertReport
    {
        /// <summary>玩家名称。</summary>
        public string PlayerName;

        /// <summary>当前生命值。</summary>
        public float CurrentHp;

        /// <summary>最大生命值。</summary>
        public float MaxHp;

        /// <summary>当前生命比例。</summary>
        public float HpRatio;

        /// <summary>当前生命提示等级。</summary>
        public PlayerVitalAlertLevel Level;

        /// <summary>是否处于复活等待。</summary>
        public bool IsRespawning;

        /// <summary>面向玩家的建议文案。</summary>
        public string AdviceText;

        /// <summary>扫描异常信息，正常为空。</summary>
        public string ErrorMessage;

        /// <summary>
        /// 是否达到可主动 Tip 的生命危险程度。
        /// </summary>
        public bool ShouldShowTip
        {
            get
            {
                return this.Level == PlayerVitalAlertLevel.Wounded ||
                    this.Level == PlayerVitalAlertLevel.Critical ||
                    this.Level == PlayerVitalAlertLevel.Respawning;
            }
        }

        /// <summary>
        /// 构建用于变化检测的签名。
        /// </summary>
        /// <returns>报告关键字段签名。</returns>
        public string BuildSignature()
        {
            StringBuilder builder = new StringBuilder(128);
            builder.Append(this.PlayerName).Append('|')
                .Append(Mathf.RoundToInt(this.HpRatio * 100.0f)).Append('|')
                .Append(this.Level).Append('|')
                .Append(this.IsRespawning).Append('|')
                .Append(this.ErrorMessage);

            return builder.ToString();
        }

        /// <summary>
        /// 生成 HUD、Editor 菜单和日志使用的摘要文本。
        /// </summary>
        /// <returns>多行玩家生命摘要。</returns>
        public string ToSummaryText()
        {
            if (!string.IsNullOrEmpty(this.ErrorMessage))
            {
                return this.ErrorMessage;
            }

            return PlayerVitalAlertTool.BuildSummaryText(
                this.Level,
                this.CurrentHp,
                this.MaxHp,
                this.AdviceText);
        }

        /// <summary>
        /// 生成适合现有 TipUI 展示的短文案。
        /// </summary>
        /// <returns>短 Tip 文案。</returns>
        public string ToTipText()
        {
            if (!string.IsNullOrEmpty(this.ErrorMessage))
            {
                return this.ErrorMessage;
            }

            return PlayerVitalAlertTool.BuildTipText(
                this.PlayerName,
                this.Level,
                this.CurrentHp,
                this.MaxHp,
                this.AdviceText);
        }
    }
}
