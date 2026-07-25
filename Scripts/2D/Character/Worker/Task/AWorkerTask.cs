namespace LAB2D.Character.Worker.Task
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Data;
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
        /// 默认实现访问 WeatherGameplayEffect.Instance 和 WorkerConditionManager.Instance。
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
        /// 默认实现访问 BuildMap.Instance。
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
        /// 默认实现访问 WorkerTaskManager.Instance.CompleteTask。
        /// 可替换为测试桩或自定义实现。
        /// </summary>
        public static System.Action<AWorkerTask> TaskCompletionProvider { get; set; }
            = (task) => ServiceLocator.Get<WorkerTaskManager>().CompleteTask(task);

        /// <summary>
        /// 库存管理器访问提供者 — 统一库存操作入口。
        /// 默认返回 InventoryManager.Instance。
        /// 可替换为测试桩或自定义实现。
        /// </summary>
        public static System.Func<InventoryManager> InventoryProvider { get; set; }
            = () => ServiceLocator.Get<InventoryManager>();

        /// <summary>
        /// 物品数据提供者 — 根据物品 ID 获取配置数据。
        /// 默认返回 ItemDataManager.Instance.GetById(id)。
        /// 可替换为测试桩。
        /// </summary>
        public static System.Func<int, ItemData> ItemDataProvider { get; set; }
            = (id) => ServiceLocator.Get<ItemDataManager>().GetById(id);

        /// <summary>
        /// 物品地图提供者 — 物品在地图上的放置/拾取操作。
        /// 默认返回 ItemMap.Instance。
        /// 可替换为测试桩。
        /// </summary>
        public static System.Func<ItemMap> ItemMapProvider { get; set; }
            = () => ServiceLocator.Get<ItemMap>();

        /// <summary>
        /// 日志提供者 — 任务相关的错误/警告日志输出。
        /// 默认实现访问 LogManager.Instance。
        /// 可替换为测试桩（如静默日志）。
        /// </summary>
        public static System.Action<string, LogManager.LogLevelEnum> LogProvider { get; set; }
            = (message, level) => ServiceLocator.Get<LogManager>().Log(message, level);

        /// <summary>
        /// 物品类型查找提供者 — 根据物品 ID 返回物品类型枚举。
        /// 默认实现访问 ItemDataManager.Instance.IdToType。
        /// 可替换为测试桩。
        /// </summary>
        public static System.Func<int, AItem.ItemTypeEnum> ItemTypeProvider { get; set; }
            = (id) => ServiceLocator.Get<ItemDataManager>().IdToType(id);

        /// <summary>
        /// 物品实例工厂提供者 — 根据名称创建物品实例。
        /// 默认实现访问 ItemInstanceFactory.Instance.GetBackpackItemByName。
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
        public static System.Action<AWorkerTask, Vector3IntLAB, int> TaskAddProvider { get; set; }
            = (task, pos, stage) => ServiceLocator.Get<WorkerTaskManager>().AddTask(task, pos, stage);
        public static System.Action<UnityEngine.Vector3Int> DeleteHungryTaskProvider { get; set; }
            = (pos) => ServiceLocator.Get<WorkerTaskManager>().DeleteHungryTask(
                LAB2D.UnityAdapter.UnityVectorAdapter.ToGameGridPosition(pos));
        public static System.Func<ResourceMap> ResourceMapProvider { get; set; }
            = () => ServiceLocator.Get<ResourceMap>();
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
        public static System.Func<string, bool, UnityEngine.GameObject> ResourceInstantiateProvider { get; set; }
            = (name, active) => ServiceLocator.Get<ResourceManager>().Instantiate(name, active);
        public static System.Action<AWorker> FurnitureBedProvider { get; set; }
            = (worker) => ServiceLocator.Get<FurnitureManager>().RemoveWorkerFromBed(worker);
        public static System.Func<AttackEffectManager.EffectTypeEnum, float, UnityEngine.ParticleSystem> AttackEffectProvider { get; set; }
            = (type, rad) => AttackEffectManager.Instance.GetEffect(type, rad);
        public static System.Action<AEnemy> EnemyRemoveProvider { get; set; }
            = (enemy) => ServiceLocator.Get<EnemyManager>().Remove(enemy);
        public static System.Func<bool> EnemyCanCreateProvider { get; set; }
            = () => ServiceLocator.Get<EnemyManager>().CanCreateEnemy();
        public static System.Func<int> PlayerCountProvider { get; set; }
            = () => ServiceLocator.Get<PlayerManager>().Count();
        public static System.Func<int, Character> PlayerGetProvider { get; set; }
            = (i) => ServiceLocator.Get<PlayerManager>().Get(i);
        public static System.Func<int> WorkerCountProvider { get; set; }
            = () => ServiceLocator.Get<WorkerManager>().Count();
        public static System.Func<int, Character> WorkerGetProvider { get; set; }
            = (i) => ServiceLocator.Get<WorkerManager>().Get(i);
        public static System.Action<AEnemy, Character, int> EnemyDefeatedProvider { get; set; }
            = (enemy, attacker, xp) => ServiceLocator.Get<GameplaySessionStats>().RecordEnemyDefeated(enemy, attacker, xp);
        public static System.Func<int> WaveIndexProvider { get; set; }
            = () => ServiceLocator.Get<WaveManager>() != null ? ServiceLocator.Get<WaveManager>().CurrentWaveIndex - 1 : 0;
        public static System.Action<UnityEngine.Vector3, float, bool, bool> FloatingTextProvider { get; set; }
            = (pos, dmg, crit, combo) => ServiceLocator.Get<FloatingTextManager>().SpawnDamageText(pos, dmg, crit, combo);
        public static System.Func<bool> NetworkIsOnlineProvider { get; set; }
            = () => ServiceLocator.Get<NetworkConnect>() != null && ServiceLocator.Get<NetworkConnect>().IsOnline;
        public static System.Func<bool> NetworkIsMasterClientProvider { get; set; }
            = () => Photon.Pun.PhotonNetwork.IsMasterClient;
        public static System.Action<UnityEngine.GameObject> NetworkDestroyProvider { get; set; }
            = (go) => Photon.Pun.PhotonNetwork.Destroy(go);
        public static System.Func<int, ABackpackItem> ItemFactoryByIdProvider { get; set; }
            = (id) => ServiceLocator.Get<ItemInstanceFactory>().GetBackpackItemById(id);
        public static System.Func<string> NameGeneratorProvider { get; set; }
            = () => ServiceLocator.Get<NameGenerator>().GetRandomName();
        public static System.Action<string> AsyncProgressSetTipProvider { get; set; }
            = (tip) => AsyncProgressUI.Instance.SetTip(tip);
        public static System.Action<System.Action> AsyncProgressCompleteProvider { get; set; }
            = (callback) => AsyncProgressUI.Instance.Complete += new AsyncProgressUI.CompleteDelegate(callback);
        public static System.Action<AWorker> LocateWorkerUIAddProvider { get; set; }
            = (worker) => LocateWorkerUI.Instance.AddWorkerItem(worker);
        public static System.Action<AWorker> LocateWorkerUIRemoveProvider { get; set; }
            = (worker) => LocateWorkerUI.Instance.RemoveWorkerItem(worker);
        public static System.Action<string> ShowTipProvider { get; set; }
            = (message) => { if (GlobalInit.Instance != null) GlobalInit.Instance.ShowTip(message); };
        public static System.Action AsyncProgressAddOneProvider { get; set; }
            = () => AsyncProgressUI.Instance.AddOneProcess();
        public static System.Action<int> AsyncProgressAddTotalProvider { get; set; }
            = (total) => AsyncProgressUI.Instance.AddTotal(total);

        /// <summary>
        /// 任务ID
        /// </summary>
        public long TaskId { get; set; }

        /// <summary>
        /// 目标位置, 仅用于阶段性目标
        /// </summary>
        public Vector3IntLAB TargetMap { get; protected set; }

        /// <summary>
        /// 任务类型
        /// </summary>
        public WorkerTaskType TaskType { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 不需要重写
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <returns>是否成功</returns>
        public bool Execute(AWorker worker, float deltaTime)
        {
            // 工作扣减疲劳值
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;

            // 吃饭和睡觉任务不消耗疲劳
            if (this.TaskType != WorkerTaskType.Eat && this.TaskType != WorkerTaskType.Sleep)
            {
                workerData.CurTired = this.progressService.ApplyTiredCost(
                    workerData.CurTired,
                    deltaTime,
                    WorkerTaskTimeConfig.WorkTiredCostPerSecond);
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
        }

        /// <summary>
        /// Worker是否可以接该任务
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <returns>是否</returns>
        public bool IsCanWork(AWorker worker)
        {
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            if (!workerData.TaskToggle[(int)this.TaskType])
            {
                return false;
            }

            // 饥饿时候不能接任务
            if (workerData.CurHungry < AWorker.ThresholdHungry && this.TaskType != WorkerTaskType.Eat)
            {
                return false;
            }

            // 是否有做任务的位置, 并且不是锻炼任务(由于目标位置不确定, 并且一定可以有位置做)
            if (this.TaskType != WorkerTaskType.Exercise && this.AvailableNeighborPos.TrueForAll(pos =>
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
            LogProvider("放弃任务", LogManager.LogLevelEnum.Warning);
            worker.GiveUpTask();
        }

        /// <inheritdoc/>
        public virtual void Finish(AWorker worker)
        {
            TaskCompletionProvider(this);
            TaskLifecycleProvider(this, worker, false);
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            workerData.Task = null;
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
            this.stageInit[stage].Invoke(worker);
            worker.Manager.ChangeState(AWorkerState.TypeEnum.Seek);
        }
    }
}
