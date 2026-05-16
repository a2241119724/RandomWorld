namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 玩家生命危险提示工具类。
    /// 只负责血量比例计算、提示等级判断和展示文案格式化，不访问 Scene、Prefab、存档、Photon 或 AssetBundle。
    /// 使用边界：本工具不持有运行时状态，也不修改玩家血量、死亡惩罚、恢复数值或 UI 对象。
    /// </summary>
    public static class PlayerVitalAlertTool
    {
        /// <summary>
        /// 安全获取玩家数据。
        /// </summary>
        /// <param name="player">目标玩家。</param>
        /// <param name="playerData">返回的玩家数据。</param>
        /// <returns>成功获取时返回 true。</returns>
        public static bool TryGetPlayerData(Player player, out Player.PlayerData playerData)
        {
            playerData = null;
            if (player == null || player.CharacterDataLAB == null)
            {
                return false;
            }

            playerData = player.CharacterDataLAB as Player.PlayerData;
            return playerData != null;
        }

        /// <summary>
        /// 计算安全比例。
        /// </summary>
        /// <param name="current">当前值。</param>
        /// <param name="max">最大值。</param>
        /// <returns>0 到 1 之间的比例；最大值无效时返回 0。</returns>
        public static float GetSafeRatio(float current, float max)
        {
            if (max <= 0.0f)
            {
                return 0.0f;
            }

            return Mathf.Clamp01(current / max);
        }

        /// <summary>
        /// 根据玩家血量和复活状态计算生命提示等级。
        /// </summary>
        /// <param name="currentHp">当前生命值。</param>
        /// <param name="maxHp">最大生命值。</param>
        /// <param name="isRespawning">是否处于复活等待。</param>
        /// <returns>玩家生命提示等级。</returns>
        public static PlayerVitalAlertLevel GetLevel(float currentHp, float maxHp, bool isRespawning)
        {
            if (isRespawning)
            {
                return PlayerVitalAlertLevel.Respawning;
            }

            float ratio = GetSafeRatio(currentHp, maxHp);
            if (ratio <= PlayerVitalAlertConstant.CriticalRatio)
            {
                return PlayerVitalAlertLevel.Critical;
            }

            if (ratio <= PlayerVitalAlertConstant.WarningRatio)
            {
                return PlayerVitalAlertLevel.Wounded;
            }

            return PlayerVitalAlertLevel.Safe;
        }

        /// <summary>
        /// 获取生命提示等级中文名。
        /// </summary>
        /// <param name="level">生命提示等级。</param>
        /// <returns>适合 UI、Tip 和日志展示的中文名。</returns>
        public static string GetLevelName(PlayerVitalAlertLevel level)
        {
            switch (level)
            {
                case PlayerVitalAlertLevel.Wounded:
                    return "生命偏低";
                case PlayerVitalAlertLevel.Critical:
                    return "生命濒危";
                case PlayerVitalAlertLevel.Respawning:
                    return "复活等待";
                default:
                    return "生命安全";
            }
        }

        /// <summary>
        /// 获取生命提示等级 RichText 颜色。
        /// </summary>
        /// <param name="level">生命提示等级。</param>
        /// <returns>HTML 颜色字符串。</returns>
        public static string GetLevelRichColor(PlayerVitalAlertLevel level)
        {
            switch (level)
            {
                case PlayerVitalAlertLevel.Wounded:
                    return PixelUITheme.RichGold;
                case PlayerVitalAlertLevel.Critical:
                    return PixelUITheme.RichCoral;
                case PlayerVitalAlertLevel.Respawning:
                    return PixelUITheme.RichLavender;
                default:
                    return PixelUITheme.RichMint;
            }
        }

        /// <summary>
        /// 获取当前等级对应的玩家建议文案。
        /// </summary>
        /// <param name="level">生命提示等级。</param>
        /// <returns>建议文案。</returns>
        public static string GetAdviceText(PlayerVitalAlertLevel level)
        {
            switch (level)
            {
                case PlayerVitalAlertLevel.Wounded:
                    return PlayerVitalAlertConstant.WoundedAdviceText;
                case PlayerVitalAlertLevel.Critical:
                    return PlayerVitalAlertConstant.CriticalAdviceText;
                case PlayerVitalAlertLevel.Respawning:
                    return PlayerVitalAlertConstant.RespawningAdviceText;
                default:
                    return PlayerVitalAlertConstant.SafeAdviceText;
            }
        }

        /// <summary>
        /// 生成生命提示 Tip 文案。
        /// </summary>
        /// <param name="playerName">玩家名称。</param>
        /// <param name="level">生命提示等级。</param>
        /// <param name="currentHp">当前生命值。</param>
        /// <param name="maxHp">最大生命值。</param>
        /// <param name="adviceText">建议文案。</param>
        /// <returns>适合现有 TipUI 展示的短文本。</returns>
        public static string BuildTipText(
            string playerName,
            PlayerVitalAlertLevel level,
            float currentHp,
            float maxHp,
            string adviceText)
        {
            string displayName = string.IsNullOrEmpty(playerName)
                ? PlayerVitalAlertConstant.DefaultPlayerName
                : playerName;
            return $"{displayName} {GetLevelName(level)}：{FormatHp(currentHp, maxHp)}。{adviceText}";
        }

        /// <summary>
        /// 生成生命恢复 Tip 文案。
        /// </summary>
        /// <param name="playerName">玩家名称。</param>
        /// <param name="currentHp">当前生命值。</param>
        /// <param name="maxHp">最大生命值。</param>
        /// <returns>适合现有 TipUI 展示的恢复短文本。</returns>
        public static string BuildRecoveredTipText(string playerName, float currentHp, float maxHp)
        {
            string displayName = string.IsNullOrEmpty(playerName)
                ? PlayerVitalAlertConstant.DefaultPlayerName
                : playerName;
            return $"{displayName} {PlayerVitalAlertConstant.RecoveredTipText} {FormatHp(currentHp, maxHp)}";
        }

        /// <summary>
        /// 生成生命状态摘要文本。
        /// </summary>
        /// <param name="level">生命提示等级。</param>
        /// <param name="currentHp">当前生命值。</param>
        /// <param name="maxHp">最大生命值。</param>
        /// <param name="adviceText">建议文案。</param>
        /// <returns>适合 Editor 菜单和后续 HUD 展示的摘要。</returns>
        public static string BuildSummaryText(
            PlayerVitalAlertLevel level,
            float currentHp,
            float maxHp,
            string adviceText)
        {
            return $"{PlayerVitalAlertConstant.SummaryTitle}: {GetLevelName(level)} | {FormatHp(currentHp, maxHp)}\n" +
                $"{PlayerVitalAlertConstant.AdviceLabel}: {adviceText}";
        }

        /// <summary>
        /// 格式化生命值和百分比。
        /// </summary>
        /// <param name="currentHp">当前生命值。</param>
        /// <param name="maxHp">最大生命值。</param>
        /// <returns>生命值文本。</returns>
        public static string FormatHp(float currentHp, float maxHp)
        {
            float ratio = GetSafeRatio(currentHp, maxHp);
            return $"{Mathf.CeilToInt(Mathf.Max(0.0f, currentHp))}/{Mathf.CeilToInt(Mathf.Max(0.0f, maxHp))} ({Mathf.RoundToInt(ratio * 100.0f)}%)";
        }
    }
}
