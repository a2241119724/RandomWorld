namespace LAB2D.Character.Worker
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Character.Worker.State;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core.Seek;
    using LAB2D.Item;
    using LAB2D.Item.Build.Furniture.Bed;
    using LAB2D.Serializable;
    using LAB2D.UI.Character;
    using System;
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Worker
    /// </summary>
    public abstract class AWorker : Character
    {
        /// <summary>
        /// 饥饿值阈值
        /// </summary>
        public static readonly float ThresholdHungry = 20.0f;

        /// <summary>
        /// 疲劳值阈值
        /// </summary>
        public static readonly float ThresholdTired = 20.0f;

        /// <summary>
        /// Worker 放弃任务回调 — 清除资源预留并通知任务管理器。
        /// 默认实现访问 InventoryManager.Instance 和 WorkerTaskManager.Instance。
        /// </summary>
        public static System.Action<AWorker, AWorkerTask> GiveUpTaskProvider { get; set; }
            = (worker, task) =>
            {
                Core.ServiceLocator.Get<InventoryManager>().DeleteWorkerPre(worker);
                Core.ServiceLocator.Get<WorkerTaskManager>().GiveUpTask(task);
            };

        /// <summary>
        /// Worker 死亡回调 — 从管理器移除并记录统计数据。
        /// 默认实现访问 NetworkConnect.Instance / WorkerManager.Instance / WorkerEfficiencyTracker.Instance。
        /// </summary>
        public static System.Action<AWorker> DeathProvider { get; set; }
            = (worker) =>
            {
                if (!Core.GameServices.NetworkIsOnlineProvider() || Core.GameServices.NetworkIsMasterClientProvider())
                {
                    Core.ServiceLocator.Get<WorkerManager>().Remove(worker);
                }

                Core.ServiceLocator.Get<WorkerEfficiencyTracker>().RecordWorkerDeath(worker);
            };

        /// <summary>
        /// 日志提供者 — Worker 相关的错误/警告日志。
        /// 默认实现访问 ServiceLocator.Get<LogManager>()。
        /// </summary>
        public static System.Action<string, LogManager.LogLevelEnum> LogProvider { get; set; }
            = (msg, level) => ServiceLocator.Get<LogManager>().Log(msg, level);

        private Dictionary<int, ResourceInfo> resourceInfos; // 携带的资源
        private Slider progress;
        private Text nameUI;
        private CharacterStatusUI statusBar; // 记录实例化血条
        private int dialoguePauseCount;

        /// <summary>
        /// Worker状态
        /// </summary>
        public Text WorkerStateText { get; set; }

        /// <summary>
        /// 寻路
        /// </summary>
        public ASeek Seek { get; set; }

        /// <summary>
        /// Worker的床
        /// </summary>
        public ABed BedItem { get; set; }

        /// <summary>
        /// 是否因为对话暂停行动。
        /// </summary>
        public bool IsDialoguePaused => this.dialoguePauseCount > 0;

        /// <summary>
        /// 状态管理器
        /// </summary>
        public WorkerStateManager<ICharacterState, AWorkerState.TypeEnum, AWorker> Manager { get; private set; }

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            this.basicAttribute = new Attribute(1.0f, 1.0f, 1.0f, 1.0f, 0.05f, 1.0f, 1.0f, 1.0f);
            this.CharacterDataLAB = new WorkerData();
            this.CharacterDataLAB.Character = this;
            this.CharacterDataLAB.Weapon = (AWeapon)AWorkerTask.ItemFactoryProvider("CustomSword");
            this.Manager = new WorkerStateManager<ICharacterState, AWorkerState.TypeEnum, AWorker>(this);
            this.nameUI = this.transform.Find("Name").GetComponent<Text>();
            this.WorkerStateText = this.transform.Find("State").GetComponent<Text>();
            this.progress = this.transform.Find("Progress").GetComponent<Slider>();
            this.progress.gameObject.SetActive(false);
            this.resourceInfos = new Dictionary<int, ResourceInfo>();
            this.statusBar = this.transform.Find("Hp").GetComponent<CharacterStatusUI>();
            if (this.statusBar == null)
            {
                LogProvider("statusBar Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.Seek = new AStar(this);
            this.AttackLayers = LayerMask.GetMask("Tile", LayerConstant.ENEMY_LAYER);
            this.AttackTags = new List<string>
            {
                "Enemy",
            };
        }

        /// <inheritdoc/>
        public override void Start()
        {
            base.Start();
            this.nameUI.text = this.name;
            this.statusBar.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);

            // 添加 NPC 对话触发器
            NPCDialogueTrigger trigger = this.GetComponent<NPCDialogueTrigger>();
            if (trigger == null)
            {
                trigger = this.gameObject.AddComponent<NPCDialogueTrigger>();
                trigger.profileName = "Worker";
            }

            // 迁移逻辑：旧存档 Worker 有家但 LifeStage 为默认值 Bootstrap → 升级到 Settled
            WorkerData wd = this.CharacterDataLAB as WorkerData;
            if (wd != null && wd.HomePosition != null && wd.LifeStage == Domain.Worker.WorkerLifeStage.Bootstrap)
            {
                wd.LifeStage = Domain.Worker.WorkerLifeStage.Settled;
            }
        }

        public void Update()
        {
            this.transform.rotation = Quaternion.identity;

            if (this.IsDialoguePaused)
            {
                this.UpdateDialoguePauseText();
                return;
            }

            // 执行当前状态的函数
            this.Manager.CurrentState.OnUpdate();

            // 每 60 帧更新人格：饥饿/疲劳/精气神导致心情下降
            if (Time.frameCount % 60 == 0)
            {
                WorkerData wd = this.CharacterDataLAB as WorkerData;
                if (wd != null)
                {
                    float hungryRatio = wd.MaxHungry > 0 ? wd.CurHungry / wd.MaxHungry : 1f;
                    float tiredRatio = wd.MaxTired > 0 ? wd.CurTired / wd.MaxTired : 1f;
                    wd.Personality = wd.Personality.AfterSuffer(hungryRatio, tiredRatio);

                    // 精气神过低额外降低心情
                    float spiritRatio = wd.MaxSpirit > 0 ? wd.CurSpirit / wd.MaxSpirit : 1f;
                    if (spiritRatio < 0.3f)
                    {
                        float spiritPenalty = (1f - spiritRatio) * 2f;
                        wd.Personality = new Domain.Worker.WorkerPersonality(
                            Math.Max(0f, wd.Personality.Mood - spiritPenalty),
                            wd.Personality.Ambition,
                            wd.Personality.Diligence,
                            wd.Personality.Sociality);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void Attack()
        {
        }

        /// <summary>
        /// 设置任务进度条
        /// </summary>
        /// <param name="value">进度值</param>
        /// <param name="enable">是否显示进度条</param>
        public void SetProgress(float value, bool enable)
        {
            this.progress.value = value;
            this.progress.gameObject.SetActive(enable);
        }

        /// <summary>
        /// 对话开始时暂停 Worker 的状态机和任务进度。
        /// </summary>
        public void PauseForDialogue()
        {
            this.dialoguePauseCount++;
            this.UpdateDialoguePauseText(true);
        }

        /// <summary>
        /// 对话结束后恢复 Worker 原来的状态机。
        /// </summary>
        public void ResumeFromDialogue()
        {
            if (this.dialoguePauseCount <= 0)
            {
                return;
            }

            this.dialoguePauseCount--;
            if (this.dialoguePauseCount == 0 && this.WorkerStateText != null)
            {
                this.WorkerStateText.text = this.Manager.CurrentStateType.ToString();
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string resources = string.Empty;
            foreach (KeyValuePair<int, ResourceInfo> resource in this.resourceInfos)
            {
                ItemData itemData = AWorkerTask.ItemDataProvider(resource.Key);
                resources += $"  {itemData.CnName}(id:{resource.Key}) x{resource.Value.Count}\n";
            }

            WorkerData workerData = this.CharacterDataLAB as WorkerData;
            string taskInfo = string.Empty;
            if (workerData.Task != null)
            {
                taskInfo += $"Task:{workerData.Task.TaskType}:{workerData.Task.TaskId}\n" +
                    $"TaskTarget:{workerData.Task.TargetMap}\n";
            }

            string equipmentInfo = string.Empty;
            if (workerData.Weapon != null)
            {
                equipmentInfo += $"  武器: {AWorkerTask.ItemDataProvider(workerData.Weapon.Id).CnName}\n";
            }

            Dictionary<AEquipment.EquipTypeEnum, AEquipment> equipments = workerData.GetEquipments();
            foreach (var item in equipments)
            {
                if (item.Value != null)
                {
                    equipmentInfo += $"  {EquipmentLootTool.GetSlotName(item.Key)}: {AWorkerTask.ItemDataProvider(item.Value.Id).CnName}\n";
                }
            }

            return base.ToString() +
                $"状态:{this.Manager.CurrentStateType}\n" +
                taskInfo +
                $"IsSeeking:{this.Seek.IsSeeking()}\n" +
                $"饥饿值: {workerData.CurHungry:F0}/{workerData.MaxHungry:F0}\n" +
                $"疲劳值: {workerData.CurTired:F0}/{workerData.MaxTired:F0}\n" +
                $"最大携带: {workerData.MaxResourceCount}\n" +
                $"钱包: {workerData.Wallet}\n" +
                $"人格: {workerData.Personality}\n" +
                $"TargetMap:{this.Seek.TargetMap}\n" +
                $"SeekId:{this.CharacterDataLAB.SeekId}\n" +
                $"装备:\n{equipmentInfo}" +
                $"携带资源:\n{resources}";
        }

        /// <summary>
        /// 添加携带的资源
        /// </summary>
        /// <param name="resourceInfo">资源</param>
        public void AddResource(ResourceInfo resourceInfo)
        {
            if (resourceInfo.Count == 0)
            {
                return;
            }

            if (this.resourceInfos.ContainsKey(resourceInfo.Id))
            {
                this.resourceInfos[resourceInfo.Id].Count += resourceInfo.Count;
            }
            else
            {
                this.resourceInfos.Add(resourceInfo.Id, DataTool.DeepCopyByBinary(resourceInfo));
            }
        }

        /// <summary>
        /// 死亡丢弃身上的资源
        /// </summary>
        public void DropResource()
        {
            foreach (KeyValuePair<int, ResourceInfo> resource in this.resourceInfos)
            {
                if (resource.Value.Count <= 0)
                {
                    continue;
                }

                ABackpackItem item = AWorkerTask.ItemFactoryByIdProvider(resource.Key);
                AWorkerTask.TryMergeOrPlaceDrop(
                    AWorkerTask.TileMapWorldToMapProvider(this.transform.position),
                    resource.Value, item.Tile.name);
            }
        }

        /// <summary>
        /// 删除携带的资源
        /// </summary>
        /// <param name="needResource">资源</param>
        public void SubResource(Dictionary<int, ResourceInfo> needResource)
        {
            foreach (KeyValuePair<int, ResourceInfo> need in needResource)
            {
                if (this.resourceInfos.ContainsKey(need.Key))
                {
                    this.resourceInfos[need.Key].Count -= need.Value.Count;
                }
                else
                {
                    LogProvider("自身资源不够，仍然建造成功，错误", LogManager.LogLevelEnum.Error);
                }
            }
        }

        /// <summary>
        /// 删除携带的资源
        /// </summary>
        /// <param name="resourceInfo">资源</param>
        public void SubResource(ResourceInfo resourceInfo)
        {
            if (resourceInfo.Count == 0)
            {
                return;
            }

            if (this.resourceInfos.ContainsKey(resourceInfo.Id))
            {
                this.resourceInfos[resourceInfo.Id].Count -= resourceInfo.Count;
            }
            else
            {
                LogProvider("自身资源不够，仍然建造成功，错误", LogManager.LogLevelEnum.Error);
            }
        }

        /// <summary>
        /// 根据ID获得携带的资源数量
        /// </summary>
        /// <param name="id">资源ID</param>
        /// <returns>资源数量</returns>
        public int GetResourceCountById(int id)
        {
            if (this.resourceInfos.ContainsKey(id))
            {
                return this.resourceInfos[id].Count;
            }

            return 0;
        }

        /// <summary>
        /// 获取 Worker 携带的所有资源（只读副本，供 MarketService 等使用）。
        /// </summary>
        /// <returns>资源信息列表</returns>
        public List<ResourceInfo> GetAllResources()
        {
            List<ResourceInfo> result = new List<ResourceInfo>();
            foreach (KeyValuePair<int, ResourceInfo> kv in this.resourceInfos)
            {
                if (kv.Value.Count > 0)
                {
                    result.Add(new ResourceInfo(kv.Value.Id, kv.Value.Count, kv.Value.OwnerId));
                }
            }

            return result;
        }

        /// <summary>
        /// 将携带的资源存入个人仓库。
        /// </summary>
        /// <param name="resourceInfo">要存入的资源（Count 为存入数量）</param>
        public void DepositToStorage(ResourceInfo resourceInfo)
        {
            if (resourceInfo == null || resourceInfo.Count <= 0) return;
            WorkerData wd = this.CharacterDataLAB as WorkerData;
            if (wd == null) return;

            // 先从身上扣
            this.SubResource(resourceInfo);

            // 存入仓库（保持 OwnerId）
            if (wd.Storage.ContainsKey(resourceInfo.Id))
            {
                wd.Storage[resourceInfo.Id].Count += resourceInfo.Count;
            }
            else
            {
                wd.Storage[resourceInfo.Id] = new ResourceInfo(
                    resourceInfo.Id, resourceInfo.Count, resourceInfo.OwnerId);
            }
        }

        /// <summary>
        /// 从个人仓库取出资源到身上。
        /// </summary>
        /// <param name="id">物品ID</param>
        /// <param name="count">取出数量</param>
        /// <returns>实际取出的数量</returns>
        public int WithdrawFromStorage(int id, int count)
        {
            WorkerData wd = this.CharacterDataLAB as WorkerData;
            if (wd == null || !wd.Storage.ContainsKey(id)) return 0;

            ResourceInfo stored = wd.Storage[id];
            int take = Math.Min(stored.Count, count);
            if (take <= 0) return 0;

            stored.Count -= take;
            if (stored.Count <= 0) wd.Storage.Remove(id);

            // 添加到身上（保持 OwnerId）
            this.AddResource(new ResourceInfo(id, take, stored.OwnerId));
            return take;
        }

        /// <summary>
        /// 获取个人仓库所有资源。
        /// </summary>
        public List<ResourceInfo> GetStorageResources()
        {
            WorkerData wd = this.CharacterDataLAB as WorkerData;
            List<ResourceInfo> result = new List<ResourceInfo>();
            if (wd?.Storage == null) return result;
            foreach (var kv in wd.Storage)
            {
                if (kv.Value.Count > 0)
                    result.Add(new ResourceInfo(kv.Value.Id, kv.Value.Count, kv.Value.OwnerId));
            }
            return result;
        }

        /// <summary>
        /// 仓库中是否有指定数量的物品。
        /// </summary>
        public bool HasInStorage(int id, int count)
        {
            WorkerData wd = this.CharacterDataLAB as WorkerData;
            return wd?.Storage != null
                && wd.Storage.TryGetValue(id, out ResourceInfo r)
                && r.Count >= count;
        }

        /// <summary>
        /// 判断worker携带的资源够不够建造
        /// </summary>
        /// <param name="needResource">需要建造的资源</param>
        /// <returns>是否</returns>
        public bool IsEnough(Dictionary<int, ResourceInfo> needResource)
        {
            foreach (KeyValuePair<int, ResourceInfo> need in needResource)
            {
                if (!this.resourceInfos.ContainsKey(need.Key) || this.resourceInfos[need.Key].Count < need.Value.Count)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 放弃任务
        /// </summary>
        public void GiveUpTask()
        {
            AWorker.WorkerData workerData = this.CharacterDataLAB as AWorker.WorkerData;
            GiveUpTaskProvider(this, workerData.Task);
            workerData.Task = null;
            this.Manager.ChangeState(AWorkerState.TypeEnum.Seek);
        }

        private void UpdateDialoguePauseText(bool force = false)
        {
            if (!force && Time.frameCount % 60 != 0)
            {
                return;
            }

            if (this.WorkerStateText == null)
            {
                return;
            }

            AWorker.WorkerData workerData = this.CharacterDataLAB as AWorker.WorkerData;
            if (workerData != null && workerData.Task != null)
            {
                this.WorkerStateText.text = $"对话中\n暂停任务: {workerData.Task.Name}";
                return;
            }

            this.WorkerStateText.text = "对话中";
        }

        /// <summary>
        /// 建造所需要的资源数量减去worker身上所带的资源数量
        /// </summary>
        /// <param name="needResource">需要的资源</param>
        /// <returns>Worker携带不够的资源数</returns>
        public Dictionary<int, ResourceInfo> GetRemaining(Dictionary<int, ResourceInfo> needResource)
        {
            Dictionary<int, ResourceInfo> remaining = new ();
            foreach (KeyValuePair<int, ResourceInfo> need in needResource)
            {
                if (this.resourceInfos.ContainsKey(need.Key))
                {
                    remaining.Add(need.Key, new ResourceInfo(need.Key, need.Value.Count - this.resourceInfos[need.Key].Count));
                }
                else
                {
                    remaining.Add(need.Key, DataTool.DeepCopyByBinary(need.Value));
                }
            }

            return remaining;
        }

        /// <inheritdoc/>
        public override void ReduceHp(float hp, Character attacker, bool isCRT = false)
        {
            if (hp <= 0)
            {
                LogProvider("Hp can't less than zero!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            base.ReduceHp(hp, attacker, isCRT);
            this.statusBar.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
            if (this.Manager.CurrentStateType != AWorkerState.TypeEnum.Attack)
            {
                this.Manager.ChangeState(AWorkerState.TypeEnum.Attack);
            }
            else
            {
                this.Manager.CurrentState.Reset();
            }
        }

        /// <inheritdoc/>
        protected override void Death()
        {
            base.Death();
            this.statusBar.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
            DeathProvider(this);
            this.Manager.ChangeState(AWorkerState.TypeEnum.Dead);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            this.collisionBugDetector.AddColliderCount(DateTime.Now.Ticks);
            if (this.collisionBugDetector.IsBug(this.name, 1000))
            {
                this.Manager.ChangeState(AWorkerState.TypeEnum.Seek);
            }
        }

        /// <summary>
        /// 敌人数据
        /// </summary>
        [Serializable]
        public class WorkerData : CharacterData
        {
            /// <summary>
            /// 最大疲劳值
            /// </summary>
            public float MaxTired = 100.0f;

            /// <summary>
            /// 当前疲劳值
            /// </summary>
            public float CurTired = 100.0f;

            /// <summary>
            /// 最大饥饿值
            /// </summary>
            public float MaxHungry = 100.0f;

            /// <summary>
            /// 当前饥饿值
            /// </summary>
            public float CurHungry = 100.0f;

            /// <summary>
            /// 最大持有资源数量
            /// </summary>
            public int MaxResourceCount = 30;

            /// <summary>
            /// 是否需要做任务的开关
            /// 使用 WorkerTaskType 作为键的字典，未在字典中的任务类型视为关闭。
            /// </summary>
            public Dictionary<WorkerTaskType, bool> TaskToggle;

            /// <summary>
            /// 当前状态
            /// </summary>
            public AWorkerState.TypeEnum CurrentStateType;

            /// <summary>
            /// 任务
            /// </summary>
            public AWorkerTask Task;

            /// <summary>
            /// 货币钱包
            /// </summary>
            public Domain.Worker.CurrencyAmount Wallet = new Domain.Worker.CurrencyAmount(30);

            /// <summary>
            /// 人格数值 — 心情、事业心、勤奋、社交。
            /// 影响 Worker 自主决策行为和效率。
            /// </summary>
            public Domain.Worker.WorkerPersonality Personality = Domain.Worker.WorkerPersonality.Neutral;

            /// <summary>
            /// 上次空闲帧数 — 用于检测连续空闲以调整人格。
            /// </summary>
            public long LastActiveFrame;

            /// <summary>
            /// 个人仓库 — Worker 可将资源存放到这里（不受携带上限限制）。
            /// Key: 物品ID, Value: 资源信息（含所有权）。
            /// </summary>
            public Dictionary<int, ResourceInfo> Storage;

            /// <summary>当前目标 — 驱动 Worker 的悬赏和自主行为。</summary>
            public Domain.Worker.WorkerGoal CurrentGoal = Domain.Worker.WorkerGoal.EarnMoney();

            /// <summary>家/床位置。default(Vector3IntLAB) 表示无家，由 FurnitureManager 分配床时同步写入。</summary>
            public Vector3IntLAB HomePosition;

            /// <summary>规划的建家位置。default(Vector3IntLAB) 表示未规划，Worker 首次 Seek 时由 WorkerBrain 选定。</summary>
            public Vector3IntLAB PlannedHomePosition;

            /// <summary>建家阶段：0=需要建房间, 1=房间已建需要床, 2=完成。</summary>
            public int HomeBuildStage;

            /// <summary>当前精气神值。</summary>
            public float CurSpirit = 100.0f;

            /// <summary>最大精气神值。</summary>
            public float MaxSpirit = 100.0f;

            /// <summary>生命周期阶段。</summary>
            public Domain.Worker.WorkerLifeStage LifeStage = Domain.Worker.WorkerLifeStage.Bootstrap;

            /// <summary>连续地面睡眠次数（用于递增惩罚）。</summary>
            public int GroundSleepCount;

            /// <summary>当前阶段需要囤积的食物数量。</summary>
            public int FoodStockpileTarget = 3;

            /// <summary>漫游剩余路点数 — >0 表示正在漫游，到达目标后递减并继续漫游。</summary>
            public int WanderWaypointsRemaining;

            public WorkerData()
            {
                // 设置默认可接受任务类型
                // 空字典 = 所有任务类型默认允许。
                // 只有玩家通过 UI 手动关闭的任务类型才会被写入 false。
                // 参见 AWorkerTask.IsCanWork 的 opt-out 语义。
                this.TaskToggle = new Dictionary<WorkerTaskType, bool>();
                this.Personality = Domain.Worker.WorkerPersonality.Randomize();
                this.Storage = new Dictionary<int, ResourceInfo>();
            }
        }
    }
}
