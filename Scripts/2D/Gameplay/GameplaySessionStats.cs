namespace LAB2D.Gameplay
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Character.Enemy;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Gameplay;
    using LAB2D.Item;
    using Character = LAB2D.Character.Character;
    using LAB2D.Domain.Common;
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// 仅运行时使用的会话统计数据，涵盖战斗、奖励、收集和任务反馈。
    /// 本类不保存数据、不同步网络，也不需要场景对象。
    /// </summary>
    public class GameplaySessionStats : Singleton<GameplaySessionStats>
    {
        private const float DefaultComboTimeout = 4.0f;
        private const int ItemTypeIdInterval = 100000;

        private readonly GameplaySessionStatsRuleService ruleService;
        private readonly Dictionary<int, int> collectedItemsById;
        private readonly Dictionary<string, int> collectedItemsBySource;
        private readonly Dictionary<AItem.ItemTypeEnum, int> collectedItemsByType;
        private readonly Dictionary<string, int> defeatedEnemiesByAttackerType;
        private readonly Dictionary<string, int> defeatedEnemiesByType;
        private readonly Dictionary<string, int> completedWorkerTasksByType;

        private float comboTimeout;
        private float lastDefeatRealtime;
        private float sessionStartRealtime;
        private IGameTime gameTime;

        private IGameTime GameTime => this.gameTime ?? (this.gameTime = Core.ServiceLocator.Get<IGameTime>());
        private int criticalHitCount;
        private int currentCombo;
        private int maxCombo;
        private int playerDeathCount;
        private int totalCollectedItemCount;
        private int totalDamageDealt;
        private int totalDamageTaken;
        private int totalDefeatedEnemyCount;
        private int totalExperienceGained;
        private int totalWorkerDeathCount;
        private int totalWorkerTaskCompletedCount;

        public GameplaySessionStats()
        {
            this.ruleService = new GameplaySessionStatsRuleService();
            this.collectedItemsById = new Dictionary<int, int>();
            this.collectedItemsBySource = new Dictionary<string, int>();
            this.collectedItemsByType = new Dictionary<AItem.ItemTypeEnum, int>();
            this.defeatedEnemiesByAttackerType = new Dictionary<string, int>();
            this.defeatedEnemiesByType = new Dictionary<string, int>();
            this.completedWorkerTasksByType = new Dictionary<string, int>();
            this.comboTimeout = DefaultComboTimeout;
            this.ResetSession();
        }

        public event Action<GameplaySessionStatsSnapshot> StatsChanged;

        public float ComboTimeout
        {
            get
            {
                return this.comboTimeout;
            }

            set
            {
                this.comboTimeout = this.ruleService.ClampComboTimeout(value);
            }
        }

        public void ResetSession()
        {
            this.sessionStartRealtime = this.GameTime.RealtimeSinceStartup;
            this.lastDefeatRealtime = -this.comboTimeout;
            this.criticalHitCount = 0;
            this.currentCombo = 0;
            this.maxCombo = 0;
            this.playerDeathCount = 0;
            this.totalCollectedItemCount = 0;
            this.totalDamageDealt = 0;
            this.totalDamageTaken = 0;
            this.totalDefeatedEnemyCount = 0;
            this.totalExperienceGained = 0;
            this.totalWorkerDeathCount = 0;
            this.totalWorkerTaskCompletedCount = 0;
            this.collectedItemsById.Clear();
            this.collectedItemsBySource.Clear();
            this.collectedItemsByType.Clear();
            this.defeatedEnemiesByAttackerType.Clear();
            this.defeatedEnemiesByType.Clear();
            this.completedWorkerTasksByType.Clear();
            this.NotifyStatsChanged();
        }

        public void RecordItemCollected(ResourceInfo resourceInfo)
        {
            this.RecordItemCollected(resourceInfo, null);
        }

        public void RecordItemCollected(ResourceInfo resourceInfo, string source)
        {
            if (resourceInfo == null || resourceInfo.Count <= 0)
            {
                return;
            }

            this.totalCollectedItemCount += resourceInfo.Count;
            this.AddCount(this.collectedItemsById, resourceInfo.Id, resourceInfo.Count);
            this.AddCount(this.collectedItemsBySource, string.IsNullOrEmpty(source) ? "UnknownSource" : source, resourceInfo.Count);
            this.AddCount(this.collectedItemsByType, this.ResolveItemType(resourceInfo.Id), resourceInfo.Count);
            this.NotifyStatsChanged();
        }

        public void RecordEnemyDefeated(AEnemy enemy, Character attacker, int experienceReward)
        {
            string enemyType = enemy == null ? "UnknownEnemy" : enemy.GetType().Name;
            this.RecordEnemyDefeated(enemyType, attacker == null ? string.Empty : attacker.GetType().Name, experienceReward);
        }

        public void RecordEnemyDefeated(string enemyType, string attackerType, int experienceReward)
        {
            enemyType = string.IsNullOrEmpty(enemyType) ? "UnknownEnemy" : enemyType;
            attackerType = string.IsNullOrEmpty(attackerType) ? "UnknownAttacker" : attackerType;

            this.totalDefeatedEnemyCount++;
            this.AddCount(this.defeatedEnemiesByAttackerType, attackerType, 1);
            this.AddCount(this.defeatedEnemiesByType, enemyType, 1);
            if (experienceReward > 0)
            {
                this.totalExperienceGained += experienceReward;
            }

            this.UpdateCombo();
            this.NotifyStatsChanged();
        }

        public void RecordExperienceGained(int experience)
        {
            if (experience <= 0)
            {
                return;
            }

            this.totalExperienceGained += experience;
            this.NotifyStatsChanged();
        }

        public void RecordDamageDealt(float damage, bool isCritical)
        {
            int damageValue = this.ruleService.ToRecordedDamage(damage);
            if (damageValue == 0)
            {
                return;
            }

            this.totalDamageDealt += damageValue;
            if (isCritical)
            {
                this.criticalHitCount++;
            }

            this.NotifyStatsChanged();
        }

        public void RecordDamageTaken(float damage)
        {
            int damageValue = this.ruleService.ToRecordedDamage(damage);
            if (damageValue == 0)
            {
                return;
            }

            this.totalDamageTaken += damageValue;
            this.NotifyStatsChanged();
        }

        public void RecordWorkerTaskCompleted(WorkerTaskType taskType)
        {
            this.totalWorkerTaskCompletedCount++;
            this.AddCount(this.completedWorkerTasksByType, taskType.ToString(), 1);
            this.NotifyStatsChanged();
        }

        public void RecordPlayerDeath()
        {
            this.playerDeathCount++;
            this.currentCombo = 0;
            this.NotifyStatsChanged();
        }

        public void RecordWorkerDeath()
        {
            this.totalWorkerDeathCount++;
            this.NotifyStatsChanged();
        }

        public GameplaySessionStatsSnapshot CreateSnapshot()
        {
            return new GameplaySessionStatsSnapshot
            {
                SessionDuration = this.ruleService.GetSessionDuration(this.GameTime.RealtimeSinceStartup, this.sessionStartRealtime),
                CriticalHitCount = this.criticalHitCount,
                CurrentCombo = this.currentCombo,
                MaxCombo = this.maxCombo,
                PlayerDeathCount = this.playerDeathCount,
                TotalCollectedItemCount = this.totalCollectedItemCount,
                TotalDamageDealt = this.totalDamageDealt,
                TotalDamageTaken = this.totalDamageTaken,
                TotalDefeatedEnemyCount = this.totalDefeatedEnemyCount,
                TotalExperienceGained = this.totalExperienceGained,
                TotalWorkerDeathCount = this.totalWorkerDeathCount,
                TotalWorkerTaskCompletedCount = this.totalWorkerTaskCompletedCount,
                CollectedItemsById = new Dictionary<int, int>(this.collectedItemsById),
                CollectedItemsBySource = new Dictionary<string, int>(this.collectedItemsBySource),
                CollectedItemsByType = new Dictionary<AItem.ItemTypeEnum, int>(this.collectedItemsByType),
                CompletedWorkerTasksByType = new Dictionary<string, int>(this.completedWorkerTasksByType),
                DefeatedEnemiesByAttackerType = new Dictionary<string, int>(this.defeatedEnemiesByAttackerType),
                DefeatedEnemiesByType = new Dictionary<string, int>(this.defeatedEnemiesByType),
            };
        }

        public string BuildSummaryText()
        {
            GameplaySessionStatsSnapshot snapshot = this.CreateSnapshot();
            StringBuilder builder = new StringBuilder(256);
            builder.AppendLine("Session Stats");
            builder.AppendLine($"Duration: {snapshot.SessionDuration:0.0}s");
            builder.AppendLine($"Enemies Defeated: {snapshot.TotalDefeatedEnemyCount}");
            builder.AppendLine($"Max Combo: {snapshot.MaxCombo}");
            builder.AppendLine($"Experience: {snapshot.TotalExperienceGained}");
            builder.AppendLine($"Items Collected: {snapshot.TotalCollectedItemCount}");
            builder.AppendLine($"Damage Dealt: {snapshot.TotalDamageDealt}");
            builder.AppendLine($"Damage Taken: {snapshot.TotalDamageTaken}");
            builder.AppendLine($"Critical Hits: {snapshot.CriticalHitCount}");
            builder.AppendLine($"Worker Tasks: {snapshot.TotalWorkerTaskCompletedCount}");
            builder.AppendLine($"Player Deaths: {snapshot.PlayerDeathCount}");
            builder.AppendLine($"Worker Deaths: {snapshot.TotalWorkerDeathCount}");
            return builder.ToString();
        }

        private void UpdateCombo()
        {
            float now = this.GameTime.RealtimeSinceStartup;
            this.currentCombo = this.ruleService.GetNextCombo(
                now,
                this.lastDefeatRealtime,
                this.comboTimeout,
                this.currentCombo);
            this.lastDefeatRealtime = now;
            this.maxCombo = this.ruleService.GetMaxCombo(this.maxCombo, this.currentCombo);
        }

        private AItem.ItemTypeEnum ResolveItemType(int itemId)
        {
            if (itemId < 0)
            {
                return AItem.ItemTypeEnum.Null;
            }

            int typeValue = itemId / ItemTypeIdInterval;
            if (typeValue < 0 || typeValue > (int)AItem.ItemTypeEnum.Null)
            {
                return AItem.ItemTypeEnum.Null;
            }

            return (AItem.ItemTypeEnum)typeValue;
        }

        private void NotifyStatsChanged()
        {
            this.StatsChanged?.Invoke(this.CreateSnapshot());
        }

        private void AddCount<TKey>(Dictionary<TKey, int> dict, TKey key, int count)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] += count;
                return;
            }

            dict.Add(key, count);
        }
    }

    [Serializable]
    public class GameplaySessionStatsSnapshot
    {
        public float SessionDuration;
        public int CriticalHitCount;
        public int CurrentCombo;
        public int MaxCombo;
        public int PlayerDeathCount;
        public int TotalCollectedItemCount;
        public int TotalDamageDealt;
        public int TotalDamageTaken;
        public int TotalDefeatedEnemyCount;
        public int TotalExperienceGained;
        public int TotalWorkerDeathCount;
        public int TotalWorkerTaskCompletedCount;
        public Dictionary<int, int> CollectedItemsById;
        public Dictionary<string, int> CollectedItemsBySource;
        public Dictionary<AItem.ItemTypeEnum, int> CollectedItemsByType;
        public Dictionary<string, int> CompletedWorkerTasksByType;
        public Dictionary<string, int> DefeatedEnemiesByAttackerType;
        public Dictionary<string, int> DefeatedEnemiesByType;
    }
}
