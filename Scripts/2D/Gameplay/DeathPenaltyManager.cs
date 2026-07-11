namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// Manages player death penalty: respawn delay, experience loss, random respawn position,
    /// death screen display, and death tracking.
    /// Low-intrusion singleton — does not modify Scenes, Prefabs, SO, or save data.
    /// </summary>
    public class DeathPenaltyManager : Singleton<DeathPenaltyManager>
    {
        /// <summary>
        /// Seconds the player must wait before respawning.
        /// </summary>
        public float RespawnDelaySeconds = 3.0f;

        /// <summary>
        /// Fraction of current experience lost on death (0.0 - 1.0).
        /// </summary>
        public float ExperienceLossPercent = 0.1f;

        /// <summary>
        /// Fraction of max HP restored on respawn (0.3 = 30%).
        /// </summary>
        public float HpRestorePercent = 0.3f;

        private readonly DeathPenaltyRuleService ruleService = new DeathPenaltyRuleService();
        private float respawnDeadline = -1f;

        /// <summary>
        /// Whether the player is currently waiting to respawn.
        /// </summary>
        public bool IsRespawning
        {
            get { return this.ruleService.IsRespawning(this.respawnDeadline, Time.realtimeSinceStartup); }
        }

        /// <summary>
        /// Seconds remaining until respawn, or 0 if not respawning.
        /// </summary>
        public float RespawnRemaining
        {
            get
            {
                if (!this.IsRespawning)
                {
                    return 0f;
                }

                return this.ruleService.GetRespawnRemaining(this.respawnDeadline, Time.realtimeSinceStartup);
            }
        }

        /// <summary>
        /// Total death count for the current session.
        /// </summary>
        public int SessionDeathCount
        {
            get { return GameplaySessionStats.Instance.CreateSnapshot().PlayerDeathCount; }
        }

        /// <summary>
        /// Call from Player.Death(). Applies death penalty, shows death screen, and starts respawn timer.
        /// </summary>
        public void HandlePlayerDeath(Player player)
        {
            if (player == null)
            {
                return;
            }

            // Apply experience penalty
            int expLoss = this.ruleService.GetExperienceLoss(
                player.CharacterDataLAB.CurExperience,
                this.ExperienceLossPercent);
            if (expLoss > 0)
            {
                player.CharacterDataLAB.CurExperience = this.ruleService.ApplyExperienceLoss(
                    player.CharacterDataLAB.CurExperience,
                    expLoss);
            }

            // Start respawn countdown
            this.respawnDeadline = this.ruleService.GetRespawnDeadline(
                Time.realtimeSinceStartup,
                this.RespawnDelaySeconds);

            // Show death screen (programmatic UI, no prefab needed)
            DeathMenuPanel.Instance.Show(
                GameplaySessionStats.Instance.CreateSnapshot().PlayerDeathCount,
                this.ruleService.ToCountdownSeconds(this.RespawnDelaySeconds));
        }

        /// <summary>
        /// Call every frame from Player. Returns true when respawn just completed.
        /// Moves player to a random available map position, restores HP/MP, and closes death screen.
        /// </summary>
        public bool TryCompleteRespawn(Player player)
        {
            // No active respawn countdown
            if (this.respawnDeadline < 0f)
            {
                return false;
            }

            // Still waiting for countdown to expire
            if (Time.realtimeSinceStartup < this.respawnDeadline)
            {
                return false;
            }

            // Clear respawn state
            this.respawnDeadline = -1f;

            // Find random available position on the map
            Vector3Int randomMapPos = TileMap.Instance.GenCanReachPos();
            Vector3 respawnWorldPos = TileMap.Instance.MapPosToWorldPos(randomMapPos);
            player.transform.position = respawnWorldPos;

            // Restore player layer so enemies can detect again
            player.gameObject.layer = LayerMask.NameToLayer(LayerConstant.PLAYER_LAYER);

            // Restore HP (30% by default) and MP
            player.CharacterDataLAB.Hp = this.ruleService.GetRestoredHp(
                player.CharacterDataLAB.MaxHp,
                this.HpRestorePercent);

            Player.PlayerData playerData = player.CharacterDataLAB as Player.PlayerData;
            if (playerData != null)
            {
                playerData.Mp = playerData.MaxMp;
            }

            // Close death screen
            DeathMenuPanel.Instance.Hide();
            GlobalInit.Instance.ShowTip("Respawned!");

            return true;
        }

        /// <summary>
        /// Update the death screen countdown display.
        /// Called each frame from Player.Update() during respawn.
        /// </summary>
        public void UpdateDeathScreen()
        {
            DeathMenuPanel.Instance.UpdateCountdown(this.ruleService.ToCountdownSeconds(this.RespawnRemaining));
        }
    }
}
