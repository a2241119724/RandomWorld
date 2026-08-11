namespace LAB2D.Character.Worker
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Character.Worker.State;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Constant;
    using LAB2D.Core.Seek;
    using LAB2D.Item;
    using LAB2D.Map;
    using LAB2D.Item.Build.Furniture.Bed;
    using LAB2D.Serializable;
    using LAB2D.UI.Character;
    using System;
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.UI;
    using LAB2D.Data;
    using LAB2D.Domain.Common;

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
        private float lastPotionUseTime = float.MinValue;
        private const float PotionCooldownSeconds = 3.0f;
        private const float LowHpThreshold = 0.3f;
        private const float HealAmount = 10.0f;
        private static int? cachedAddHpItemId;
        private Slider progress;
        private Text nameUI;
        private Text dialogText; // Dialog/Text — 内心独白
        private GameObject dialogRoot; // Dialog 根节点
        private float dialogTextTimer; // 独白切换计时器
        private float dialogTextSwitchInterval = 5.0f; // 下次切换独白的间隔
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
            this.CharacterDataLAB.Weapon = (AWeapon)AWorkerTask.ItemFactoryProvider(PrefabConstant.CUSTOM_SWORD);
            this.Manager = new WorkerStateManager<ICharacterState, AWorkerState.TypeEnum, AWorker>(this);
            this.nameUI = this.transform.Find("Name").GetComponent<Text>();
            this.WorkerStateText = this.transform.Find("State").GetComponent<Text>();
            Transform dialogTrans = this.transform.Find("Dialog");
            if (dialogTrans != null)
            {
                this.dialogRoot = dialogTrans.gameObject;
                Transform textTrans = dialogTrans.Find("Text");
                if (textTrans != null)
                {
                    this.dialogText = textTrans.GetComponent<Text>();
                    if (this.dialogText != null)
                    {
                        this.dialogRoot.SetActive(false);
                    }
                    else
                    {
                        LogProvider($"[Monologue] {this.name} Dialog/Text 缺少 Text 组件!", LogManager.LogLevelEnum.Warning);
                    }
                }
                else
                {
                    LogProvider($"[Monologue] {this.name} Dialog 下没有 Text 子节点!", LogManager.LogLevelEnum.Warning);
                }
            }
            else
            {
                LogProvider($"[Monologue] {this.name} 没有 Dialog 子节点!", LogManager.LogLevelEnum.Warning);
            }
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
            this.AttackLayers = LayerMask.GetMask("Tile", "BuildTile", LayerConstant.ENEMY_LAYER);
            this.AttackTags = new List<string>
            {
                "Enemy",
            };
        }

        /// <inheritdoc/>
        public override void Start()
        {
            base.Start();
            this.MoveSpeed = 6f;
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

            // 初始化状态（从 Awake 移至此处，确保读档时 CharacterDataLAB 已被覆盖后再进入状态）
            if (this.Manager.CurrentState == null)
            {
                this.Manager.ChangeState(AWorkerState.TypeEnum.Seek);
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

            // 执行当前状态的函数（状态可能在 OnUpdate 中切换）
            AWorkerState.TypeEnum stateBefore = this.Manager.CurrentStateType;
            if (this.Manager.CurrentState != null)
            {
                this.Manager.CurrentState.OnUpdate();
            }

            // 脱离战斗时尝试使用血瓶
            if (stateBefore == AWorkerState.TypeEnum.Attack
                && this.Manager.CurrentStateType != AWorkerState.TypeEnum.Attack)
            {
                this.TryConsumeHealthPotion();
            }

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

                // 检查血量是否过低，尝试使用血瓶
                float hpRatio = this.CharacterDataLAB.MaxHp > 0f
                    ? this.CharacterDataLAB.Hp / this.CharacterDataLAB.MaxHp
                    : 1f;
                if (hpRatio > 0f && hpRatio < LowHpThreshold)
                {
                    this.TryConsumeHealthPotion();
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
        /// 显示随机内心独白（闲逛漫游时调用）。
        /// 根据 Worker 当前状态选择合适的独白内容。
        /// </summary>
        /// <param name="taskType">可选：当前任务类型，传入则偏向任务相关独白</param>
        public void ShowRandomMonologue(LAB2D.Enum.WorkerTaskType? taskType = null)
        {
            if (this.dialogText == null || this.dialogRoot == null)
            {
                return;
            }

            WorkerData wd = this.CharacterDataLAB as WorkerData;
            if (wd == null)
            {
                return;
            }

            // 每隔一定时间切换独白（使用 deltaTime 累计）
            this.dialogTextTimer += this.DeltaTime;
            if (this.dialogTextTimer < this.dialogTextSwitchInterval && this.dialogRoot.activeSelf)
            {
                return;
            }

            this.dialogTextTimer = 0f;

            string text;
            if (taskType.HasValue)
            {
                this.dialogTextSwitchInterval = UnityEngine.Random.Range(6.0f, 12.0f);
                text = Constant.WorkerInnerMonologue.GetRandomForTask(
                    taskType.Value,
                    wd.CurHungry, wd.MaxHungry,
                    wd.CurTired, wd.MaxTired);
            }
            else
            {
                this.dialogTextSwitchInterval = UnityEngine.Random.Range(4.0f, 8.0f);
                text = Constant.WorkerInnerMonologue.GetRandom(
                    wd.CurHungry, wd.MaxHungry,
                    wd.CurTired, wd.MaxTired,
                    wd.CurSpirit, wd.MaxSpirit);
            }

            this.dialogText.text = text;
            this.dialogRoot.SetActive(true);
        }

        /// <summary>
        /// 隐藏内心独白（Worker 获得任务或进入非漫游状态时调用）。
        /// </summary>
        public void HideDialogText()
        {
            if (this.dialogRoot != null)
            {
                this.dialogRoot.SetActive(false);
                this.dialogTextTimer = 0f;
            }
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
                string ownerLabel = Domain.Worker.ItemOwnershipService.GetOwnerLabel(resource.Value);
                resources += $"  {itemData.CnName}(id:{resource.Key}) x{resource.Value.Count} [{ownerLabel}]\n";
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
                $"碰撞计数:{this.collisionBugDetector.ColliderCount}\n" +
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
        /// 获取携带的全部资源（用于存档）。
        /// </summary>
        internal Dictionary<int, ResourceInfo> GetCarriedResources()
        {
            return this.resourceInfos;
        }

        /// <summary>
        /// 恢复携带的全部资源（用于读档）。
        /// </summary>
        internal void RestoreCarriedResources(Dictionary<int, ResourceInfo> resources)
        {
            this.resourceInfos = resources ?? new Dictionary<int, ResourceInfo>();
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

            // 重置建造卡死重试计数
            if (workerData != null)
            {
                workerData.BuildStuckRetryCount = 0;
            }

            // 采集任务被放弃时，释放 GatherMap 认领锁（配合失败缓存防止重复选取）
            if (workerData.Task != null && workerData.Task.TaskType == WorkerTaskType.Gather)
            {
                Core.ServiceLocator.Get<GatherMap>().CancelGather(
                    Vector3IntLAB.ToVector3Int(workerData.Task.TargetMap));
            }

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

            // 仅存活时切换状态（防止覆盖 Dead 状态）
            if (this.CharacterDataLAB.Hp > 0f)
            {
                if (this.Manager.CurrentStateType != AWorkerState.TypeEnum.Attack)
                {
                    this.Manager.ChangeState(AWorkerState.TypeEnum.Attack);
                }
                else
                {
                    this.Manager.CurrentState.Reset();
                }
            }
        }

        /// <summary>
        /// 获取 AddHp 血瓶的物品 ID（静态懒加载缓存）。
        /// </summary>
        /// <returns>物品 ID，失败返回 -1</returns>
        private static int GetAddHpItemId()
        {
            if (!cachedAddHpItemId.HasValue)
            {
                ItemData itemData = ServiceLocator.Get<ItemDataManager>().GetByName("AddHp");
                cachedAddHpItemId = (itemData != null && itemData != ItemData.Empty)
                    ? itemData.Id
                    : -1;
            }

            return cachedAddHpItemId.Value;
        }

        /// <summary>
        /// 给 Worker 加血，使用 CharacterHealthComponent 统一处理回血逻辑。
        /// </summary>
        /// <param name="hp">回复的血量</param>
        private void AddHp(float hp)
        {
            CharacterRuntimeState state = CharacterRuntimeState.FromCharacterData(
                this.CharacterDataLAB.Hp,
                this.CharacterDataLAB.MaxHp,
                this.CharacterDataLAB.Mp,
                this.CharacterDataLAB.MaxMp,
                this.CharacterDataLAB.Level,
                this.CharacterDataLAB.CurExperience,
                this.CharacterDataLAB.MaxExperience);
            CharacterRuntimeState newState = this.healthComponent.ApplyHealingToState(state, hp);
            this.CharacterDataLAB.Hp = newState.Hp;
            this.statusBar.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
        }

        /// <summary>
        /// 尝试消耗一个血瓶回血。
        /// 优先消耗身上携带的，身上没有再消耗个人仓库的。
        /// 受冷却时间限制，HP 已满或已死亡时跳过。
        /// </summary>
        /// <returns>是否成功使用了血瓶</returns>
        private bool TryConsumeHealthPotion()
        {
            // HP 已满，不浪费血瓶
            if (this.CharacterDataLAB.Hp >= this.CharacterDataLAB.MaxHp)
            {
                return false;
            }

            // 已死亡，跳过
            if (this.CharacterDataLAB.Hp <= 0f)
            {
                return false;
            }

            // 冷却检查
            float now = this.gameTime.Time;
            if (now - this.lastPotionUseTime < PotionCooldownSeconds)
            {
                return false;
            }

            // 获取血瓶物品 ID
            int itemId = GetAddHpItemId();
            if (itemId < 0)
            {
                return false;
            }

            bool consumed = false;
            WorkerData wd = this.CharacterDataLAB as WorkerData;

            // 优先从身上携带的资源中消耗
            if (this.resourceInfos.TryGetValue(itemId, out ResourceInfo carried)
                && carried.Count > 0)
            {
                this.SubResource(new ResourceInfo(itemId, 1, carried.OwnerId));
                consumed = true;
            }
            // 身上没有则从个人仓库中消耗
            else if (wd?.Storage != null
                && wd.Storage.TryGetValue(itemId, out ResourceInfo stored)
                && stored.Count > 0)
            {
                stored.Count -= 1;
                if (stored.Count <= 0)
                {
                    wd.Storage.Remove(itemId);
                }

                consumed = true;
            }

            if (!consumed)
            {
                return false;
            }

            // 应用回血
            this.AddHp(HealAmount);
            this.lastPotionUseTime = now;
            return true;
        }

        /// <inheritdoc/>
        protected override void Death()
        {
            base.Death();
            this.statusBar.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
            DeathProvider(this);
            this.Manager.ChangeState(AWorkerState.TypeEnum.Dead);
        }

        /// <summary>
        /// GameObject 销毁时停止寻路线程并清理资源，
        /// 防止关闭游戏时后台 ThreadPool 线程访问已销毁对象导致卡死。
        /// </summary>
        protected void OnDestroy()
        {
            // 停止寻路，让后台线程检测到 isStopThread 后退出
            this.Seek?.StopMove();

            // 清理 LineRenderer 的材质实例（每个 Worker 在 ASeek 构造中创建了一个）
            if (this.Seek?.LineRenderer != null)
            {
                Material mat = this.Seek.LineRenderer.material;
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            this.collisionBugDetector.AddColliderCount(DateTime.Now.Ticks);
            if (this.collisionBugDetector.IsBug(this.name, 100))
            {
                this.collisionBugDetector.ColliderCount = 0; // 重置计数器，防止重复触发

                AWorker.WorkerData workerData = this.CharacterDataLAB as AWorker.WorkerData;

                // 建造任务：碰撞 Bug 触发时优先重试重新寻路，而非直接放弃。
                // 建造现场通常空间狭窄，碰撞频繁但并非真正阻塞。
                // 最大 3 次重试，每次触发提供约 1.6 秒额外调整时间（总计约 5 秒）。
                const int maxRetries = 3;
                if (workerData?.Task != null && workerData.Task.TaskType == WorkerTaskType.Build)
                {
                    workerData.BuildStuckRetryCount++;
                    if (workerData.BuildStuckRetryCount < maxRetries)
                    {
                        // 重新寻路绕过阻塞，不放弃任务
                        this.Manager.ChangeState(AWorkerState.TypeEnum.Seek);
                        return;
                    }

                    workerData.BuildStuckRetryCount = 0;
                }

                this.GiveUpTask(); // 放弃当前任务，让WorkerBrain做新决策避开阻塞点
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
            /// 上次发布悬赏的时间（Time.time），用于冷却控制。
            /// 初始值设为极小值，确保首次可立即发布。
            /// </summary>
            public float LastBountyPostTime = -999f;

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

            /// <summary>任务完成后强制下次 Seek 立即触发决策，跳过随机漫游。</summary>
            public bool ForceDecisionOnNextSeek;

            /// <summary>当前阶段需要囤积的食物数量。</summary>
            public int FoodStockpileTarget = 3;

            /// <summary>漫游剩余路点数 — >0 表示正在漫游，到达目标后递减并继续漫游。</summary>
            public int WanderWaypointsRemaining;

            /// <summary>
            /// 建造任务卡死重试计数 — 碰撞 Bug 触发时递增，任务完成/切换时重置。
            /// [NonSerialized] 因为是瞬时运行时计数器。
            /// </summary>
            [NonSerialized]
            public int BuildStuckRetryCount;

            /// <summary>
            /// 当前携带的资源（用于存档持久化）。
            /// 与 AWorker.resourceInfos 双向同步：SaveData 时从 resourceInfos 写入、LoadData 时写回 resourceInfos。
            /// </summary>
            public Dictionary<int, ResourceInfo> CarriedResources;

            public WorkerData()
            {
                // 所有任务类型默认开启（opt-out 语义：只有玩家通过 UI 手动关闭的才会被写入 false）
                this.TaskToggle = new Dictionary<WorkerTaskType, bool>();
                for (WorkerTaskType t = 0; t < WorkerTaskType._Count; t++)
                {
                    this.TaskToggle[t] = true;
                }
                this.Personality = Domain.Worker.WorkerPersonality.Randomize();
                this.Storage = new Dictionary<int, ResourceInfo>();
                this.CarriedResources = new Dictionary<int, ResourceInfo>();
            }
        }
    }
}
