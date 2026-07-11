namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Enemy;
    using LAB2D.Character.Player;
    using LAB2D.Enum;
    using Character = LAB2D.Character.Character;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 波次 Boss 与波间奖励管理器。
    /// 负责 Boss 波判定、敌人属性缩放、波间奖励生成、玩家奖励应用和事件通知。
    /// 本类只保存运行时状态，不修改存档结构，不写资源，不参与 Photon 同步。
    /// </summary>
    public class WaveBossRewardManager : Singleton<WaveBossRewardManager>
    {
        private readonly List<WaveRewardOption> currentOptions;
        private bool initialized;
        private bool enabled = true;
        private bool tipEnabled = true;
        private int currentWaveIndex;
        private bool currentWaveIsBoss;
        private float playerDamageBonus;
        private float playerDamageReduction;
        private float playerMoveSpeedBonus;

        /// <summary>
        /// 构造函数，初始化默认状态。
        /// </summary>
        public WaveBossRewardManager()
        {
            this.currentOptions = new List<WaveRewardOption>(WaveBossRewardConstant.RewardOptionCount);
            this.CurrentState = WaveBossRewardState.CreateDefault();
        }

        /// <summary>
        /// 当前波次 Boss 与奖励状态。
        /// </summary>
        public WaveBossRewardState CurrentState { get; private set; }

        /// <summary>
        /// 当前待选奖励，只读提供给 UI。
        /// </summary>
        public IReadOnlyList<WaveRewardOption> CurrentOptions
        {
            get { return this.currentOptions; }
        }

        /// <summary>
        /// 状态变化事件，HUD 可订阅刷新摘要。
        /// </summary>
        public event Action<WaveBossRewardState> OnStateChanged;

        /// <summary>
        /// 奖励选项变化事件，奖励面板可订阅刷新按钮。
        /// </summary>
        public event Action<IReadOnlyList<WaveRewardOption>> OnRewardOptionsChanged;

        /// <summary>
        /// 玩家选择奖励事件。
        /// </summary>
        public event Action<WaveRewardOption> OnRewardSelected;

        /// <summary>
        /// 奖励提示请求事件，允许表现层接管 Tip。
        /// </summary>
        public event Action<string> OnRewardTipRequested;

        /// <summary>
        /// 启用 Boss 与奖励系统。
        /// </summary>
        public void Enable()
        {
            this.enabled = true;
            this.EnsureInitialized();
            this.UpdateState(this.CurrentState.Phase);
        }

        /// <summary>
        /// 禁用 Boss 与奖励系统，清理待选奖励并让所有倍率回到 1。
        /// </summary>
        public void Disable()
        {
            this.enabled = false;
            this.currentOptions.Clear();
            this.playerDamageBonus = 0.0f;
            this.playerDamageReduction = 0.0f;
            this.playerMoveSpeedBonus = 0.0f;
            this.UpdateState(WavePhaseType.Idle);
            this.OnRewardOptionsChanged?.Invoke(this.currentOptions);
        }

        /// <summary>
        /// 设置是否显示 Tip 提示。
        /// </summary>
        /// <param name="enabledTip">是否显示提示。</param>
        public void SetTipEnabled(bool enabledTip)
        {
            this.tipEnabled = enabledTip;
        }

        /// <summary>
        /// 波次开始时由 WaveManager 调用，记录阶段并提示 Boss 波。
        /// </summary>
        /// <param name="waveIndex">当前波次。</param>
        /// <param name="difficultyScale">WaveManager 当前难度倍率。</param>
        public void OnWaveStarted(int waveIndex, float difficultyScale)
        {
            this.EnsureInitialized();
            if (!this.enabled)
            {
                return;
            }

            this.currentWaveIndex = waveIndex;
            this.currentWaveIsBoss = WaveBossRewardTool.IsBossWave(waveIndex);
            this.UpdateState(this.currentWaveIsBoss ? WavePhaseType.BossWave : WavePhaseType.NormalWave);

            if (this.currentWaveIsBoss)
            {
                this.ShowTip($"第 {waveIndex} 波 Boss 来袭! 难度 {difficultyScale:0.00}x");
            }
        }

        /// <summary>
        /// 根据 Boss 波规则修正敌人数量。
        /// </summary>
        /// <param name="waveIndex">当前波次。</param>
        /// <param name="baseEnemyCount">WaveManager 原始敌人数。</param>
        /// <returns>最终敌人数。</returns>
        public int GetEnemyCountForWave(int waveIndex, int baseEnemyCount)
        {
            if (!this.enabled)
            {
                return baseEnemyCount;
            }

            return WaveBossRewardTool.GetEnemyCountForWave(baseEnemyCount, waveIndex);
        }

        /// <summary>
        /// 配置本波生成的敌人属性。
        /// Boss 波最后一名敌人会被放大为 Boss，其余敌人获得普通难度缩放。
        /// </summary>
        /// <param name="enemyObject">敌人根对象。</param>
        /// <param name="waveIndex">当前波次。</param>
        /// <param name="spawnIndex">本波内生成序号，从 0 开始。</param>
        /// <param name="totalEnemies">本波总敌人数。</param>
        /// <param name="difficultyScale">WaveManager 当前难度倍率。</param>
        public void ConfigureSpawnedEnemy(
            GameObject enemyObject,
            int waveIndex,
            int spawnIndex,
            int totalEnemies,
            float difficultyScale)
        {
            if (!this.enabled || enemyObject == null)
            {
                return;
            }

            AEnemy enemy = enemyObject.GetComponent<AEnemy>();
            AEnemy.EnemyData enemyData = enemy == null ? null : enemy.CharacterDataLAB as AEnemy.EnemyData;
            if (enemy == null || enemyData == null)
            {
                return;
            }

            bool isBoss = WaveBossRewardTool.IsBossEnemySpawn(waveIndex, spawnIndex, totalEnemies);
            if (isBoss)
            {
                this.ApplyBossScale(enemy, enemyData, waveIndex, difficultyScale);
                return;
            }

            this.ApplyNormalScale(enemyData, waveIndex, difficultyScale);
        }

        /// <summary>
        /// 选择当前奖励。
        /// </summary>
        /// <param name="optionIndex">奖励索引，0 到 2。</param>
        /// <returns>是否选择成功。</returns>
        public bool SelectReward(int optionIndex)
        {
            this.EnsureInitialized();
            if (!this.enabled || optionIndex < 0 || optionIndex >= this.currentOptions.Count)
            {
                return false;
            }

            WaveRewardOption option = this.currentOptions[optionIndex];
            this.ApplyReward(option);
            this.currentOptions.Clear();
            this.OnRewardSelected?.Invoke(option);
            this.OnRewardOptionsChanged?.Invoke(this.currentOptions);
            this.UpdateState(WavePhaseType.Resting);
            this.ShowTip($"已选择奖励: {option.Title}，{option.Description}");
            return true;
        }

        /// <summary>
        /// 构建调试用奖励选项。
        /// </summary>
        /// <param name="bossReward">是否按 Boss 波奖励生成。</param>
        public void CreateDebugRewardOptions(bool bossReward)
        {
            this.EnsureInitialized();
            this.CreateRewardOptions(WaveBossRewardTool.ClampWaveIndex(this.currentWaveIndex), bossReward);
        }

        /// <summary>
        /// 应用玩家伤害强化。
        /// </summary>
        /// <param name="attacker">攻击者。</param>
        /// <param name="baseDamage">基础伤害。</param>
        /// <returns>奖励倍率调整后的伤害。</returns>
        public float GetAdjustedPlayerOutgoingDamage(Character attacker, float baseDamage)
        {
            if (!this.enabled || attacker is not Player)
            {
                return baseDamage;
            }

            return WeatherGameplayTool.ApplyMultiplier(baseDamage, 1.0f + this.playerDamageBonus, 0.0f);
        }

        /// <summary>
        /// 应用玩家减伤强化。
        /// </summary>
        /// <param name="target">受击目标。</param>
        /// <param name="baseDamage">基础伤害。</param>
        /// <returns>减伤后的安全伤害。</returns>
        public float GetAdjustedIncomingDamage(Character target, float baseDamage)
        {
            if (!this.enabled || target is not Player)
            {
                return baseDamage;
            }

            return WeatherGameplayTool.ApplyMultiplier(baseDamage, 1.0f - this.playerDamageReduction, 0.1f);
        }

        /// <summary>
        /// 应用玩家移动强化。
        /// </summary>
        /// <param name="player">玩家。</param>
        /// <param name="baseSpeed">基础速度。</param>
        /// <returns>移动强化后的速度。</returns>
        public float GetAdjustedPlayerMoveSpeed(Player player, float baseSpeed)
        {
            if (!this.enabled || player == null)
            {
                return baseSpeed;
            }

            return WeatherGameplayTool.ApplyMultiplier(baseSpeed, 1.0f + this.playerMoveSpeedBonus, 0.0f);
        }

        /// <summary>
        /// 延迟订阅 WaveManager 事件。
        /// </summary>
        private void EnsureInitialized()
        {
            if (this.initialized)
            {
                return;
            }

            this.initialized = true;
            this.SubscribeWaveManager();
        }

        /// <summary>
        /// 订阅波次生命周期事件。
        /// </summary>
        private void SubscribeWaveManager()
        {
            try
            {
                WaveManager manager = WaveManager.Instance;
                if (manager == null)
                {
                    return;
                }

                manager.OnWaveEnd -= this.HandleWaveEnd;
                manager.OnRestStart -= this.HandleRestStart;
                manager.OnAllWavesCleared -= this.HandleAllWavesCleared;
                manager.OnWaveEnd += this.HandleWaveEnd;
                manager.OnRestStart += this.HandleRestStart;
                manager.OnAllWavesCleared += this.HandleAllWavesCleared;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[WaveBossReward] 订阅波次事件失败: " + exception.Message);
            }
        }

        /// <summary>
        /// 普通敌人属性缩放。
        /// </summary>
        /// <param name="enemyData">敌人数据。</param>
        /// <param name="waveIndex">当前波次。</param>
        /// <param name="difficultyScale">难度倍率。</param>
        private void ApplyNormalScale(AEnemy.EnemyData enemyData, int waveIndex, float difficultyScale)
        {
            float healthMultiplier = WaveBossRewardTool.GetNormalEnemyHealthMultiplier(waveIndex, difficultyScale);
            float attackMultiplier = WaveBossRewardTool.GetNormalEnemyAttackMultiplier(waveIndex, difficultyScale);
            float defenseMultiplier = WaveBossRewardTool.GetNormalEnemyDefenseMultiplier(waveIndex, difficultyScale);
            this.ApplyEnemyScale(enemyData, healthMultiplier, attackMultiplier, defenseMultiplier);
        }

        /// <summary>
        /// Boss 敌人属性和视觉缩放。
        /// </summary>
        /// <param name="enemy">敌人组件。</param>
        /// <param name="enemyData">敌人数据。</param>
        /// <param name="waveIndex">当前波次。</param>
        /// <param name="difficultyScale">难度倍率。</param>
        private void ApplyBossScale(AEnemy enemy, AEnemy.EnemyData enemyData, int waveIndex, float difficultyScale)
        {
            float healthMultiplier = WaveBossRewardTool.GetBossHealthMultiplier(waveIndex, difficultyScale);
            float attackMultiplier = WaveBossRewardTool.GetBossAttackMultiplier(waveIndex, difficultyScale);
            float defenseMultiplier = WaveBossRewardTool.GetBossDefenseMultiplier(waveIndex, difficultyScale);
            this.ApplyEnemyScale(enemyData, healthMultiplier, attackMultiplier, defenseMultiplier);

            enemy.gameObject.name = WaveBossRewardTool.BuildBossName(enemy.gameObject.name, waveIndex);
            enemy.transform.localScale *= WaveBossRewardConstant.BossVisualScale;

            SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = Color.Lerp(renderer.color, new Color32(255, 120, 90, 255), 0.45f);
            }
        }

        /// <summary>
        /// 对敌人数据应用倍率。
        /// </summary>
        /// <param name="enemyData">敌人数据。</param>
        /// <param name="healthMultiplier">生命倍率。</param>
        /// <param name="attackMultiplier">攻击倍率。</param>
        /// <param name="defenseMultiplier">防御倍率。</param>
        private void ApplyEnemyScale(
            AEnemy.EnemyData enemyData,
            float healthMultiplier,
            float attackMultiplier,
            float defenseMultiplier)
        {
            enemyData.MaxHp = WaveBossRewardTool.ScaleAttribute(enemyData.MaxHp, healthMultiplier, 1.0f);
            enemyData.Hp = enemyData.MaxHp;
            enemyData.ATN = WaveBossRewardTool.ScaleAttribute(enemyData.ATN, attackMultiplier, 0.1f);
            enemyData.INT = WaveBossRewardTool.ScaleAttribute(enemyData.INT, attackMultiplier, 0.0f);
            enemyData.DEF = WaveBossRewardTool.ScaleAttribute(enemyData.DEF, defenseMultiplier, 0.0f);
            enemyData.RES = WaveBossRewardTool.ScaleAttribute(enemyData.RES, defenseMultiplier, 0.0f);
        }

        /// <summary>
        /// 波次结束后生成奖励选项。
        /// </summary>
        /// <param name="waveIndex">刚结束的波次。</param>
        /// <param name="totalCompleted">已完成波次数。</param>
        private void HandleWaveEnd(int waveIndex, int totalCompleted)
        {
            if (!this.enabled)
            {
                return;
            }

            this.CreateRewardOptions(waveIndex, WaveBossRewardTool.IsBossWave(waveIndex));
        }

        /// <summary>
        /// 波间休息开始回调。
        /// 如果仍有待选奖励，保持奖励选择阶段，避免 UI 被休息阶段覆盖。
        /// </summary>
        /// <param name="duration">休息时长。</param>
        private void HandleRestStart(float duration)
        {
            if (!this.enabled || this.currentOptions.Count > 0)
            {
                return;
            }

            this.UpdateState(WavePhaseType.Resting);
        }

        /// <summary>
        /// 全部波次完成回调。
        /// </summary>
        /// <param name="totalWaves">总完成波次。</param>
        private void HandleAllWavesCleared(int totalWaves)
        {
            if (!this.enabled)
            {
                return;
            }

            this.currentOptions.Clear();
            this.UpdateState(WavePhaseType.Completed);
            this.OnRewardOptionsChanged?.Invoke(this.currentOptions);
        }

        /// <summary>
        /// 创建三选一奖励。
        /// </summary>
        /// <param name="waveIndex">波次。</param>
        /// <param name="bossReward">是否为 Boss 波奖励。</param>
        private void CreateRewardOptions(int waveIndex, bool bossReward)
        {
            this.currentOptions.Clear();
            List<WaveRewardType> pool = new List<WaveRewardType>
            {
                WaveRewardType.Heal,
                WaveRewardType.Experience,
                WaveRewardType.DamageBoost,
                WaveRewardType.DefenseBoost,
                WaveRewardType.MoveSpeedBoost,
            };

            int optionCount = WaveBossRewardTool.GetRewardOptionCount(WaveBossRewardConstant.RewardOptionCount, pool.Count);
            for (int i = 0; i < optionCount; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, pool.Count);
                WaveRewardType rewardType = pool[randomIndex];
                pool.RemoveAt(randomIndex);
                float value = WaveBossRewardTool.GetRewardValue(rewardType, bossReward, waveIndex);
                this.currentOptions.Add(new WaveRewardOption
                {
                    Id = $"A004_W{waveIndex}_{rewardType}",
                    WaveIndex = waveIndex,
                    IsBossReward = bossReward,
                    RewardType = rewardType,
                    Value = value,
                    Title = WaveBossRewardTool.GetRewardName(rewardType),
                    Description = WaveBossRewardTool.BuildRewardDescription(rewardType, value),
                });
            }

            this.UpdateState(WavePhaseType.RewardSelection);
            this.OnRewardOptionsChanged?.Invoke(this.currentOptions);
            this.ShowTip(bossReward ? "Boss 已击败，选择强化奖励!" : "波次清除，选择一项补给奖励。");
        }

        /// <summary>
        /// 应用奖励到玩家或本局 Buff。
        /// </summary>
        /// <param name="option">奖励选项。</param>
        private void ApplyReward(WaveRewardOption option)
        {
            Player player = PlayerManager.Instance?.Mine;
            switch (option.RewardType)
            {
                case WaveRewardType.Heal:
                    if (player != null)
                    {
                        player.AddHp(player.CharacterDataLAB.MaxHp * option.Value);
                    }

                    break;
                case WaveRewardType.Experience:
                    if (player != null)
                    {
                        player.AddExperienceValue(WaveBossRewardTool.ToRoundedInt(option.Value));
                    }

                    break;
                case WaveRewardType.DamageBoost:
                    this.playerDamageBonus = WaveBossRewardTool.AddWithCap(
                        this.playerDamageBonus,
                        option.Value,
                        WaveBossRewardConstant.MaxPlayerDamageBonus);
                    break;
                case WaveRewardType.DefenseBoost:
                    this.playerDamageReduction = WaveBossRewardTool.AddWithCap(
                        this.playerDamageReduction,
                        option.Value,
                        WaveBossRewardConstant.MaxPlayerDamageReduction);
                    break;
                case WaveRewardType.MoveSpeedBoost:
                    this.playerMoveSpeedBonus = WaveBossRewardTool.AddWithCap(
                        this.playerMoveSpeedBonus,
                        option.Value,
                        WaveBossRewardConstant.MaxPlayerMoveSpeedBonus);
                    break;
            }
        }

        /// <summary>
        /// 更新状态并通知外部。
        /// </summary>
        /// <param name="phase">当前阶段。</param>
        private void UpdateState(WavePhaseType phase)
        {
            this.CurrentState = new WaveBossRewardState
            {
                Phase = phase,
                CurrentWaveIndex = this.currentWaveIndex,
                IsBossWave = this.currentWaveIsBoss,
                HasPendingReward = this.currentOptions.Count > 0,
                PendingRewardCount = this.currentOptions.Count,
                SystemEnabled = this.enabled,
                PlayerDamageBonus = this.playerDamageBonus,
                PlayerDamageReduction = this.playerDamageReduction,
                PlayerMoveSpeedBonus = this.playerMoveSpeedBonus,
            };

            this.OnStateChanged?.Invoke(this.CurrentState);
        }

        /// <summary>
        /// 显示提示。
        /// </summary>
        /// <param name="message">提示文本。</param>
        private void ShowTip(string message)
        {
            this.OnRewardTipRequested?.Invoke(message);
            if (!this.tipEnabled)
            {
                return;
            }

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
                Debug.LogWarning("[WaveBossReward] 显示 Tip 失败: " + exception.Message);
            }

            Debug.Log("[波次奖励] " + message);
        }
    }

    /// <summary>
    /// 波间奖励选项数据。
    /// 由 WaveBossRewardManager 创建，供 UI 按钮和 Editor 调试只读展示。
    /// </summary>
    [Serializable]
    public class WaveRewardOption
    {
        /// <summary>奖励唯一标识。</summary>
        public string Id;

        /// <summary>生成该奖励的波次。</summary>
        public int WaveIndex;

        /// <summary>是否为 Boss 波奖励。</summary>
        public bool IsBossReward;

        /// <summary>奖励类型。</summary>
        public WaveRewardType RewardType;

        /// <summary>奖励数值。百分比奖励使用 0.1 表示 10%。</summary>
        public float Value;

        /// <summary>奖励标题。</summary>
        public string Title;

        /// <summary>奖励描述。</summary>
        public string Description;

        /// <summary>
        /// 构建按钮文本。
        /// </summary>
        /// <param name="index">按钮序号。</param>
        /// <returns>适合按钮展示的多行文本。</returns>
        public string ToButtonText(int index)
        {
            string prefix = this.IsBossReward ? "Boss奖励" : "波间奖励";
            return $"{index + 1}. {this.Title}\n<size=14>{prefix} | {this.Description}</size>";
        }
    }

    /// <summary>
    /// 波次 Boss 与奖励系统状态快照。
    /// </summary>
    [Serializable]
    public class WaveBossRewardState
    {
        /// <summary>当前阶段。</summary>
        public WavePhaseType Phase;

        /// <summary>当前波次。</summary>
        public int CurrentWaveIndex;

        /// <summary>当前波是否为 Boss 波。</summary>
        public bool IsBossWave;

        /// <summary>是否存在待选奖励。</summary>
        public bool HasPendingReward;

        /// <summary>待选奖励数量。</summary>
        public int PendingRewardCount;

        /// <summary>系统是否启用。</summary>
        public bool SystemEnabled;

        /// <summary>玩家本局伤害加成。</summary>
        public float PlayerDamageBonus;

        /// <summary>玩家本局减伤。</summary>
        public float PlayerDamageReduction;

        /// <summary>玩家本局移动加成。</summary>
        public float PlayerMoveSpeedBonus;

        /// <summary>
        /// 创建默认状态。
        /// </summary>
        /// <returns>默认状态。</returns>
        public static WaveBossRewardState CreateDefault()
        {
            return new WaveBossRewardState
            {
                Phase = WavePhaseType.Idle,
                CurrentWaveIndex = 0,
                IsBossWave = false,
                HasPendingReward = false,
                PendingRewardCount = 0,
                SystemEnabled = true,
                PlayerDamageBonus = 0.0f,
                PlayerDamageReduction = 0.0f,
                PlayerMoveSpeedBonus = 0.0f,
            };
        }

        /// <summary>
        /// 构建状态摘要。
        /// </summary>
        /// <returns>适合 HUD 和 Editor 展示的多行文本。</returns>
        public string ToSummaryText()
        {
            string phaseText = this.Phase switch
            {
                WavePhaseType.NormalWave => "普通波次",
                WavePhaseType.BossWave => "Boss 波",
                WavePhaseType.RewardSelection => "奖励选择",
                WavePhaseType.Resting => "波间休息",
                WavePhaseType.Completed => "全部完成",
                _ => "空闲",
            };

            return $"A004 波次强化: {(this.SystemEnabled ? "已启用" : "已禁用")}\n" +
                $"阶段: {phaseText} | 波次: {this.CurrentWaveIndex}\n" +
                $"待选奖励: {(this.HasPendingReward ? this.PendingRewardCount.ToString() : "无")}\n" +
                WaveBossRewardTool.BuildBuffSummary(
                    this.PlayerDamageBonus,
                    this.PlayerDamageReduction,
                    this.PlayerMoveSpeedBonus);
        }
    }
}
