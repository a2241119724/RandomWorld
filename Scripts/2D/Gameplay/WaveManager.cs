namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Data;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Wave;
    using LAB2D.UnityAdapter;
    using System;
    using System.Collections;

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
    public class WaveManager : ASingletonSaveData<WaveManager>, IWaveStateProvider
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
                return this.flowService.GetDifficultyScale(this.runtimeState, this.CreateWaveConfigModel());
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

        private object waveCoroutine;
        private readonly WaveRuntimeState runtimeState = new WaveRuntimeState();
        private readonly WaveFlowService flowService = new WaveFlowService();
        private readonly WaveSpawnPlanService spawnPlanService = new WaveSpawnPlanService();
        private IWaveSceneAdapter sceneAdapter = new UnityWaveSceneAdapter();
        private IWaveTimeScheduler timeScheduler = new UnityWaveTimeScheduler();
        private EventBus eventBus;

        private EventBus EventBus
        {
            get
            {
                if (this.eventBus == null)
                {
                    this.eventBus = Core.ServiceLocator.Get<EventBus>();
                }

                return this.eventBus;
            }
        }

        /// <summary>
        /// 替换 WaveManager 的 Unity 场景访问桥。传入 null 会恢复默认 Unity 实现。
        /// </summary>
        public void SetSceneAdapter(IWaveSceneAdapter adapter)
        {
            this.sceneAdapter = adapter ?? new UnityWaveSceneAdapter();
        }

        /// <summary>
        /// 替换 WaveManager 的协程调度桥。传入 null 会恢复默认 Unity 实现。
        /// </summary>
        public void SetTimeScheduler(IWaveTimeScheduler scheduler)
        {
            this.timeScheduler = scheduler ?? new UnityWaveTimeScheduler();
        }

        /// <summary>
        /// 启动波次系统（接管敌人生成控制权）
        /// </summary>
        public void StartWaves()
        {
            if (this.waveCoroutine != null)
            {
                return;
            }

            this.sceneAdapter.SetWaveControlEnabled(true);
            this.ResetState();
            this.waveCoroutine = this.timeScheduler.Start(this.WaveLoop());
        }

        /// <summary>
        /// 停止波次系统（恢复默认敌人生成模式）
        /// </summary>
        public void StopWaves()
        {
            this.sceneAdapter.SetWaveControlEnabled(false);
            if (this.waveCoroutine != null)
            {
                this.timeScheduler.Stop(this.waveCoroutine);
                this.waveCoroutine = null;
            }

            this.flowService.Stop(this.runtimeState);
            this.SyncPublicStateFromRuntime();
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
            this.flowService.Reset(this.runtimeState);
            this.SyncPublicStateFromRuntime();
        }

        /// <summary>
        /// 波次主循环协程
        /// </summary>
        private IEnumerator WaveLoop()
        {
            // 等待地图初始化完成
            yield return this.timeScheduler.WaitUntilMapReady();

            while (true)
            {
                // 检查是否达到总波次上限
                if (this.flowService.TryCreateAllWavesClearedDecision(
                    this.runtimeState,
                    this.CreateWaveConfigModel(),
                    out WaveFlowDecision allWavesClearedDecision))
                {
                    this.OnAllWavesCleared?.Invoke(allWavesClearedDecision.TotalWavesCompleted);
                    this.EventBus.PublishInternal(new AllWavesClearedEvent { TotalWavesCompleted = allWavesClearedDecision.TotalWavesCompleted });
                    this.StopWaves();
                    yield break;
                }

                // 波间休息
                if (this.TotalWavesCompleted > 0)
                {
                    WaveFlowDecision restDecision = this.flowService.BeginRestAndCreateDecision(
                        this.runtimeState,
                        this.Config.restTimeBetweenWaves);
                    this.SyncPublicStateFromRuntime();
                    this.OnRestStart?.Invoke(restDecision.RestDuration);
                    this.EventBus.PublishInternal(new WaveRestStartedEvent { RestDuration = restDecision.RestDuration });
                    this.OnWaveStateChanged?.Invoke();
                    yield return this.timeScheduler.WaitForSeconds(restDecision.RestDuration);
                    this.flowService.EndRest(this.runtimeState);
                    this.SyncPublicStateFromRuntime();
                }

                // 开始新波次
                WaveFlowDecision waveStartedDecision = this.flowService.BeginNextWaveAndCreateDecision(
                    this.runtimeState,
                    this.CountAliveEnemies(),
                    0,
                    this.CreateWaveConfigModel());
                this.SyncPublicStateFromRuntime();

                // 波前存活数已交给 WaveRuntimeState 保存，Unity 侧只负责读取场景数量。
                // A004：通知 Boss 与波间奖励系统同步当前波次阶段，保持波次系统仍为主流程。
                this.sceneAdapter.OnWaveStarted(waveStartedDecision.WaveIndex, waveStartedDecision.DifficultyScale);
                this.OnWaveStart?.Invoke(waveStartedDecision.WaveIndex);
                this.EventBus.PublishInternal(new WaveStartedEvent { WaveIndex = waveStartedDecision.WaveIndex, DifficultyScale = waveStartedDecision.DifficultyScale });
                this.OnWaveStateChanged?.Invoke();

                // 生成当前波次的所有敌人
                WaveSpawnPlan spawnPlan = this.CreateSpawnPlan();
                for (int i = 0; i < spawnPlan.Requests.Count; i++)
                {
                    WaveSpawnRequest spawnRequest = spawnPlan.Requests[i];
                    int maxAliveEnemies = this.GetEffectiveMaxAliveEnemies();
                    // 检查最大同时存活敌人限制
                    while (this.CountAliveEnemies() >= maxAliveEnemies)
                    {
                        // 等待有空位再继续生成
                        yield return this.timeScheduler.WaitForSeconds(1.0f);
                    }

                    if (this.sceneAdapter.TrySpawnEnemy(this.Config.useRandomSpawnPositions, spawnRequest))
                    {
                        // A004：生成后立即套用普通难度缩放或 Boss 缩放，不改敌人 Prefab 本体。
                        this.flowService.RegisterSpawnSuccess(this.runtimeState);
                        this.SyncPublicStateFromRuntime();
                    }

                    // 波内生成间隔（第一只立即生成，后续按间隔）
                    if (i < spawnPlan.Requests.Count - 1)
                    {
                        yield return this.timeScheduler.WaitForSeconds(this.Config.spawnInterval);
                    }
                }

                this.flowService.SyncAliveCountAfterSpawning(this.runtimeState);
                this.SyncPublicStateFromRuntime();
                this.OnWaveStateChanged?.Invoke();

                // 等待波次清理：当存活敌人数量降至波次前水平时，认为波次完成
                WaveFlowDecision waitDecision = this.flowService.CreateWaitForWaveClearDecision(this.runtimeState);
                yield return this.WaitForWaveClear(waitDecision);

                // 波次完成
                WaveFlowDecision waveCompletedDecision = this.flowService.CompleteCurrentWaveAndCreateDecision(this.runtimeState);
                this.SyncPublicStateFromRuntime();
                this.OnWaveEnd?.Invoke(waveCompletedDecision.WaveIndex, waveCompletedDecision.TotalWavesCompleted);
                this.EventBus.PublishInternal(new WaveEndedEvent { WaveIndex = waveCompletedDecision.WaveIndex, TotalWavesCompleted = waveCompletedDecision.TotalWavesCompleted });
                this.OnWaveStateChanged?.Invoke();
            }
        }

        /// <summary>
        /// 等待当前波次内所有敌人被击杀
        /// </summary>
        private IEnumerator WaitForWaveClear(WaveFlowDecision decision)
        {
            if (decision == null || decision.Type != WaveFlowDecisionType.WaitForWaveClear)
            {
                yield break;
            }

            while (true)
            {
                yield return this.timeScheduler.WaitForSeconds(1.0f);

                // 检查 Player 是否存活
                if (!this.sceneAdapter.IsPlayerAlive())
                {
                    // 玩家死亡，等待重生后继续当前波次
                    yield return this.timeScheduler.WaitForSeconds(3.0f);
                    continue;
                }

                // 波次清理条件：波内至少生成了一只敌人，且实际存活敌人降回波前水平
                int currentAlive = this.CountAliveEnemies();
                bool allWaveEnemiesDefeated = this.flowService.IsCurrentWaveCleared(this.runtimeState, currentAlive);

                if (allWaveEnemiesDefeated)
                {
                    // 额外等待一小段时间，确保敌人死亡动画和清理完成
                    yield return this.timeScheduler.WaitForSeconds(1.0f);
                    break;
                }
            }
        }

        /// <summary>
        /// 计算指定波次的敌人数量
        /// </summary>
        private int GetEnemyCountForWave(int waveIndex)
        {
            return this.flowService.GetEnemyCountForWave(waveIndex, this.CreateWaveConfigModel());
        }

        private WaveSpawnPlan CreateSpawnPlan()
        {
            int baseEnemiesInWave = this.GetEnemyCountForWave(this.CurrentWaveIndex);
            int adjustedEnemiesInWave = this.sceneAdapter.GetEnemyCountForWave(this.CurrentWaveIndex, baseEnemiesInWave);
            return this.spawnPlanService.CreatePlan(this.runtimeState, this.CreateWaveConfigModel(), adjustedEnemiesInWave);
        }

        /// <summary>
        /// 统计 EnemyManager 中实际存活的敌人数量（排除已销毁的 null 引用）
        /// EnemyManager.Characters 在被 PhotonNetwork.Destroy 后不会自动清理 null 条目，因此不能直接使用 Count()。
        /// </summary>
        private int CountAliveEnemies()
        {
            return this.sceneAdapter.CountAliveEnemies();
        }

        private int GetEffectiveMaxAliveEnemies()
        {
            int runtimeMaxEnemyCount = this.sceneAdapter.GetRuntimeMaxEnemyCount();
            return this.flowService.GetEffectiveMaxAliveEnemies(this.Config.maxAliveEnemies, runtimeMaxEnemyCount);
        }

        private void SyncPublicStateFromRuntime()
        {
            this.CurrentWaveIndex = this.runtimeState.CurrentWaveIndex;
            this.TotalWavesCompleted = this.runtimeState.TotalWavesCompleted;
            this.EnemiesAliveInWave = this.runtimeState.EnemiesAliveInWave;
            this.EnemiesDefeatedInWave = this.runtimeState.EnemiesDefeatedInWave;
            this.IsWaveActive = this.runtimeState.IsWaveActive;
            this.IsResting = this.runtimeState.IsResting;
        }

        /// <summary>
        /// 创建供纯规则服务使用的波次配置模型。
        /// </summary>
        private WaveConfigModel CreateWaveConfigModel()
        {
            return new WaveConfigModel
            {
                BaseEnemyCount = this.Config.baseEnemyCount,
                EnemiesPerWaveIncrease = this.Config.enemiesPerWaveIncrease,
                MaxAliveEnemies = this.Config.maxAliveEnemies,
                TotalWaves = this.Config.totalWaves,
                DifficultyScalePerWave = this.Config.difficultyScalePerWave,
            };
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            WaveManagerData data = new WaveManagerData
            {
                CurrentWaveIndex = this.CurrentWaveIndex,
                TotalWavesCompleted = this.TotalWavesCompleted,
                EnemiesAliveBeforeWave = this.runtimeState.EnemiesAliveBeforeWave,
                EnemiesSpawnedThisWave = this.runtimeState.EnemiesSpawnedThisWave,
                IsWaveActive = this.IsWaveActive,
                IsResting = this.IsResting,
            };
            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), data);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            WaveManagerData data = DataTool.LoadDataByBinary<WaveManagerData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            if (data == null)
            {
                return;
            }

            // 恢复运行时状态：如果是波次进行中，视为已完成该波次（防止读档后重复生成敌人）
            int totalWavesCompleted = data.TotalWavesCompleted;
            bool isWaveActive = data.IsWaveActive;
            if (isWaveActive)
            {
                totalWavesCompleted++;
                isWaveActive = false;
            }

            this.runtimeState.RestoreFrom(
                data.CurrentWaveIndex,
                totalWavesCompleted,
                data.EnemiesAliveBeforeWave,
                data.EnemiesSpawnedThisWave,
                isWaveActive,
                false); // 读档时不在休息状态
            this.SyncPublicStateFromRuntime();

            // 如果之前已经启动过波次系统，读档后自动恢复
            if (totalWavesCompleted > 0)
            {
                this.sceneAdapter.SetWaveControlEnabled(true);
                Core.GameServices.AsyncProgressCompleteProvider(() =>
                {
                    this.waveCoroutine = this.timeScheduler.Start(this.WaveLoop());
                });
            }
        }

        /// <summary>
        /// 波次管理存档数据
        /// </summary>
        [Serializable]
        public class WaveManagerData
        {
            public int CurrentWaveIndex;
            public int TotalWavesCompleted;
            public int EnemiesAliveBeforeWave;
            public int EnemiesSpawnedThisWave;
            public bool IsWaveActive;
            public bool IsResting;
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
