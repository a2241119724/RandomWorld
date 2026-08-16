namespace LAB2D.Character.Worker.Task
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Data;
    using LAB2D.Domain.Common;
    using LAB2D.Enum;
    using LAB2D.Item;
    using LAB2D.Map;
    using LAB2D.Serializable;
    using LAB2D.Tool;
    using LAB2D.UI.Character;
    using LAB2D.UI.Panel.PanelUI;
    using LAB2D.Domain.Worker;
    using System;
    using System.Collections.Generic;
    // WorkerTaskType 已提取到 LAB2D.Enum.WorkerTaskType（独立枚举）

    /// <summary>
    /// Worker任务
    /// </summary>
    [Serializable]
    public abstract class AWorkerTask : IWorkerTask, IWorkerTaskInfo
    {
        /// <summary>
        /// 悬赏任务覆盖 OwnerId — WorkerBountyTask 在执行前设置，
        /// 使得 innerTask（如 Gather）产出的资源归悬赏发布者而非执行者。
        /// 0 = 不覆盖 / Player（配合 IsBountyExecution 区分）。
        /// Unity InstanceID 可正可负，不能用 -1 判断。
        /// </summary>
        public static int BountyOwnerOverride = 0;

        /// <summary>
        /// 是否正在执行悬赏任务 — 与 BountyOwnerOverride 配合使用。
        /// BountyOwnerOverride=0 存在歧义：可能是"无悬赏"也可能是"Player 发布的悬赏(ownerId=0)"。
        /// IsBountyExecution=true 时，BountyOwnerOverride=0 表示 Player 悬赏。
        /// </summary>
        public static bool IsBountyExecution = false;

        /// <summary>
        /// Worker在工作时的位置（上下左右）
        /// </summary>
        public List<Vector3IntLAB> AvailableNeighborPos;

        /// <summary>
        /// 临近的位置
        /// </summary>
        protected static readonly List<Vector3IntLAB> Neighbors = new ()
        {
            new Vector3IntLAB(0, 1, 0), // 上
            new Vector3IntLAB(1, 0, 0), // 右
            new Vector3IntLAB(0, -1, 0), // 下
            new Vector3IntLAB(-1, 0, 0), // 左
            new Vector3IntLAB(1, 1, 0), // 右上
            new Vector3IntLAB(1, -1, 0), // 右下
            new Vector3IntLAB(-1, -1, 0), // 左下
            new Vector3IntLAB(-1, 1, 0), // 左上
            new Vector3IntLAB(0, 0, 0), // 自身
        };

        /// <summary>
        /// 任务需要的时间
        /// </summary>
        protected float maxProgress = WorkerTaskTimeConfig.DefaultTaskSeconds;
        private readonly WorkerTaskProgressService progressService = new WorkerTaskProgressService();

        /// <summary>
        /// 任务阶段
        /// </summary>
        protected int stage;

        /// <summary>
        /// 当前经过时间
        /// </summary>
        protected float curProgress = 0.0f;

        /// <summary>
        /// 任务阶段上下文
        /// </summary>
        [NonSerialized]
        protected List<Action<AWorker>> stageInit;

        public AWorkerTask(WorkerTaskType taskType)
        {
            this.TaskType = taskType;
            this.Name = taskType.ToString();
            this.AvailableNeighborPos = new List<Vector3IntLAB>();
            this.stageInit = new List<Action<AWorker>>();
            this.Init();
        }

        public enum RectType
        {
            /// <summary>
            /// 建造的Rect以鼠标为中心(房间)
            /// </summary>
            Center,

            /// <summary>
            /// 建造的Rect以鼠标为左下, Tile大于1格的(床)
            /// </summary>
            BottomLeft,

            /// <summary>
            /// 建造的Rect以鼠标为左上, 可自定义大小的建造(房间)
            /// </summary>
            TopLeft,
        }

        // WorkerTaskType 已提取到 LAB2D.Enum.WorkerTaskType。
        // 本文件通过 using WorkerTaskType = LAB2D.Enum.WorkerTaskType 保持向后兼容。

        /// <summary>
        /// 任务进度倍率提供者 — 组合天气效果和 Worker 状态对任务进度的倍率影响。
        /// 默认实现访问 ServiceLocator.Get&lt;WeatherGameplayEffect&gt;() 和 ServiceLocator.Get&lt;WorkerConditionManager&gt;()。
        /// 可替换为测试桩或自定义实现。
        /// </summary>
        public static System.Func<WorkerTaskType, AWorker, float> ProgressMultiplierProvider { get; set; }
            = (taskType, worker) =>
            {
                float multiplier = ServiceLocator.Get<WeatherGameplayEffect>().GetWorkerTaskProgressMultiplier(taskType);
                multiplier *= ServiceLocator.Get<WorkerConditionManager>().GetWorkerTaskProgressMultiplier(worker, taskType);
                return multiplier;
            };

        /// <summary>
        /// 地图可通过性查询 — 判断指定格子是否可到达。
        /// 默认实现访问 ServiceLocator.Get&lt;BuildMap&gt;()。
        /// 可替换为测试桩或自定义实现（如 IMapWalkabilityQuery 适配器）。
        /// </summary>
        public static System.Func<int, int, bool> WalkabilityProvider { get; set; }
            = (x, y) => ServiceLocator.Get<BuildMap>().IsCanReach(new UnityEngine.Vector3Int(x, y, 0));

        /// <summary>
        /// 任务生命周期回调 — 记录任务开始和完成的统计追踪。
        /// bool 参数：true = 任务开始, false = 任务完成。
        /// 默认实现访问 WorkerEfficiencyTracker。
        /// 可替换为测试桩或自定义实现。
        /// </summary>
        public static System.Action<AWorkerTask, AWorker, bool> TaskLifecycleProvider { get; set; }
            = (task, worker, isStart) =>
            {
                if (isStart)
                {
                    ServiceLocator.Get<WorkerEfficiencyTracker>().RecordTaskStarted(worker, task);
                }
                else
                {
                    ServiceLocator.Get<WorkerEfficiencyTracker>().RecordTaskCompleted(worker, task);
                }
            };

        /// <summary>
        /// 任务完成处理器 — 从任务队列中移除已完成任务。
        /// 默认实现访问 ServiceLocator.Get&lt;WorkerTaskManager&gt;().CompleteTask。
        /// 可替换为测试桩或自定义实现。
        /// </summary>
        public static System.Action<AWorkerTask> TaskCompletionProvider { get; set; }
            = (task) => ServiceLocator.Get<WorkerTaskManager>().CompleteTask(task);

        /// <summary>
        /// 库存管理器访问提供者 — 统一库存操作入口。
        /// 默认返回 ServiceLocator.Get&lt;InventoryManager&gt;()。
        /// 可替换为测试桩或自定义实现。
        /// </summary>
        public static System.Func<InventoryManager> InventoryProvider { get; set; }
            = () => ServiceLocator.Get<InventoryManager>();

        /// <summary>
        /// 物品数据提供者 — 根据物品 ID 获取配置数据。
        /// 默认返回 ServiceLocator.Get&lt;ItemDataManager&gt;().GetById(id)。
        /// 可替换为测试桩。
        /// </summary>
        public static System.Func<int, ItemData> ItemDataProvider { get; set; }
            = (id) => ServiceLocator.Get<ItemDataManager>().GetById(id);

        /// <summary>
        /// 物品地图提供者 — 物品在地图上的放置/拾取操作。
        /// 默认返回 ServiceLocator.Get&lt;ItemMap&gt;()。
        /// 可替换为测试桩。
        /// </summary>
        public static System.Func<ItemMap> ItemMapProvider { get; set; }
            = () => ServiceLocator.Get<ItemMap>();

        /// <summary>
        /// 日志提供者 — 任务相关的错误/警告日志输出。
        /// 默认实现访问 ServiceLocator.Get&lt;LogManager&gt;()。
        /// 可替换为测试桩（如静默日志）。
        /// </summary>
        public static System.Action<string, LogManager.LogLevelEnum> LogProvider { get; set; }
            = (message, level) => ServiceLocator.Get<LogManager>().Log(message, level);

        /// <summary>
        /// 节流版日志 — 同一 key 在 intervalSec 秒内最多输出一条。
        /// 用于高频状态切换/寻路日志（敌人漫游每秒数十次 → game.log 181 万行/61 分钟的刷屏源，
        /// 见 bug-fixes.md 2026-08-15）。在保留诊断能力的同时避免刷爆日志。
        /// key 建议用 "{角色名}|{事件类型}" 区分不同角色与事件；打点处独立维护 key 即可。
        /// 注意 elapsed&lt;0（Time.time 重置，如关闭域重载的新 play 会话）会放行并更新时间戳，
        /// 否则残留的旧时间戳会把该 key 的日志整体抑制到 time 追平为止。
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<string, float> s_logThrottleLastTime
            = new System.Collections.Generic.Dictionary<string, float>();
        private static readonly object s_logThrottleLock = new object();

        public static void LogProviderThrottled(
            string key, float intervalSec, string message, LogManager.LogLevelEnum level)
        {
            float now = UnityEngine.Time.time;
            lock (s_logThrottleLock)
            {
                if (s_logThrottleLastTime.TryGetValue(key, out float last))
                {
                    float elapsed = now - last;
                    if (elapsed >= 0f && elapsed < intervalSec)
                    {
                        return;
                    }
                }

                s_logThrottleLastTime[key] = now;
            }

            LogProvider(message, level);
        }

        /// <summary>
        /// 物品类型查找提供者 — 根据物品 ID 返回物品类型枚举。
        /// 默认实现访问 ServiceLocator.Get&lt;ItemDataManager&gt;().IdToType。
        /// 可替换为测试桩。
        /// </summary>
        public static System.Func<int, AItem.ItemTypeEnum> ItemTypeProvider { get; set; }
            = (id) => ServiceLocator.Get<ItemDataManager>().IdToType(id);

        /// <summary>
        /// 物品实例工厂提供者 — 根据名称创建物品实例。
        /// 默认实现访问 ServiceLocator.Get&lt;ItemInstanceFactory&gt;().GetBackpackItemByName。
        /// 可替换为测试桩。
        /// </summary>
        public static System.Func<string, ABackpackItem> ItemFactoryProvider { get; set; }
            = (name) => ServiceLocator.Get<ItemInstanceFactory>().GetBackpackItemByName(name);

        public static System.Func<EquipmentBeamManager> EquipmentBeamProvider { get; set; }
            = () => ServiceLocator.Get<EquipmentBeamManager>();
        public static System.Func<EnemyLootManager> EnemyLootProvider { get; set; }
            = () => ServiceLocator.Get<EnemyLootManager>();
        public static System.Func<string, object> ResourceLoadProvider { get; set; }
            = (name) => ServiceLocator.Get<ResourceManager>().GetAsset(name);
        public static System.Func<UnityEngine.Vector3Int, UnityEngine.Vector3> TileMapPositionProvider { get; set; }
            = (pos) => ServiceLocator.Get<TileMap>().MapPosToWorldPos(pos);
        public static System.Action<AWorkerTask, GameGridPosition, int> TaskAddProvider { get; set; }
            = (task, pos, stage) => ServiceLocator.Get<WorkerTaskManager>().AddTask(task, pos, stage);
        public static System.Action<UnityEngine.Vector3Int> DeleteHungryTaskProvider { get; set; }
            = (pos) => ServiceLocator.Get<WorkerTaskManager>().DeleteHungryTask(
                LAB2D.UnityAdapter.UnityVectorAdapter.ToGameGridPosition(pos));
        public static System.Func<ResourceMap> ResourceMapProvider { get; set; }
            = () => ServiceLocator.Get<ResourceMap>();
        public static System.Func<TileMap> TileMapProvider { get; set; }
            = () => ServiceLocator.Get<TileMap>();
        public static System.Func<int, System.Collections.Generic.List<DropItem>> DropDataProvider { get; set; }
            = (id) => ServiceLocator.Get<DropDataManager>().GetDropItemsById(id);
        public static System.Func<UnityEngine.Vector3Int, int, bool, UnityEngine.Vector3Int> AvailablePositionProvider { get; set; }
            = (pos, radius, center) => ServiceLocator.Get<IsAvailableMap>().GenAvailablePosMap(pos, radius, center);
        public static System.Func<GatherMap> GatherMapProvider { get; set; }
            = () => ServiceLocator.Get<GatherMap>();
        public static System.Func<FarmlandManager> FarmlandManagerProvider { get; set; }
            = () => ServiceLocator.Get<FarmlandManager>();
        public static System.Action<Vector3IntLAB> BuildMapCompletionProvider { get; set; }
            = (pos) => ServiceLocator.Get<BuildMap>().SetComplete(pos);
        public static System.Func<UnityEngine.Vector3, UnityEngine.Vector3Int> TileMapWorldToMapProvider { get; set; }
            = (pos) => ServiceLocator.Get<TileMap>().WorldPosToMapPos(pos);
        public static System.Func<UnityEngine.Vector3Int, UnityEngine.Vector3Int> GenCanReachPosProvider { get; set; }
            = (pos) => ServiceLocator.Get<TileMap>().GenCanReachPos(pos);
        // --- 以下 Provider 已迁移至 GameServices，保留 [Obsolete] 代理确保向后兼容 ---

        [System.Obsolete("Use GameServices.ResourceInstantiateProvider instead.")]
        public static System.Func<string, bool, UnityEngine.GameObject> ResourceInstantiateProvider
        {
            get => Core.GameServices.ResourceInstantiateProvider;
            set => Core.GameServices.ResourceInstantiateProvider = value;
        }

        [System.Obsolete("Use GameServices.FurnitureBedProvider instead.")]
        public static System.Action<AWorker> FurnitureBedProvider
        {
            get => Core.GameServices.FurnitureBedProvider;
            set => Core.GameServices.FurnitureBedProvider = value;
        }

        [System.Obsolete("Use GameServices.AttackEffectProvider instead.")]
        public static System.Func<AttackEffectManager.EffectTypeEnum, float, UnityEngine.ParticleSystem> AttackEffectProvider
        {
            get => Core.GameServices.AttackEffectProvider;
            set => Core.GameServices.AttackEffectProvider = value;
        }

        [System.Obsolete("Use GameServices.EnemyRemoveProvider instead.")]
        public static System.Action<AEnemy> EnemyRemoveProvider
        {
            get => Core.GameServices.EnemyRemoveProvider;
            set => Core.GameServices.EnemyRemoveProvider = value;
        }

        [System.Obsolete("Use GameServices.EnemyCanCreateProvider instead.")]
        public static System.Func<bool> EnemyCanCreateProvider
        {
            get => Core.GameServices.EnemyCanCreateProvider;
            set => Core.GameServices.EnemyCanCreateProvider = value;
        }

        [System.Obsolete("Use GameServices.PlayerCountProvider instead.")]
        public static System.Func<int> PlayerCountProvider
        {
            get => Core.GameServices.PlayerCountProvider;
            set => Core.GameServices.PlayerCountProvider = value;
        }

        [System.Obsolete("Use GameServices.PlayerGetProvider instead.")]
        public static System.Func<int, Character> PlayerGetProvider
        {
            get => Core.GameServices.PlayerGetProvider;
            set => Core.GameServices.PlayerGetProvider = value;
        }

        [System.Obsolete("Use GameServices.WorkerCountProvider instead.")]
        public static System.Func<int> WorkerCountProvider
        {
            get => Core.GameServices.WorkerCountProvider;
            set => Core.GameServices.WorkerCountProvider = value;
        }

        [System.Obsolete("Use GameServices.WorkerGetProvider instead.")]
        public static System.Func<int, Character> WorkerGetProvider
        {
            get => Core.GameServices.WorkerGetProvider;
            set => Core.GameServices.WorkerGetProvider = value;
        }

        [System.Obsolete("Use GameServices.EnemyDefeatedProvider instead.")]
        public static System.Action<AEnemy, Character, int> EnemyDefeatedProvider
        {
            get => Core.GameServices.EnemyDefeatedProvider;
            set => Core.GameServices.EnemyDefeatedProvider = value;
        }

        [System.Obsolete("Use GameServices.WaveIndexProvider instead.")]
        public static System.Func<int> WaveIndexProvider
        {
            get => Core.GameServices.WaveIndexProvider;
            set => Core.GameServices.WaveIndexProvider = value;
        }

        [System.Obsolete("Use GameServices.FloatingTextProvider instead.")]
        public static System.Action<UnityEngine.Vector3, float, bool, bool> FloatingTextProvider
        {
            get => Core.GameServices.FloatingTextProvider;
            set => Core.GameServices.FloatingTextProvider = value;
        }

        [System.Obsolete("Use GameServices.NetworkIsOnlineProvider instead.")]
        public static System.Func<bool> NetworkIsOnlineProvider
        {
            get => Core.GameServices.NetworkIsOnlineProvider;
            set => Core.GameServices.NetworkIsOnlineProvider = value;
        }

        [System.Obsolete("Use GameServices.NetworkIsMasterClientProvider instead.")]
        public static System.Func<bool> NetworkIsMasterClientProvider
        {
            get => Core.GameServices.NetworkIsMasterClientProvider;
            set => Core.GameServices.NetworkIsMasterClientProvider = value;
        }

        [System.Obsolete("Use GameServices.NetworkDestroyProvider instead.")]
        public static System.Action<UnityEngine.GameObject> NetworkDestroyProvider
        {
            get => Core.GameServices.NetworkDestroyProvider;
            set => Core.GameServices.NetworkDestroyProvider = value;
        }

        public static System.Func<int, ABackpackItem> ItemFactoryByIdProvider { get; set; }
            = (id) => ServiceLocator.Get<ItemInstanceFactory>().GetBackpackItemById(id);

        [System.Obsolete("Use GameServices.NameGeneratorProvider instead.")]
        public static System.Func<string> NameGeneratorProvider
        {
            get => Core.GameServices.NameGeneratorProvider;
            set => Core.GameServices.NameGeneratorProvider = value;
        }

        [System.Obsolete("Use GameServices.AsyncProgressSetTipProvider instead.")]
        public static System.Action<string> AsyncProgressSetTipProvider
        {
            get => Core.GameServices.AsyncProgressSetTipProvider;
            set => Core.GameServices.AsyncProgressSetTipProvider = value;
        }

        [System.Obsolete("Use GameServices.AsyncProgressCompleteProvider instead.")]
        public static System.Action<System.Action> AsyncProgressCompleteProvider
        {
            get => Core.GameServices.AsyncProgressCompleteProvider;
            set => Core.GameServices.AsyncProgressCompleteProvider = value;
        }

        [System.Obsolete("Use GameServices.LocateWorkerUIAddProvider instead.")]
        public static System.Action<AWorker> LocateWorkerUIAddProvider
        {
            get => Core.GameServices.LocateWorkerUIAddProvider;
            set => Core.GameServices.LocateWorkerUIAddProvider = value;
        }

        [System.Obsolete("Use GameServices.LocateWorkerUIRemoveProvider instead.")]
        public static System.Action<AWorker> LocateWorkerUIRemoveProvider
        {
            get => Core.GameServices.LocateWorkerUIRemoveProvider;
            set => Core.GameServices.LocateWorkerUIRemoveProvider = value;
        }

        [System.Obsolete("Use GameServices.ShowTipProvider instead.")]
        public static System.Action<string> ShowTipProvider
        {
            get => Core.GameServices.ShowTipProvider;
            set => Core.GameServices.ShowTipProvider = value;
        }

        [System.Obsolete("Use GameServices.AsyncProgressAddOneProvider instead.")]
        public static System.Action AsyncProgressAddOneProvider
        {
            get => Core.GameServices.AsyncProgressAddOneProvider;
            set => Core.GameServices.AsyncProgressAddOneProvider = value;
        }

        [System.Obsolete("Use GameServices.AsyncProgressAddTotalProvider instead.")]
        public static System.Action<int> AsyncProgressAddTotalProvider
        {
            get => Core.GameServices.AsyncProgressAddTotalProvider;
            set => Core.GameServices.AsyncProgressAddTotalProvider = value;
        }

        /// <summary>
        /// 任务ID
        /// </summary>
        public long TaskId { get; set; }

        /// <summary>
        /// 任务最后一次因目标不可达而失败的时间戳（0=从未失败）。
        /// 设置后任务进入冷却期，冷却期内不会被分配；冷却结束后可被其他 Worker 重试。
        /// </summary>
        public float LastFailedTime { get; set; }

        /// <summary>
        /// 任务失败后冷却时长（秒），冷却期内任务不会被分配。
        /// </summary>
        public const float FailedCooldownSeconds = 10f;

        /// <summary>
        /// 是否处于冷却期（冷却期内任务不可被任何 Worker 接取）。
        /// </summary>
        public bool IsInCooldown => this.LastFailedTime > 0
            && UnityEngine.Time.time - this.LastFailedTime < FailedCooldownSeconds;

        /// <summary>
        /// 目标位置, 仅用于阶段性目标
        /// </summary>
        public virtual Vector3IntLAB TargetMap { get; protected set; }

        /// <summary>
        /// 任务类型
        /// </summary>
        public WorkerTaskType TaskType { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 执行任务时是否消耗疲劳值。Eat/Sleep 任务重写为 false。
        /// </summary>
        protected virtual bool ConsumesTiredness => true;

        /// <summary>
        /// 此任务的疲劳消耗速率（/秒）。子类可重写以实现差异化消耗。
        /// 默认返回 WorkTiredCostPerSecond。
        /// </summary>
        protected virtual float TiredCostPerSecond => WorkerTaskTimeConfig.WorkTiredCostPerSecond;

        /// <summary>
        /// Worker 饥饿时是否阻止接此任务。Eat 任务重写为 false（饥饿时才需要吃饭）。
        /// </summary>
        protected virtual bool BlocksWhenHungry => true;

        /// <summary>
        /// 是否需要检查临近位置是否可行走。Exercise 任务重写为 false（原地锻炼无需外部位置）。
        /// </summary>
        protected virtual bool RequiresWalkableNeighbor => true;

        /// <summary>
        /// 任务特征标志，用于 WorkerTaskManager 的通用分派逻辑。
        /// 新增任务类型时只需重写此属性，无需修改 Manager。
        /// </summary>
        public virtual TaskTraits Traits => TaskTraits.None;

        /// <summary>
        /// 任务专属 Worker 的 instance ID。默认 0 表示不专属任何 Worker。
        /// WorkerSpecific 任务（Wear/Sleep/Exercise）、Bounty 发布者、
        /// PickUp 目标所有者等应重写此属性。
        /// Worker 死亡时，WorkerTaskManager 会清除所有 OwnerWorkerId 匹配的任务。
        /// </summary>
        public virtual int OwnerWorkerId => 0;

        /// <summary>
        /// 执行任务。子类可通过 override 自定义执行逻辑（如 WorkerBountyTask 委托给内部任务）。
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <returns>是否成功</returns>
        public virtual bool Execute(AWorker worker, float deltaTime)
        {
            // 工作扣减疲劳值
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;

            // 子类可通过 ConsumesTiredness 虚属性控制是否消耗疲劳
            // 通过 TiredCostPerSecond 虚属性支持差异化消耗
            if (this.ConsumesTiredness)
            {
                workerData.CurTired = this.progressService.ApplyTiredCost(
                    workerData.CurTired,
                    deltaTime,
                    this.TiredCostPerSecond);
            }

            float progressMultiplier = ProgressMultiplierProvider(this.TaskType, worker);
            WorkerTaskProgressResult progressResult = this.progressService.AdvanceProgress(
                this.curProgress,
                this.maxProgress,
                deltaTime,
                progressMultiplier);
            this.curProgress = progressResult.CurrentProgress;
            if (progressResult.Completed)
            {
                worker.SetProgress(this.curProgress, false);
                if (this.StageChangeRule(worker))
                {
                    this.Finish(worker);
                    return true;
                }

                return false;
            }

            worker.SetProgress(this.progressService.GetProgressRatio(this.curProgress, this.maxProgress), true);
            return false;
        }

        /// <summary>
        /// 选择到最近的任务之后执行
        /// </summary>
        /// <param name="worker">Worker</param>
        public virtual void Start(AWorker worker)
        {
            this.curProgress = 0.0f;
            TaskLifecycleProvider(this, worker, true);

            // [TaskDiag] 记录任务开始（通用事件点，覆盖直接指派与队列分配的启动）
            LogProvider(
                $"[TaskDiag] {worker.name} 开始任务 type={this.TaskType} target=({this.TargetMap.X},{this.TargetMap.Y})",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// Worker是否可以接该任务
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <returns>是否</returns>
        public bool IsCanWork(AWorker worker)
        {
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;

            // 所有任务类型默认为开启（opt-out 语义）：只有字典中显式记录 false 时才阻止。
            // WorkerData 构造函数中已将所有 WorkerTaskType 初始化为 true。
            if (workerData.TaskToggle.TryGetValue(this.TaskType, out bool enabled) && !enabled)
            {
                return false;
            }

            // 饥饿时候不能接任务（Eat 任务通过 BlocksWhenHungry 虚属性豁免）
            if (workerData.CurHungry < AWorker.ThresholdHungry && this.BlocksWhenHungry)
            {
                return false;
            }

            // 是否有做任务的位置（Exercise 通过 RequiresWalkableNeighbor 虚属性豁免）
            if (this.RequiresWalkableNeighbor && this.AvailableNeighborPos.TrueForAll(pos =>
            {
                Vector3IntLAB target = pos + this.TargetMap;
                return !WalkabilityProvider(target.X, target.Y);
            }))
            {
                return false;
            }

            return this.DoIsCanWork(worker);
        }

        /// <summary>
        /// 放弃任务
        /// </summary>
        /// <param name="worker">Worker</param>
        public virtual void GiveUpTask(AWorker worker)
        {
            // [TaskDiag] 任务侧放弃通知（含任务类型与目标），终端执行见 AWorker.GiveUpTask
            LogProvider(
                $"[TaskDiag] {worker.name} 放弃任务 type={this.TaskType} target=({this.TargetMap.X},{this.TargetMap.Y})",
                LogManager.LogLevelEnum.Warning);
            worker.GiveUpTask();
        }

        /// <inheritdoc/>
        public virtual void Finish(AWorker worker)
        {
            // [TaskDiag] 记录任务完成（通用事件点）
            LogProvider(
                $"[TaskDiag] {worker.name} 完成任务 type={this.TaskType} target=({this.TargetMap.X},{this.TargetMap.Y})",
                LogManager.LogLevelEnum.Debug);

            TaskCompletionProvider(this);
            TaskLifecycleProvider(this, worker, false);
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            workerData.Task = null;

            // 完成任务 → 人格变化：心情↑ 勤奋↑ 事业心微↑
            if (workerData != null)
            {
                workerData.Personality = workerData.Personality.AfterTaskComplete();
                // 任务完成后强制下次 Seek 立即决策，避免无意义漫游
                workerData.ForceDecisionOnNextSeek = true;
            }
        }

        /// <summary>
        /// Worker是否可以接该任务,具体实现
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <returns>是否</returns>
        protected abstract bool DoIsCanWork(AWorker worker);

        /// <summary>
        /// 初始化可用位置, 用于判断是否接受任务
        /// </summary>
        protected abstract void Init();

        /// <summary>
        /// 是否真的完成，为多阶段任务服务（Carry）
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <returns>是否</returns>
        protected virtual bool StageChangeRule(AWorker worker)
        {
            return true;
        }

        /// <summary>
        /// 任务进入不同阶段,切换上下文
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="stage">任务所处阶段</param>
        protected void ChangeStage(AWorker worker, int stage)
        {
            if (this.stageInit.Count < stage + 1)
            {
                LogProvider("没有该阶段", LogManager.LogLevelEnum.Error);
                return;
            }

            this.stage = stage;
            this.curProgress = 0.0f;
            this.stageInit[stage].Invoke(worker);
            worker.Manager.ChangeState(AWorkerState.TypeEnum.Seek);
        }

        /// <summary>
        /// 放置掉落物到地图。可堆叠物品优先合并到周围同类堆叠，不可堆叠或找不到则找空地放置。
        /// 合并和放置都使用环形辐射扫描，从中心一圈一圈向外找，保证找到最近的位置。
        /// </summary>
        /// <param name="center">搜索中心</param>
        /// <param name="resourceInfo">掉落物信息</param>
        /// <param name="tileName">瓦片名称（用于创建新掉落物图标）</param>
        /// <returns>实际放置位置，default 表示无处可放</returns>
        public static UnityEngine.Vector3Int TryMergeOrPlaceDrop(
            UnityEngine.Vector3Int center, ResourceInfo resourceInfo, string tileName)
        {
            const int mergeRadius = 5;
            const int placeMaxRadius = 20;
            ItemData itemData = ItemDataProvider(resourceInfo.Id);

            LogProvider(
                $"[掉落入口] center=({center.x},{center.y}) id={resourceInfo.Id} count={resourceInfo.Count} stackable={itemData?.IsStackable}",
                LogManager.LogLevelEnum.Trace);

            // 可堆叠物品：附近 5 格内找同类合并（仅限同 Owner）
            if (itemData != null && itemData.IsStackable)
            {
                DropManager dropManager = ServiceLocator.Get<DropManager>();
                UnityEngine.Vector3Int mergePos = FindNearbyDrop(center, mergeRadius, resourceInfo.Id, resourceInfo.OwnerId, dropManager);
                if (mergePos != default)
                {
                    AItem.ItemTypeEnum itemType = ItemTypeProvider(resourceInfo.Id);
                    dropManager.AddDrop(itemType, mergePos, resourceInfo);
                    return mergePos;
                }
            }

            // 找不到合并或不可堆叠：环形辐射找最近的空地放置
            UnityEngine.Vector3Int pos = FindNearestFreeTile(center, placeMaxRadius);

            if (pos != default)
            {
                UnityEngine.Tilemaps.TileBase tile = (UnityEngine.Tilemaps.TileBase)ResourceLoadProvider(tileName);
                ItemMapProvider().PutDownToDrop(pos, tile, resourceInfo);
            }

            return pos;
        }

        /// <summary>
        /// 环形辐射扫描，查找周围已有的同ID+同Owner掉落物位置，优先最近。
        /// 不同 Owner 的资源不合并，避免悬赏产物归错人。
        /// </summary>
        private static UnityEngine.Vector3Int FindNearbyDrop(
            UnityEngine.Vector3Int center, int maxRadius, int itemId, int ownerId, DropManager dropManager)
        {
            FindClosest(center, maxRadius, pos =>
            {
                ResourceInfo existing = dropManager.GetDropByAll(pos);
                return existing != null && existing.Id == itemId && existing.OwnerId == ownerId;
            }, out UnityEngine.Vector3Int found);

            return found;
        }

        /// <summary>
        /// 环形辐射扫描，查找最近的可用空地。直接复用 IsAvailableMap 的检查逻辑。
        /// </summary>
        private static UnityEngine.Vector3Int FindNearestFreeTile(UnityEngine.Vector3Int center, int maxRadius)
        {
            IsAvailableMap availableMap = ServiceLocator.Get<IsAvailableMap>();
            int failCount = 0;

            for (int r = 0; r <= maxRadius; r++)
            {
                bool foundInRing = FindClosest(center, r, pos =>
                {
                    bool free = availableMap.IsTileFreeForDrop(pos);
                    // 每次放置只输出第一条失败诊断，避免逐格刷屏（日志观测 [掉落诊断] 3k+ 条，含 5 次 ServiceLocator 查询）
                    if (!free && failCount < 1)
                    {
                        failCount++;
                        TileMap tm = ServiceLocator.Get<TileMap>();
                        bool terrainOk = true;
                        if (tm.TileMapDataLAB?.MapTiles != null &&
                            pos.x >= 0 && pos.x < tm.TileMapDataLAB.Height &&
                            pos.y >= 0 && pos.y < tm.TileMapDataLAB.Width)
                        {
                            int tid = tm.TileMapDataLAB.MapTiles[pos.x, pos.y];
                            terrainOk = ServiceLocator.Get<TerrainConfigDatabase>().CanBuild(tid);
                        }
                        LogProvider(
                            $"[掉落诊断] r={r} ({pos.x},{pos.y}) " +
                            $"Reach={ServiceLocator.Get<TileMap>().IsCanReach(pos)} " +
                            $"Bld={ServiceLocator.Get<BuildMap>().IsFreeTile(pos)} " +
                            $"Res={ServiceLocator.Get<ResourceMap>().IsFreeTile(pos)} " +
                            $"Item={ItemMapProvider().IsFreeTile(pos)} " +
                            $"Terrain={terrainOk}",
                            LogManager.LogLevelEnum.Trace);
                    }
                    return free;
                }, out UnityEngine.Vector3Int found);

                if (foundInRing)
                {
                    return found;
                }
            }

            return default;
        }

        /// <summary>
        /// 按距离从小到大遍历周围格子（环形辐射），找到第一个满足 check 的位置。
        /// </summary>
        private static bool FindClosest(
            UnityEngine.Vector3Int center, int maxRadius,
            System.Func<UnityEngine.Vector3Int, bool> check,
            out UnityEngine.Vector3Int result)
        {
            // 按 max(|dx|,|dy|) 分层，逐层遍历环边
            for (int r = 0; r <= maxRadius; r++)
            {
                // 上下水平边: y = ±r, x ∈ [-r, r]
                for (int dx = -r; dx <= r; dx++)
                {
                    if (TryCheck(center.x + dx, center.y - r, check, out result)) return true;
                    if (r > 0 && TryCheck(center.x + dx, center.y + r, check, out result)) return true;
                }

                // 左右垂直边（不含角，角已在水平边处理）: x = ±r, y ∈ [-(r-1), r-1]
                for (int dy = -r + 1; dy <= r - 1; dy++)
                {
                    if (TryCheck(center.x - r, center.y + dy, check, out result)) return true;
                    if (TryCheck(center.x + r, center.y + dy, check, out result)) return true;
                }
            }

            result = default;
            return false;
        }

        private static bool TryCheck(int x, int y,
            System.Func<UnityEngine.Vector3Int, bool> check,
            out UnityEngine.Vector3Int result)
        {
            UnityEngine.Vector3Int pos = new UnityEngine.Vector3Int(x, y, 0);
            if (check(pos))
            {
                result = pos;
                return true;
            }

            result = default;
            return false;
        }
    }
}
