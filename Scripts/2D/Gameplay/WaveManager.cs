namespace LAB2D
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Random = UnityEngine.Random;

    /// <summary>
    /// 波次管理器 — 将敌人生成从固定间隔改为波次递增模式。
    /// 独立于 EnemyManager 的存档/加载逻辑，仅控制运行时生成节奏。
    /// 启用后会自动接管 EnemyManager.GenEnemy() 的生成职责。
    ///
    /// 接入方式：
    ///   1. 调用 WaveManager.Instance.StartWaves() 启动波次
    ///   2. 或设置 WaveConfig.autoStart = true 让 WaveManager 在 Start 时自动启动
    ///   3. Editor 菜单：工具 > 波次管理 > 开始波次 / 停止波次
    ///   4. 不需要波次时调用 StopWaves() 恢复默认的固定间隔生成模式
    /// </summary>
    public class WaveManager : Singleton<WaveManager>
    {
        /// <summary>
        /// 波次配置
        /// </summary>
        public WaveConfig Config { get; private set; } = new WaveConfig();

        /// <summary>
        /// 当前波次索引（从 1 开始，0 表示未开始）
        /// </summary>
        public int CurrentWaveIndex { get; private set; }

        /// <summary>
        /// 当前波次中存活的敌人数量
        /// </summary>
        public int EnemiesAliveInWave { get; private set; }

        /// <summary>
        /// 当前波次中已击杀的敌人数量
        /// </summary>
        public int EnemiesDefeatedInWave { get; private set; }

        /// <summary>
        /// 是否正在波次战斗中
        /// </summary>
        public bool IsWaveActive { get; private set; }

        /// <summary>
        /// 是否在波间休息中
        /// </summary>
        public bool IsResting { get; private set; }

        /// <summary>
        /// 已完成的波次总数
        /// </summary>
        public int TotalWavesCompleted { get; private set; }

        /// <summary>
        /// 当前难度缩放因子（基于已完成波次计算）
        /// </summary>
        public float CurrentDifficultyScale
        {
            get
            {
                return 1.0f + (this.TotalWavesCompleted * this.Config.difficultyScalePerWave);
            }
        }

        /// <summary>
        /// 波次开始事件，参数为波次索引
        /// </summary>
        public event Action<int> OnWaveStart;

        /// <summary>
        /// 波次结束事件，参数为波次索引和是否玩家存活
        /// </summary>
        public event Action<int, int> OnWaveEnd;

        /// <summary>
        /// 所有波次完成事件（仅在设置了 totalWaves > 0 时触发）
        /// </summary>
        public event Action<int> OnAllWavesCleared;

        /// <summary>
        /// 波间休息开始事件
        /// </summary>
        public event Action<float> OnRestStart;

        /// <summary>
        /// 单帧波次状态更新事件，用于 UI 刷新
        /// </summary>
        public event Action OnWaveStateChanged;

        private Coroutine waveCoroutine;
        private int enemiesAliveBeforeWave;
        private int enemiesSpawnedThisWave;

        /// <summary>
        /// 启动波次系统（接管敌人生成控制权）
        /// </summary>
        public void StartWaves()
        {
            if (this.waveCoroutine != null)
            {
                return;
            }

            EnemyManager.IsWaveControlEnabled = true;
            this.ResetState();
            this.waveCoroutine = TileMap.Instance.StartCoroutine(this.WaveLoop());
        }

        /// <summary>
        /// 停止波次系统（恢复默认敌人生成模式）
        /// </summary>
        public void StopWaves()
        {
            EnemyManager.IsWaveControlEnabled = false;
            if (this.waveCoroutine != null)
            {
                TileMap.Instance.StopCoroutine(this.waveCoroutine);
                this.waveCoroutine = null;
            }

            this.IsWaveActive = false;
            this.IsResting = false;
        }

        /// <summary>
        /// 添加波次统计到 GameplaySessionStats（用于外部调用记录波次完成）
        /// </summary>
        public WaveSummary GetWaveSummary()
        {
            return new WaveSummary
            {
                currentWaveIndex = this.CurrentWaveIndex,
                totalWavesCompleted = this.TotalWavesCompleted,
                enemiesDefeatedInWave = this.EnemiesDefeatedInWave,
                enemiesAliveInWave = this.EnemiesAliveInWave,
                difficultyScale = this.CurrentDifficultyScale,
                isWaveActive = this.IsWaveActive,
                isResting = this.IsResting,
            };
        }

        /// <summary>
        /// 重置所有波次状态
        /// </summary>
        private void ResetState()
        {
            this.CurrentWaveIndex = 0;
            this.TotalWavesCompleted = 0;
            this.EnemiesAliveInWave = 0;
            this.EnemiesDefeatedInWave = 0;
            this.enemiesAliveBeforeWave = 0;
            this.enemiesSpawnedThisWave = 0;
            this.IsWaveActive = false;
            this.IsResting = false;
        }

        /// <summary>
        /// 波次主循环协程
        /// </summary>
        private IEnumerator WaveLoop()
        {
            // 等待地图初始化完成
            yield return new WaitUntil(() => Lock.IsCompleteTileMap);

            while (true)
            {
                // 检查是否达到总波次上限
                if (this.Config.totalWaves > 0 && this.TotalWavesCompleted >= this.Config.totalWaves)
                {
                    this.OnAllWavesCleared?.Invoke(this.TotalWavesCompleted);
                    this.StopWaves();
                    yield break;
                }

                // 波间休息
                if (this.TotalWavesCompleted > 0)
                {
                    this.IsResting = true;
                    this.OnRestStart?.Invoke(this.Config.restTimeBetweenWaves);
                    this.OnWaveStateChanged?.Invoke();
                    yield return new WaitForSeconds(this.Config.restTimeBetweenWaves);
                    this.IsResting = false;
                }

                // 开始新波次
                this.CurrentWaveIndex++;
                this.EnemiesDefeatedInWave = 0;
                this.enemiesSpawnedThisWave = 0;

                // 记录波次前实际存活的敌人数（排除 null 引用，因销毁后列表不自动清理）
                this.enemiesAliveBeforeWave = this.CountAliveEnemies();

                this.IsWaveActive = true;
                this.OnWaveStart?.Invoke(this.CurrentWaveIndex);
                this.OnWaveStateChanged?.Invoke();

                // 生成当前波次的所有敌人
                int enemiesInWave = this.GetEnemyCountForWave(this.CurrentWaveIndex);
                for (int i = 0; i < enemiesInWave; i++)
                {
                    // 检查最大同时存活敌人限制
                    if (this.CountAliveEnemies() >= this.Config.maxAliveEnemies)
                    {
                        // 等待有空位再继续生成
                        yield return new WaitForSeconds(1.0f);
                    }

                    // 在随机可到达位置生成敌人
                    Vector3 spawnPos = this.GetSpawnPosition();
                    GameObject enemyObj = EnemyManager.Instance.Create(spawnPos);
                    if (enemyObj != null)
                    {
                        this.enemiesSpawnedThisWave++;
                    }

                    // 波内生成间隔（第一只立即生成，后续按间隔）
                    if (i < enemiesInWave - 1)
                    {
                        yield return new WaitForSeconds(this.Config.spawnInterval);
                    }
                }

                this.EnemiesAliveInWave = this.enemiesSpawnedThisWave;
                this.OnWaveStateChanged?.Invoke();

                // 等待波次清理：当存活敌人数量降至波次前水平时，认为波次完成
                yield return this.WaitForWaveClear();

                // 波次完成
                this.IsWaveActive = false;
                this.EnemiesAliveInWave = 0;
                this.TotalWavesCompleted++;
                this.OnWaveEnd?.Invoke(this.CurrentWaveIndex, this.TotalWavesCompleted);
                this.OnWaveStateChanged?.Invoke();
            }
        }

        /// <summary>
        /// 等待当前波次内所有敌人被击杀
        /// </summary>
        private IEnumerator WaitForWaveClear()
        {
            while (true)
            {
                yield return new WaitForSeconds(1.0f);

                // 检查 Player 是否存活
                Player player = PlayerManager.Instance?.Mine;
                if (player == null || player.CharacterDataLAB.Hp <= 0)
                {
                    // 玩家死亡，等待重生后继续当前波次
                    yield return new WaitForSeconds(3.0f);
                    continue;
                }

                // 波次清理条件：波内至少生成了一只敌人，且实际存活敌人降回波前水平
                int currentAlive = this.CountAliveEnemies();
                bool allWaveEnemiesDefeated = this.enemiesSpawnedThisWave > 0
                    && currentAlive <= this.enemiesAliveBeforeWave;

                if (allWaveEnemiesDefeated)
                {
                    // 额外等待一小段时间，确保敌人死亡动画和清理完成
                    yield return new WaitForSeconds(1.0f);
                    this.EnemiesDefeatedInWave = this.enemiesSpawnedThisWave;
                    break;
                }
            }
        }

        /// <summary>
        /// 计算指定波次的敌人数量
        /// </summary>
        private int GetEnemyCountForWave(int waveIndex)
        {
            return Mathf.Max(1, this.Config.baseEnemyCount + ((waveIndex - 1) * this.Config.enemiesPerWaveIncrease));
        }

        /// <summary>
        /// 统计 EnemyManager 中实际存活的敌人数量（排除已销毁的 null 引用）
        /// EnemyManager.Characters 在被 PhotonNetwork.Destroy 后不会自动清理 null 条目，因此不能直接使用 Count()。
        /// </summary>
        private int CountAliveEnemies()
        {
            int count = 0;
            foreach (AEnemy enemy in EnemyManager.Instance.Characters)
            {
                if (enemy != null)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 获取敌人生成位置（优先随机可到达位置，回退到默认位置）
        /// </summary>
        private Vector3 GetSpawnPosition()
        {
            if (this.Config.useRandomSpawnPositions && TileMap.Instance != null)
            {
                try
                {
                    Vector3 centerMap = default;
                    Player player = PlayerManager.Instance?.Mine;
                    if (player != null)
                    {
                        centerMap = TileMap.Instance.WorldPosToMapPos(player.transform.position);
                    }

                    return TileMap.Instance.MapPosToWorldPos(TileMap.Instance.GenCanReachPos(centerMap));
                }
                catch (Exception exception)
                {
                    LogManager.Instance.Log(
                        $"WaveManager.GetSpawnPosition failed, fallback to Vector3.zero.\n{exception}",
                        LogManager.LogLevelEnum.Error);

                    // 回退到默认位置
                }
            }

            return Vector3.zero;
        }
    }

    /// <summary>
    /// 波次配置 — 控制波次生成节奏和难度缩放参数
    /// </summary>
    [Serializable]
    public class WaveConfig
    {
        /// <summary>
        /// 第一波敌人基础数量
        /// </summary>
        public int baseEnemyCount = 3;

        /// <summary>
        /// 每波增加的敌人数量
        /// </summary>
        public int enemiesPerWaveIncrease = 2;

        /// <summary>
        /// 波间休息时间（秒）
        /// </summary>
        public float restTimeBetweenWaves = 15.0f;

        /// <summary>
        /// 波内敌人生成间隔（秒）
        /// </summary>
        public float spawnInterval = 2.0f;

        /// <summary>
        /// 最大同时存活敌人数量
        /// </summary>
        public int maxAliveEnemies = 20;

        /// <summary>
        /// 总波次数（0 表示无限波次）
        /// </summary>
        public int totalWaves = 0;

        /// <summary>
        /// 每波难度缩放系数（每完成一波，敌人整体强度增加此比例）
        /// </summary>
        public float difficultyScalePerWave = 0.1f;

        /// <summary>
        /// 是否使用随机生成位置（true: 随机可到达位置, false: Vector3.zero）
        /// </summary>
        public bool useRandomSpawnPositions = true;
    }

    /// <summary>
    /// 波次摘要数据 — 用于 UI 展示和统计记录
    /// </summary>
    [Serializable]
    public class WaveSummary
    {
        public int currentWaveIndex;
        public int totalWavesCompleted;
        public int enemiesDefeatedInWave;
        public int enemiesAliveInWave;
        public float difficultyScale;
        public bool isWaveActive;
        public bool isResting;
    }
}
