namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Player;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay;
    using UnityEngine;

    /// <summary>
    /// 管理玩家死亡惩罚：复活延迟、经验损失、随机复活位置、死亡画面显示和死亡追踪。
    /// 低侵入式单例 —— 不修改场景、预制体、SO 或存档数据。
    /// </summary>
    public class DeathPenaltyManager : Singleton<DeathPenaltyManager>
    {
        internal static System.Func<GameplaySessionStats> GameplaySessionStatsProvider { get; set; }
            = () => ServiceLocator.Get<GameplaySessionStats>();
        internal static System.Func<DeathMenuPanel> DeathMenuPanelProvider { get; set; }
            = () => DeathMenuPanel.Instance;

        /// <summary>
        /// 玩家复活前需要等待的秒数。
        /// </summary>
        public float RespawnDelaySeconds = 3.0f;

        /// <summary>
        /// 死亡时损失当前经验的百分比（0.0 - 1.0）。
        /// </summary>
        public float ExperienceLossPercent = 0.1f;

        /// <summary>
        /// 复活时恢复的最大生命值百分比（0.3 = 30%）。
        /// </summary>
        public float HpRestorePercent = 0.3f;

        private readonly DeathPenaltyRuleService ruleService = new DeathPenaltyRuleService();
        private float respawnDeadline = -1f;
        private IGameTime gameTime;

        private IGameTime GameTime => this.gameTime ?? (this.gameTime = Core.ServiceLocator.Get<IGameTime>());

        /// <summary>
        /// 玩家当前是否正在等待复活。
        /// </summary>
        public bool IsRespawning
        {
            get { return this.ruleService.IsRespawning(this.respawnDeadline, this.GameTime.RealtimeSinceStartup); }
        }

        /// <summary>
        /// 距离复活剩余的秒数，不在复活状态时为 0。
        /// </summary>
        public float RespawnRemaining
        {
            get
            {
                if (!this.IsRespawning)
                {
                    return 0f;
                }

                return this.ruleService.GetRespawnRemaining(this.respawnDeadline, this.GameTime.RealtimeSinceStartup);
            }
        }

        /// <summary>
        /// 当前会话的总死亡次数。
        /// </summary>
        public int SessionDeathCount
        {
            get { return GameplaySessionStatsProvider().CreateSnapshot().PlayerDeathCount; }
        }

        /// <summary>
        /// 由 Player.Death() 调用。施加死亡惩罚、显示死亡画面并启动复活倒计时。
        /// </summary>
        public void HandlePlayerDeath(Player player)
        {
            if (player == null)
            {
                return;
            }

            // 施加经验惩罚
            int expLoss = this.ruleService.GetExperienceLoss(
                player.CharacterDataLAB.CurExperience,
                this.ExperienceLossPercent);
            if (expLoss > 0)
            {
                player.CharacterDataLAB.CurExperience = this.ruleService.ApplyExperienceLoss(
                    player.CharacterDataLAB.CurExperience,
                    expLoss);
            }

            // 启动复活倒计时
            this.respawnDeadline = this.ruleService.GetRespawnDeadline(
                this.GameTime.RealtimeSinceStartup,
                this.RespawnDelaySeconds);

            // 显示死亡画面（纯代码 UI，无需预制体）
            DeathMenuPanelProvider().Show(
                GameplaySessionStatsProvider().CreateSnapshot().PlayerDeathCount,
                this.ruleService.ToCountdownSeconds(this.RespawnDelaySeconds));
        }

        /// <summary>
        /// 由 Player 每帧调用。复活完成时返回 true。
        /// 将玩家移动到随机可用地图位置，恢复 HP/MP，并关闭死亡画面。
        /// </summary>
        public bool TryCompleteRespawn(Player player)
        {
            // 无活跃的复活倒计时
            if (this.respawnDeadline < 0f)
            {
                return false;
            }

            // 仍在等待倒计时结束
            if (this.GameTime.RealtimeSinceStartup < this.respawnDeadline)
            {
                return false;
            }

            // 清除复活状态
            this.respawnDeadline = -1f;

            // 在地图上寻找随机可到达位置
            Vector3Int randomMapPos = Core.ServiceLocator.Get<TileMap>().GenCanReachPos();
            Vector3 respawnWorldPos = AWorkerTask.TileMapPositionProvider(randomMapPos);
            player.transform.position = respawnWorldPos;

            // 恢复玩家图层，使敌人可重新检测到玩家
            player.gameObject.layer = LayerMask.NameToLayer(LayerConstant.PLAYER_LAYER);

            // 恢复生命值（默认 30%）和魔法值
            player.CharacterDataLAB.Hp = this.ruleService.GetRestoredHp(
                player.CharacterDataLAB.MaxHp,
                this.HpRestorePercent);

            Player.PlayerData playerData = player.CharacterDataLAB as Player.PlayerData;
            if (playerData != null)
            {
                playerData.Mp = playerData.MaxMp;
            }

            // 关闭死亡画面
            DeathMenuPanelProvider().Hide();
            AWorkerTask.ShowTipProvider("Respawned!");

            return true;
        }

        /// <summary>
        /// 更新死亡画面倒计时显示。
        /// 复活期间由 Player.Update() 每帧调用。
        /// </summary>
        public void UpdateDeathScreen()
        {
            DeathMenuPanelProvider().UpdateCountdown(this.ruleService.ToCountdownSeconds(this.RespawnRemaining));
        }
    }
}
