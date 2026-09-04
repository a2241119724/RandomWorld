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
    /// 任务写入来源 — 决定 SetTask 的打断语义（任务写入唯一入口 AWorker.SetTask 的第二参数）。
    /// </summary>
    public enum WorkerTaskSource
    {
        /// <summary>
        /// 决策层自建（Seek 状态内自主决策）——不打断当前。
        /// </summary>
        SelfDecision,

        /// <summary>
        /// 推送分配（WorkerTaskManager 分配循环）——置延迟打断标记，回 Seek 重寻路。
        /// </summary>
        PushAssignment,

        /// <summary>
        /// 链式交接（任务 Finish 调用栈内接力）——置延迟打断标记（栈内绝不同步切状态）。
        /// </summary>
        ChainHandoff,

        /// <summary>
        /// 悬赏结算恢复 Task=悬赏本体（已 Start 过，不重启、绝不打断）。
        /// </summary>
        BountyRestore,

        /// <summary>
        /// 置空（完成/放弃/死亡清理）。
        /// </summary>
        Clear,
    }

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
        /// 疲劳值阈值：疲劳值超过 MaxTired-该值时判定为需要休息（疲劳值越大越疲）。
        /// </summary>
        public static readonly float ThresholdTired = 20.0f;

        /// <inheritdoc/>
        /// <remarks>Worker 获得每级 +5% 的属性加成（玩家的减半版本）。</remarks>
        public override bool IsWorkerCharacter => true;

        /// <summary>
        /// 当前反击锁定目标（攻击状态持有）。进入攻击状态时锁定为 LastAttacker，
        /// 之后只有被其他目标攻击才更新——被当前攻击目标打保持不换，
        /// 与 Enemy.ReduceHp 的 Target 语义对称（见 bug-fixes.md 2026-08-16）。
        /// </summary>
        public Character AttackTarget { get; set; }

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
                Core.ServiceLocator.Get<Gameplay.FavorabilityManager>().RemoveDeadWorker(worker.GetInstanceID(), worker.name);
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
        private float mindBubbleUntilTime; // 心智气泡（自主拒绝/人生事件/关系变化反馈）显示截止时间
        private CharacterStatusUI statusBar; // 记录实例化血条
        private int dialoguePauseCount;

        /// <summary>
        /// 刷新头顶血条 — 回合制战斗结束写回 Hp 后调用
        /// （正常路径 ReduceHp/Start 内部刷新，快照写回绕过了这些管线）。
        /// </summary>
        public void RefreshStatusBar()
        {
            this.statusBar?.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
        }

        /// <summary>
        /// Worker状态
        /// </summary>
        public Text WorkerStateText { get; set; }

        /// <summary>
        /// 寻路
        /// </summary>
        public ASeek Seek { get; set; }

        /// <summary>
        /// 移动服务层 — 常驻驱动移动意图（GoTo/KeepDistance/Chase/Stop），
        /// 状态层只声明意图，移动执行与 Sliding/Stuck 熔断统一在此。
        /// </summary>
        public WorkerLocomotion Locomotion { get; private set; }

        /// <summary>
        /// 延迟打断标记 — 推送分配/链式交接写入新任务后由 SetTask 置位，
        /// Update 开头消费（非 Dead/Attack/Escape 时切 Seek 重寻路新目标）。
        /// 写入可能发生在任务 Finish 调用栈内，绝不能同步切状态，故延迟到主循环。
        /// </summary>
        public bool HasPendingTaskInterrupt { get; private set; }

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
            this.nameUI = this.FindHeadChild("Name").GetComponent<Text>();
            this.WorkerStateText = this.FindHeadChild("State").GetComponent<Text>();
            Transform dialogTrans = this.FindHeadChild("Dialog");
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
            this.progress = this.FindHeadChild("Progress").GetComponent<Slider>();
            this.progress.gameObject.SetActive(false);
            this.resourceInfos = new Dictionary<int, ResourceInfo>();
            this.statusBar = this.FindHeadChild("Hp").GetComponent<CharacterStatusUI>();
            if (this.statusBar == null)
            {
                LogProvider("statusBar Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.Seek = new AStar(this);
            this.Locomotion = new WorkerLocomotion(this);
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
            this.MoveSpeed = 2f;
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

            // 心智层读档兜底：老档 Mind 为 null（BinaryFormatter 不跑构造函数），确保非空
            if (wd != null)
            {
                Domain.Worker.WorkerMindData.Ensure(wd);
            }

            // 初始化状态（从 Awake 移至此处，确保读档时 CharacterDataLAB 已被覆盖后再进入状态）
            if (this.Manager.CurrentState == null)
            {
                this.Manager.ChangeState(AWorkerState.TypeEnum.Seek);
            }
        }

        public void Update()
        {
            // 仅在根被意外旋转时归零（rotation setter 无值比较，每帧无条件写会持续
            // 置脏头顶 Canvas 的 transform → PostLateUpdate.PlayerUpdateCanvases 放大）
            if (this.transform.rotation != Quaternion.identity)
            {
                this.transform.rotation = Quaternion.identity;
            }

            if (this.IsDialoguePaused)
            {
                this.UpdateDialoguePauseText();
                return;
            }

            // 紧急生存检测（原 WorkerSeekState.OnUpdate 紧急块上移）：所有状态生效
            //（原仅 Seek 生效——Move/Work 途中饥饿/疲劳不会被打断觅食/休息）。
            // 先于延迟打断消费：生存优先，且 GiveUpTask → ChangeState(Seek) → OnEnter
            // 决策管线完成新决策与寻路（单次决策）。
            if (Time.frameCount % 10 == 0)
            {
                this.CheckSurvivalEmergency();
            }

            // 消费延迟打断：推送分配/链式交接可能在任务 Finish / 分配循环的调用栈内写入新任务
            //（SetTask 置位标记但绝不同步切状态），现在栈已退回主循环——切 Seek 重寻路新目标。
            // 豁免：Dead（清标记，死人不再接手）；Attack/Escape（保命优先，保留标记，
            // 战斗/逃跑结束后状态机自然回 Seek，届时任务已在，可再消费兜底）。
            if (this.HasPendingTaskInterrupt)
            {
                AWorkerState.TypeEnum currentType = this.Manager.CurrentStateType;
                if (currentType == AWorkerState.TypeEnum.Dead)
                {
                    this.HasPendingTaskInterrupt = false;
                }
                else if (currentType != AWorkerState.TypeEnum.Attack
                    && currentType != AWorkerState.TypeEnum.Escape)
                {
                    this.HasPendingTaskInterrupt = false;
                    AWorkerTask.LogProvider(
                        $"[TaskDiag] {this.name} 延迟打断（原状态={currentType}）→ 切Seek执行新任务",
                        LogManager.LogLevelEnum.Debug);
                    this.Manager.ChangeState(AWorkerState.TypeEnum.Seek);
                }
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
                    float tiredRatio = wd.MaxTired > 0 ? wd.CurTired / wd.MaxTired : 0f;
                    // AfterSuffer 的 ratio 语义为"状态充足比例"（大=状态好），
                    // CurTired 反向后是累积疲劳值，需取 1-疲劳比例 传入。
                    wd.Personality = wd.Personality.AfterSuffer(hungryRatio, 1f - tiredRatio);

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

        public void FixedUpdate()
        {
            if (this.IsDialoguePaused) return;

            // 先驱动移动服务层（执行当前意图/刷新到达标记），后跑状态逻辑——
            // 状态 OnUpdate 读到的 Locomotion.HasArrived 是本物理帧的最新值（原 Move 状态时序不变）。
            this.Locomotion?.TickFixed();

            if (this.Manager.CurrentState != null)
            {
                this.Manager.CurrentState.OnFixedUpdate();
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
            if (this.progress.gameObject.activeSelf != enable)
            {
                this.progress.gameObject.SetActive(enable);
            }

            if (!enable)
            {
                return;
            }

            // 逐帧写入（仅值比较挡掉重复值）：任务进度随 deltaTime 连续累积，
            // 0.1s 时间节流会造成可感知的跳格；进度可见时 Canvas 重建本身不可避免，
            // HeadUI 合并为单 Canvas 后重建边界已是每角色 1 个。
            if (this.progress.value != value)
            {
                this.progress.value = value;
            }
        }

        /// <summary>
        /// 显示心智气泡（自主意志拒绝/人生事件/关系变化等反馈）。
        /// 与内心独白共用 dialogText，但用 mindBubbleUntilTime 守卫，
        /// 防止独白定时器立即覆盖心智气泡。任务开始时 HideDialogText 会清除守卫。
        /// </summary>
        /// <param name="text">气泡文本。</param>
        /// <param name="duration">显示时长（秒）。</param>
        public void ShowMindBubble(string text, float duration = 3.5f)
        {
            if (this.dialogText == null || this.dialogRoot == null)
            {
                return;
            }

            this.dialogText.text = text;
            this.dialogRoot.SetActive(true);
            this.dialogTextTimer = 0f;
            this.dialogTextSwitchInterval = duration;
            this.mindBubbleUntilTime = UnityEngine.Time.time + duration;
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

            // 心智气泡展示期间不覆盖（拒绝/事件/关系反馈优先）
            if (UnityEngine.Time.time < this.mindBubbleUntilTime)
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
                this.mindBubbleUntilTime = 0f; // 清除心智气泡守卫
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

            // 各工种熟练度（仅已练过的工种，按中文名展示）
            string skillInfo = string.Empty;
            if (workerData.SkillProficiencies != null && workerData.SkillProficiencies.Count > 0)
            {
                foreach (KeyValuePair<WorkerTaskType, float> kv in workerData.SkillProficiencies)
                {
                    skillInfo += $"  {WorkerTaskSummaryTool.GetTaskDisplayName(kv.Key)}:{kv.Value:F0}\n";
                }
            }

            return base.ToString() +
                $"状态:{this.Manager.CurrentStateType}\n" +
                taskInfo +
                $"IsSeeking:{this.Seek.IsSeeking()}\n" +
                $"卡死检测:{this.Seek.LastStuckResult}\n" +
                $"饥饿值: {workerData.CurHungry:F0}/{workerData.MaxHungry:F0}\n" +
                $"疲劳值: {workerData.CurTired:F0}/{workerData.MaxTired:F0}\n" +
                $"精气神: {workerData.CurSpirit:F0}/{workerData.MaxSpirit:F0}\n" +
                $"压力: {workerData.CurStress:F0}/{workerData.MaxStress:F0}\n" +
                $"士气: {workerData.CurMorale:F0}/{workerData.MaxMorale:F0}\n" +
                $"最大携带: {workerData.MaxResourceCount}\n" +
                $"钱包: {workerData.Wallet}\n" +
                $"人格: {workerData.Personality} 贪婪:{workerData.Greed:F0} 懒惰:{workerData.Laziness:F0}\n" +
                $"熟练度:\n{skillInfo}" +
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
                // 诊断（溢出"可存=空"排查）：同 ID 叠加只加 Count 不更新 OwnerId，首个写入的 OwnerId
                // 会"污染"该 ID 全部分类——若先收到他人悬赏物再叠加自身物品，GetDepositableCount /
                // GetSellableSurplus 会把自身物品误判为"他人悬赏物"不可存不可卖。
                ResourceInfo existing = this.resourceInfos[resourceInfo.Id];
                if (existing.OwnerId != resourceInfo.OwnerId)
                {
                    LogProvider(
                        $"[TaskDiag] {this.name} AddResource 同ID异Owner: id={resourceInfo.Id} 现有@{existing.OwnerId} 新@{resourceInfo.OwnerId} +{resourceInfo.Count}",
                        LogManager.LogLevelEnum.Debug);
                }
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
                    // 扣到 0 移除字典项：残留的 Count=0 项会保留首写 OwnerId，
                    // 下次 AddResource 同 ID 叠加时不更新 OwnerId → 污染新物品归属。
                    if (this.resourceInfos[need.Key].Count <= 0)
                    {
                        this.resourceInfos.Remove(need.Key);
                    }
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
                // 扣到 0 移除字典项（同 Dictionary 重载：防 Count=0 残留首写 OwnerId 污染）
                if (this.resourceInfos[resourceInfo.Id].Count <= 0)
                {
                    this.resourceInfos.Remove(resourceInfo.Id);
                }
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
        /// 身上携带的物品总数量（resourceInfos 各 Count 之和）。
        /// 用于容量判断：MaxResourceCount 是硬上限，超出后不能再拾取。
        /// </summary>
        public int GetTotalCarriedCount()
        {
            int total = 0;
            foreach (KeyValuePair<int, ResourceInfo> kv in this.resourceInfos)
            {
                total += kv.Value.Count;
            }

            return total;
        }

        /// <summary>
        /// 身上再加 additional 是否仍不超过 MaxResourceCount（硬上限判断）。
        /// </summary>
        /// <param name="additional">要增加的数量</param>
        /// <returns>能装下返回 true</returns>
        public bool CanCarry(int additional)
        {
            WorkerData wd = this.CharacterDataLAB as WorkerData;
            return wd == null || this.GetTotalCarriedCount() + additional <= wd.MaxResourceCount;
        }

        /// <summary>
        /// 将携带的资源存入个人仓库。
        /// </summary>
        /// <param name="resourceInfo">要存入的资源（Count 为存入数量）</param>
        /// <returns>是否成功存入（仓库最多 4 种物品，槽满且非同类型时失败）</returns>
        public bool DepositToStorage(ResourceInfo resourceInfo)
        {
            if (resourceInfo == null || resourceInfo.Count <= 0) return false;
            WorkerData wd = this.CharacterDataLAB as WorkerData;
            if (wd == null || wd.Storage == null) return false;

            // 四格语义：仓库最多 4 种物品；已有同 ID 则叠加，否则需要空槽。
            // 先查再扣，槽满时失败且身上不被误扣。
            if (!wd.Storage.ContainsKey(resourceInfo.Id) && wd.Storage.Count >= 4)
            {
                return false;
            }

            // 防快照过期超扣：Store 任务创建时收集的 deposit 数量在步行回家途中可能已变
            //（吃了/卖了/被取走），实际扣减不得超过当前持有量，否则身上变负数且仓库虚增。
            int carriedNow = this.GetResourceCountById(resourceInfo.Id);
            int actualCount = Math.Min(resourceInfo.Count, carriedNow);
            if (actualCount <= 0) return false;

            // 先从身上扣
            this.SubResource(new ResourceInfo(resourceInfo.Id, actualCount, resourceInfo.OwnerId));

            // 存入仓库（保持 OwnerId）
            if (wd.Storage.ContainsKey(resourceInfo.Id))
            {
                wd.Storage[resourceInfo.Id].Count += actualCount;
            }
            else
            {
                wd.Storage[resourceInfo.Id] = new ResourceInfo(
                    resourceInfo.Id, actualCount, resourceInfo.OwnerId);
            }

            return true;
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

            // 添加到身上（仓库共享物取出后归自己，避免保留仓库里的他人物归属污染背包）
            this.AddResource(new ResourceInfo(id, take, this.GetInstanceID()));
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
        /// 个人仓库是否还能存放指定物品（已有同类型可叠加，否则需要空槽）。
        /// 四格语义：仓库最多 4 种物品。
        /// 旧存档（pre-Storage，Storage == null）返回 false，与 DepositToStorage /
        /// GetDepositableResources 的空判定一致，避免决策层发出注定失败的 Store 任务。
        /// </summary>
        public bool HasStorageSpaceFor(int id)
        {
            WorkerData wd = this.CharacterDataLAB as WorkerData;
            return wd?.Storage != null
                && (wd.Storage.ContainsKey(id) || wd.Storage.Count < 4);
        }

        /// <summary>
        /// 挑一件可存入仓库的物品（优先大额，一趟腾最多空间）。
        /// 可存部分 = 保留量之外的数量：食物/种子/药水/材料按类型保留量留底，
        /// 目标材料保留建房所需数，超额可存；他人悬赏物不可存。
        /// 无物可存返回 false（不修改任何状态）。
        /// </summary>
        /// <param name="deposit">挑出的物品（Count 为可存数量，调用方负责存取）</param>
        public bool TryPickDepositableResource(out ResourceInfo deposit)
        {
            deposit = null;
            WorkerData wd = this.CharacterDataLAB as WorkerData;
            if (wd == null) return false;

            List<ResourceInfo> candidates = this.GetAllResources();
            candidates.Sort((a, b) => b.Count.CompareTo(a.Count)); // 大额优先

            foreach (ResourceInfo r in candidates)
            {
                // 只存保留量之外的部分（他人悬赏物不可存）
                int depositable = this.GetDepositableCount(r);
                if (depositable <= 0) continue;
                if (!this.HasStorageSpaceFor(r.Id)) continue;            // 仓库新槽满

                deposit = new ResourceInfo(r.Id, depositable, r.OwnerId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 一次性收集身上所有可存入仓库的物品（供 Store 任务使用），Count 为可存数量（超额部分）。
        /// 与 TryPickDepositableResource 同过滤规则，但收集全部而非单件——
        /// 单件挑选器无副作用，若在 while 循环里反复调用会无限返回同一物品导致挂死。
        /// 带四格槽位预留：仓库已有种类 + 本次新收集的种类不超过 4，保证收集项都能存进。
        /// </summary>
        public List<ResourceInfo> GetDepositableResources()
        {
            List<ResourceInfo> result = new List<ResourceInfo>();
            WorkerData wd = this.CharacterDataLAB as WorkerData;
            if (wd == null || wd.Storage == null) return result;

            List<ResourceInfo> candidates = this.GetAllResources();
            candidates.Sort((a, b) => b.Count.CompareTo(a.Count)); // 大额优先，一趟腾最多空间

            int reservedNewTypes = 0; // 本次新收集的不同种类（占新槽）
            foreach (ResourceInfo r in candidates)
            {
                // 只存保留量之外的部分（他人悬赏物不可存）
                int depositable = this.GetDepositableCount(r);
                if (depositable <= 0) continue;

                ResourceInfo deposit = new ResourceInfo(r.Id, depositable, r.OwnerId);
                if (wd.Storage.ContainsKey(r.Id))
                {
                    result.Add(deposit); // 已有同类型，可叠加，不占新槽
                    continue;
                }

                if (wd.Storage.Count + reservedNewTypes >= 4) continue; // 新槽满，跳过新类型
                reservedNewTypes++;
                result.Add(deposit);
            }

            return result;
        }

        /// <summary>
        /// 某物品可存入个人仓库的数量 = 当前持有 - 保留量（超额才存）。
        /// 保留量规则（TryPickDepositableResource / GetDepositableResources 共用，避免谓词漂移）：
        /// - 他人悬赏物：不可存（返回 0）
        /// - 入仓保留量按携带上限百分比：消耗品 2.5%、材料 4%（Max 修改自动缩放）
        /// - 食物/种子/装备等：按类型保留（食物10/饥饿15、种子5、装备武器1、默认5）
        /// 返回 0 表示无可存超额。
        /// </summary>
        public int GetDepositableCount(ResourceInfo r)
        {
            if (r == null || r.Count <= 0) return 0;
            int selfId = this.GetInstanceID();
            if (r.OwnerId != 0 && r.OwnerId != selfId) return 0; // 他人悬赏物不可存

            int keep = this.GetKeepReserve(r.Id);
            int excess = r.Count - keep;
            return excess > 0 ? excess : 0;
        }

        /// <summary>
        /// 入仓保留量：单种物品低于此数量不存仓库（留身上使用）。
        /// 按"携带上限的百分比"计算（Max 修改时自动缩放，不用绝对数）：
        /// - 消耗品（血瓶等）：2.5%（Max=200 → 5）
        /// - 材料（木头/石头等）：4%（Max=200 → 8）
        /// 不再因建房目标特殊保留——建房材料不够时 Worker 从仓库取（TryMakeWithdrawForBuild）。
        /// 超过保留量的部分才入仓（只放超额，不整类清空）。
        /// </summary>
        /// <summary>入仓保留量：消耗品（血瓶等）占携带上限的比例（Max=200 → 5）。</summary>
        private const float ConsumableKeepRatio = 0.025f;
        /// <summary>入仓保留量：材料（木头/石头等）占携带上限的比例（Max=200 → 8）。</summary>
        private const float MaterialKeepRatio = 0.04f;

        private int GetKeepReserve(int itemId)
        {
            WorkerData wd = this.CharacterDataLAB as WorkerData;
            if (wd == null || wd.MaxResourceCount <= 0) return 0;

            AItem.ItemTypeEnum type = AWorkerTask.ItemTypeProvider(itemId);
            switch (type)
            {
                case AItem.ItemTypeEnum.Consumable:
                    return (int)(wd.MaxResourceCount * ConsumableKeepRatio); // 消耗品留 2.5%
                case AItem.ItemTypeEnum.Material:
                    return (int)(wd.MaxResourceCount * MaterialKeepRatio);   // 材料留 4%
                default:
                    // 食物/种子/装备等：保留现状类型保留量
                    bool isHungry = wd.CurHungry < ThresholdHungry;
                    return this.GetTypeKeepReserve(itemId, isHungry);
            }
        }

        /// <summary>
        /// 按类型保留量（入仓/出售共用，避免谓词漂移）。
        /// 食物10/饥饿15、种子5、材料15、药水5、装备武器1、默认5。
        /// </summary>
        private int GetTypeKeepReserve(int itemId, bool isHungry)
        {
            switch (AWorkerTask.ItemTypeProvider(itemId))
            {
                case AItem.ItemTypeEnum.Food:
                    return isHungry ? 15 : 10;      // 饥饿时保留更多食物
                case AItem.ItemTypeEnum.Seed:
                    return 5;
                case AItem.ItemTypeEnum.Material:
                    return 15;
                case AItem.ItemTypeEnum.Consumable:
                    return 5;
                case AItem.ItemTypeEnum.Equipment:
                case AItem.ItemTypeEnum.Weapon:
                    return 1;
                default:
                    return 5;
            }
        }

        /// <summary>
        /// 溢出失败兜底：收集"可出售的超额物资"（超过出售保留量的部分），供溢出时出售腾空间。
        /// 出售保留量规则与 WorkerSeekState.GetReserveCount 一致：建房目标材料完全保留不卖
        /// （ContainsKey 直接跳过），其余按类型保留（GetTypeKeepReserve），超额部分可卖。
        /// 仅出售自己/无主之物，他人悬赏物不卖。
        /// </summary>
        public List<ResourceInfo> GetSellableSurplus()
        {
            List<ResourceInfo> result = new List<ResourceInfo>();
            WorkerData wd = this.CharacterDataLAB as WorkerData;
            if (wd == null) return result;

            bool isHungry = wd.CurHungry < ThresholdHungry;
            bool hasBuildGoal = wd.CurrentGoal.Type == Domain.Worker.WorkerGoalType.BuildStructure
                && wd.CurrentGoal.HasMaterialNeeds;
            int selfId = this.GetInstanceID();

            foreach (ResourceInfo r in this.GetAllResources())
            {
                if (r.Count <= 0) continue;
                if (r.OwnerId != 0 && r.OwnerId != selfId) continue; // 他人悬赏物不卖

                // 建房目标材料完全保留不卖
                if (hasBuildGoal && wd.CurrentGoal.RequiredMaterials != null
                    && wd.CurrentGoal.RequiredMaterials.ContainsKey(r.Id))
                {
                    continue;
                }

                int excess = r.Count - this.GetTypeKeepReserve(r.Id, isHungry);
                if (excess <= 0) continue;
                result.Add(new ResourceInfo(r.Id, excess, r.OwnerId));
            }

            return result;
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
        /// 任务唯一写入口：赋值 + 启动 + 收口诊断，按来源决定打断语义。
        /// - SelfDecision：决策层自建，不打断（决策动作已有自身日志，收口日志降 Trace 防重复）；
        /// - PushAssignment / ChainHandoff：置延迟打断标记（Update 开头消费切 Seek 重寻路新目标）；
        /// - BountyRestore：恢复结算栈内正在执行的悬赏本体——不重启（已 Start 过）、绝不打断；
        /// - Clear：置空。
        /// </summary>
        public void SetTask(AWorkerTask task, WorkerTaskSource source)
        {
            if (!(this.CharacterDataLAB is WorkerData workerData))
            {
                return;
            }

            AWorkerTask old = workerData.Task;
            workerData.Task = task;

            if (task == null)
            {
                // Clear 置空：调用方（GiveUpTask/Finish/死亡清理）已有各自语义日志，此处降 Trace 防重复
                AWorkerTask.LogProvider(
                    $"[TaskDiag] {this.name} SetTask(null) source={source} old={old?.TaskType.ToString() ?? "null"}",
                    source == WorkerTaskSource.Clear ? LogManager.LogLevelEnum.Trace : LogManager.LogLevelEnum.Debug);
                return;
            }

            if (source != WorkerTaskSource.BountyRestore)
            {
                task.Start(this);
            }

            if (source == WorkerTaskSource.PushAssignment || source == WorkerTaskSource.ChainHandoff)
            {
                this.HasPendingTaskInterrupt = true;
            }

            AWorkerTask.LogProvider(
                $"[TaskDiag] {this.name} SetTask type={task.TaskType} target=({task.TargetMap.X},{task.TargetMap.Y}) source={source}",
                source == WorkerTaskSource.SelfDecision ? LogManager.LogLevelEnum.Trace : LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 紧急生存检测（原 WorkerSeekState.OnUpdate 紧急块上移，每 10 帧调用一次）：
        /// 饥饿/疲劳/精气神/压力超过生存阈值时强制放弃当前任务回 Seek 重决策。
        /// 豁免：对话暂停、Dead（已死）、Attack/Escape（保命优先，战斗/逃跑结束后自然回 Seek 再检测）。
        /// 决策与寻路由 GiveUpTask → ChangeState(Seek) → OnEnter 决策管线完成（单次决策，
        /// 原"重入决策后再 ExecuteAutonomousDecision 二次决策"的双重决策缺陷已消除）。
        /// </summary>
        private void CheckSurvivalEmergency()
        {
            if (this.IsDialoguePaused)
            {
                return;
            }

            AWorkerState.TypeEnum currentType = this.Manager.CurrentStateType;
            if (currentType == AWorkerState.TypeEnum.Dead
                || currentType == AWorkerState.TypeEnum.Attack
                || currentType == AWorkerState.TypeEnum.Escape)
            {
                return;
            }

            WorkerData wd = this.CharacterDataLAB as WorkerData;
            if (wd == null)
            {
                return;
            }

            bool emergency = false;

            // 饥饿 < 15 → 强制触发生存决策
            if (wd.CurHungry > 0 && wd.CurHungry < 15f)
            {
                if (wd.Task != null && wd.Task.TaskType != WorkerTaskType.Eat)
                {
                    this.GiveUpTask();
                    emergency = true;
                }
            }

            // 疲劳 > MaxTired-15 → 强制触发睡觉决策
            if (wd.CurTired < wd.MaxTired && wd.CurTired > wd.MaxTired - 15f)
            {
                if (wd.Task != null
                    && wd.Task.TaskType != WorkerTaskType.Sleep
                    && wd.Task.TaskType != WorkerTaskType.GroundSleep)
                {
                    this.GiveUpTask();
                    emergency = true;
                }
            }

            // 精气神 < 10 → 强制触发漫游/休息决策
            if (wd.CurSpirit > 0 && wd.CurSpirit < 10f)
            {
                if (wd.Task != null
                    && wd.Task.TaskType != WorkerTaskType.Wander
                    && wd.Task.TaskType != WorkerTaskType.Sleep)
                {
                    this.GiveUpTask();
                    emergency = true;
                }
            }

            // 压力 > MaxStress-10 → 强制漫游减压（吃饭/睡觉/漫游本身减压，不打断）
            if (wd.CurStress < wd.MaxStress && wd.CurStress > wd.MaxStress - 10f)
            {
                if (wd.Task != null
                    && wd.Task.TaskType != WorkerTaskType.Wander
                    && wd.Task.TaskType != WorkerTaskType.Sleep
                    && wd.Task.TaskType != WorkerTaskType.GroundSleep
                    && wd.Task.TaskType != WorkerTaskType.Eat)
                {
                    this.GiveUpTask();
                    emergency = true;
                }
            }

            if (!emergency)
            {
                return;
            }

            // 紧急打断诊断（事件点）：生存阈值触发强制放弃任务+重新决策，记录各项状态值。
            // 若同一 Worker 频繁紧急打断，说明生存压力下决策未真正解决问题（如饥饿无食物可采）。
            AWorkerTask.LogProvider(
                $"[StateDiag] {this.name} 紧急打断重决策: 饥饿={wd.CurHungry:F0} 疲劳={wd.CurTired:F0} 精气神={wd.CurSpirit:F0} 压力={wd.CurStress:F0} 士气={wd.CurMorale:F0}",
                LogManager.LogLevelEnum.Debug);

            // 心智层：极端饥饿导致的濒死经历（WorkerMindService 内按游戏日节流，同天只记一次）
            if (wd.CurHungry > 0f && wd.CurHungry < 15f)
            {
                if (Core.ServiceLocator.TryGet<WorkerMindService>(out WorkerMindService mindService))
                {
                    mindService.RecordNearDeath(this);
                }
            }
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

            // 放弃任务 → 完全清空卡死状态，避免污染下一任务
            this.Seek.ResetStuckDetection();

            // 采集任务被放弃时，释放 GatherMap 认领锁（配合失败缓存防止重复选取）
            if (workerData.Task != null && workerData.Task.TaskType == WorkerTaskType.Gather)
            {
                Core.ServiceLocator.Get<GatherMap>().CancelGather(
                    Vector3IntLAB.ToVector3Int(workerData.Task.TargetMap));
            }

            // [TaskDiag] 记录放弃事件：任务类型 + 目标坐标（GiveUpTask 是任务失败的终端事件点）
            AWorkerTask giveUpTask = workerData.Task;
            if (giveUpTask != null)
            {
                AWorkerTask.LogProvider(
                    $"[TaskDiag] {this.name} 放弃任务 type={giveUpTask.TaskType} target=({giveUpTask.TargetMap.X},{giveUpTask.TargetMap.Y})",
                    LogManager.LogLevelEnum.Debug);
            }

            GiveUpTaskProvider(this, workerData.Task);
            this.SetTask(null, WorkerTaskSource.Clear);
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

            // 好感度：攻击者关系惩罚（Player 打 Worker → 对玩家好感下降；Worker 互殴 → 受害对肇事下降）
            if (attacker != null)
            {
                bool lethal = this.CharacterDataLAB.Hp <= 0f;
                FavorabilityManager fm = Core.ServiceLocator.Get<FavorabilityManager>();
                if (fm != null)
                {
                    if (attacker.IsPlayerCharacter)
                    {
                        float delta = FavorabilityConstant.AttackToPlayerDelta;
                        if (lethal) delta += FavorabilityConstant.KillToPlayerBonus; // 致死额外惩罚
                        fm.ModifyWithPlayer(this, delta, "被玩家攻击");
                    }
                    else if (attacker.IsWorkerCharacter)
                    {
                        fm.ModifyFavorability(this, attacker.GetInstanceID(), FavorabilityConstant.WorkerAttackDelta, "被其他工人攻击");
                    }
                }

                // 心智层：被攻击事件记忆（事件点单次调用）。
                // 玩家攻击 → EVT_PLAYER_ATTACK（致死升级为 EVT_PLAYER_KILL，强度更高，弹气泡）；
                // Worker 互殴 → EVT_WORKER_ATTACK（目标为肇事者名）。
                if (Core.ServiceLocator.TryGet<WorkerMindService>(out WorkerMindService mindService))
                {
                    if (attacker.IsPlayerCharacter)
                    {
                        if (lethal)
                        {
                            mindService.RecordEvent(this, WorkerMindConstant.EVT_PLAYER_KILL,
                                MemoryValence.Negative, WorkerMindService.PlayerTargetName, 90f, "被玩家杀死了");
                            this.ShowMindBubble(WorkerInnerMonologue.GetEventThought(WorkerMindConstant.EVT_PLAYER_KILL, null));
                        }
                        else
                        {
                            mindService.RecordEvent(this, WorkerMindConstant.EVT_PLAYER_ATTACK,
                                MemoryValence.Negative, WorkerMindService.PlayerTargetName, 50f, "被玩家打了");
                        }
                    }
                    else if (attacker.IsWorkerCharacter)
                    {
                        mindService.RecordEvent(this, WorkerMindConstant.EVT_WORKER_ATTACK,
                            MemoryValence.Negative, attacker.name, 40f, $"被 {attacker.name} 打了");
                    }
                }
            }

            // 仅存活时切换状态（防止覆盖 Dead 状态）
            if (this.CharacterDataLAB.Hp > 0f)
            {
                if (this.Manager.CurrentStateType != AWorkerState.TypeEnum.Attack)
                {
                    this.Manager.ChangeState(AWorkerState.TypeEnum.Attack); // OnEnter 锁定 AttackTarget = LastAttacker
                }
                else
                {
                    // 被打刷新当前目标的锁定期（被打后继续攻击几秒不转头，与 Enemy 的"持续攻击
                    // 几秒"等效），锁定期过后被其他目标打才换。锁定基于"被打时刻"而非"进入攻击
                    // 时长"：Worker 攻击当前目标再久，被打后也先继续攻击几秒，不会一被打就转头
                    // （见 bug-fixes.md 2026-08-16）。
                    if (this.Manager.CurrentState is WorkerAttackState attackState)
                    {
                        attackState.OnHit();
                        if (attackState.CanSwitchTarget() && attacker != null && attacker != this.AttackTarget)
                        {
                            this.AttackTarget = attacker;
                            AWorkerTask.LogProviderThrottled(
                                $"{this.name}|TargetSwitch", 2f,
                                $"[StateDiag] {this.name} 换反击目标 → {attacker.name}（锁定期过后被打）",
                                LogManager.LogLevelEnum.Debug);
                        }
                    }

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

        /// <summary>
        /// 处理卡死：建造任务重试最多 3 次，否则记录失败点位并放弃任务。
        /// 由每秒位移检测（MovementStuckDetector）在 Move 状态下触发。
        /// </summary>
        public void HandleMovementStuck()
        {
            AWorker.WorkerData workerData = this.CharacterDataLAB as AWorker.WorkerData;

            // 建造任务：卡死触发时优先重试重新寻路，而非直接放弃。
            // 建造现场通常空间狭窄，碰撞频繁但并非真正阻塞。
            // 最大 3 次重试。
            const int maxRetries = 3;
            if (workerData?.Task != null && workerData.Task.TaskType == WorkerTaskType.Build)
            {
                workerData.BuildStuckRetryCount++;
                if (workerData.BuildStuckRetryCount < maxRetries)
                {
                    // [TaskDiag] 卡死路由：建造任务重试重新寻路，不放弃
                    AWorkerTask.LogProvider(
                        $"[TaskDiag] {this.name} 卡死→重试({workerData.BuildStuckRetryCount}/{maxRetries}) 任务={workerData.Task.TaskType} 目标=({this.Seek.TargetMap.x},{this.Seek.TargetMap.y})",
                        LogManager.LogLevelEnum.Debug);

                    // 重新寻路绕过阻塞，不放弃任务
                    this.Manager.ChangeState(AWorkerState.TypeEnum.Seek);
                    return;
                }

                workerData.BuildStuckRetryCount = 0;
            }

            // 放弃当前任务前记录失败点位，防止WorkerBrain立即重新选择同一目标
            Vector3Int currentTarget = this.Seek.TargetMap;
            if (currentTarget != Vector3Int.zero)
            {
                ASeek.RecordFail(currentTarget);
            }

            // 核心修复：放弃前让任务进入冷却（LastFailedTime + FailedCooldownSeconds=10s）。
            // 此前只 RecordFail 了寻路目标（邻居格）而未设置 Task.LastFailedTime，
            // 导致 IsInCooldown=false → GiveUpTask 回池 → 分配循环（TrySelectNearestAssignable）不跳过 →
            // 分配循环立即把同一任务重接回同一 Worker → "卡死→放弃→重接→再卡死"无限循环
            // （日志观测黄良/熊茂霖等 7 人各数百次 Stuck/放弃，Worker 卡在 PickUp/Build 不动）。
            if (workerData?.Task != null)
            {
                workerData.Task.LastFailedTime = UnityEngine.Time.time;

                // 任务自身目标也记入失败缓存：Seek.TargetMap 只是邻居格，
                // 决策层 ScanForResources/ScanForFood 用 IsRecentFail(任务目标) 过滤。
                // TargetMap 是引用类型，个别任务可能未设置，判空防 NRE。
                if (workerData.Task.TargetMap != null)
                {
                    Vector3Int taskTarget = Vector3IntLAB.ToVector3Int(workerData.Task.TargetMap);
                    if (taskTarget != Vector3Int.zero)
                    {
                        ASeek.RecordFail(taskTarget);
                    }
                }
            }

            // 救援传送：Worker 当前格不可通行（被新完成建筑/床碰撞体困住，可通行=False）时，
            // 即使任务进入冷却，Worker 仍被物理困在原地，无法执行任何任务。
            // 螺旋搜索最近可通行格并传送，解冻卡在碰撞体上的 Worker（黄良/苏茂/宋树/周刚豪）。
            this.TryRescueFromUnwalkableTile();

            // [TaskDiag] 卡死路由：已 RecordFail + 任务进入冷却，放弃当前任务让决策层避开阻塞点
            AWorkerTask.LogProvider(
                $"[TaskDiag] {this.name} 卡死→放弃(已RecordFail+冷却) 任务={workerData?.Task?.TaskType} 目标=({currentTarget.x},{currentTarget.y})",
                LogManager.LogLevelEnum.Debug);

            this.GiveUpTask(); // 放弃当前任务，让WorkerBrain做新决策避开阻塞点
        }

        /// <summary>
        /// 救援传送：若 Worker 当前所在格不可通行（被新完成建筑/床的碰撞体困住），
        /// 用螺旋搜索在附近找最近的可通行格并传送过去。
        /// 避免 Worker 卡在碰撞体上导致"站着不动"且无法执行任何任务。
        /// </summary>
        private void TryRescueFromUnwalkableTile()
        {
            Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(this.transform.position);
            if (ASeek.IsCanReach(posMap))
            {
                return; // 当前格可通行，无需救援
            }

            // 螺旋搜索：从内向外按 Chebyshev 距离层遍历，找最近的可行走格。
            // 半径 6 足够覆盖房间家具（床/仓库 3x2 块）附近的空地，且避免远距离瞬移。
            const int maxRadius = 6;
            for (int layer = 1; layer <= maxRadius; layer++)
            {
                for (int dx = -layer; dx <= layer; dx++)
                {
                    for (int dy = -layer; dy <= layer; dy++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != layer)
                        {
                            continue;
                        }

                        Vector3Int candidate = new Vector3Int(posMap.x + dx, posMap.y + dy, 0);
                        if (ASeek.IsCanReach(candidate))
                        {
                            this.transform.position = AWorkerTask.TileMapPositionProvider(candidate);
                            AWorkerTask.LogProvider(
                                $"[MoveDiag] {this.name} 卡死在不可通行格({posMap.x},{posMap.y}) → 救援传送至({candidate.x},{candidate.y})",
                                LogManager.LogLevelEnum.Warning);
                            return;
                        }
                    }
                }
            }

            // 附近全不可通行：兜底记录，不做传送（避免传送到远处不连贯位置）
            AWorkerTask.LogProvider(
                $"[MoveDiag] {this.name} 卡死在不可通行格({posMap.x},{posMap.y}) 但附近{maxRadius}格无可行走格，无法救援",
                LogManager.LogLevelEnum.Warning);
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
            /// 当前疲劳值（累积疲劳，越大越疲，初始 0；工作累积、睡眠降低）。
            /// </summary>
            public float CurTired = 0.0f;

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
            public int MaxResourceCount = 200;

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
            /// 上次睡眠任务因"无邻居位置"失败的时间（Time.time）。
            /// 用于决策冷却：失败后的一段时间内不再重复发起睡眠任务，
            /// 防止 worker 卡死时每帧创建任务→放弃的死循环刷屏。
            /// </summary>
            public float LastSleepFailTime = -999f;

            /// <summary>
            /// 上次拾取溢出且无物可存的时间（Time.time）。
            /// 决策层用其做冷却：冷却期内不再对同一目标反复创建拾取任务，
            /// 防止"溢出→放弃→又拾取→又溢出"的死循环。
            /// </summary>
            public float LastStorageOverflowTime = -999f;

            /// <summary>
            /// 上次仓库存取任务失败的时间（Time.time），用于决策冷却。
            /// </summary>
            public float LastStorageAccessFailTime = -999f;

            /// <summary>
            /// 人格数值 — 心情、事业心、勤奋、社交。
            /// 影响 Worker 自主决策行为和效率。
            /// </summary>
            public Domain.Worker.WorkerPersonality Personality = Domain.Worker.WorkerPersonality.Neutral;

            /// <summary>贪婪 — 对金钱/财富的渴望。0=淡泊, 100=财迷。影响赚钱类决策权重。</summary>
            public float Greed = 50f;

            /// <summary>懒惰 — 对劳作的抗拒。0=勤快, 100=躺平。影响漫游/摸鱼倾向。</summary>
            public float Laziness = 50f;

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

            /// <summary>房间外墙宽度（5-8）。0 表示尚未生成布局。</summary>
            public int HomeRoomWidth;

            /// <summary>房间外墙高度（5-8）。</summary>
            public int HomeRoomHeight;

            /// <summary>门所在边: 0=左 1=右 2=上 3=下。</summary>
            public int HomeDoorSide;

            /// <summary>门在该边的位置索引（0-based，不含角）。</summary>
            public int HomeDoorIndex;

            /// <summary>当前精气神值。</summary>
            public float CurSpirit = 100.0f;

            /// <summary>最大精气神值。</summary>
            public float MaxSpirit = 100.0f;

            /// <summary>当前压力值（劳动压迫，越大越压；工作累积、休息/睡觉/漫游/吃饭降低，初始 0）。</summary>
            public float CurStress = 0.0f;

            /// <summary>最大压力值。</summary>
            public float MaxStress = 100.0f;

            /// <summary>当前士气（长期向心力/生活满意度，越大越足；困苦下降、安好回升）。</summary>
            public float CurMorale = 100.0f;

            /// <summary>最大士气。</summary>
            public float MaxMorale = 100.0f;

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

            /// <summary>
            /// 各工种技能熟练度（0-100）— 练习增长，提升对应工种的工作速度。
            /// Key: WorkerTaskType, Value: 熟练度。仅核心工作类任务（采集/建造/搬运/种植/拆除/拾取/存取/悬赏）增长。
            /// </summary>
            public Dictionary<WorkerTaskType, float> SkillProficiencies;

            /// <summary>
            /// 生活技能累计经验 — 伐木/采矿/农耕用进废退，等级提升对应工作速度（见 LifeSkillRuleService）。
            /// Key: LifeSkillType, Value: 累计经验。读旧档可能为 null（BinaryFormatter 不跑构造函数），
            /// 使用前需 EnsureLifeSkills 兜底。
            /// </summary>
            public Dictionary<LAB2D.Enum.LifeSkillType, float> LifeSkillXp;

            /// <summary>
            /// 心智层数据（记忆/信念/怨恨/感恩/执念/漂移/关系）— 让 Worker 有自己的思想、未来不可控。
            /// 读旧档可能为 null（BinaryFormatter 不跑构造函数），需 WorkerMindData.Ensure 兜底。
            /// </summary>
            public Domain.Worker.WorkerMindData Mind;

            public WorkerData()
            {
                // 所有任务类型默认开启（opt-out 语义：只有玩家通过 UI 手动关闭的才会被写入 false）
                this.TaskToggle = new Dictionary<WorkerTaskType, bool>();
                for (WorkerTaskType t = 0; t < WorkerTaskType._Count; t++)
                {
                    this.TaskToggle[t] = true;
                }
                this.Personality = Domain.Worker.WorkerPersonality.Randomize();
                // 纯 C# 随机（原 UnityEngine.Random.Range 是 icall，裸 Mono 单测环境必炸；
                // 均匀 40-80 语义等价，项目无种子复现依赖）
                this.Greed = Domain.Worker.WorkerPersonality.NextFloat(40f, 80f);
                this.Laziness = Domain.Worker.WorkerPersonality.NextFloat(40f, 80f);
                this.Storage = new Dictionary<int, ResourceInfo>();
                this.CarriedResources = new Dictionary<int, ResourceInfo>();
                this.SkillProficiencies = new Dictionary<WorkerTaskType, float>();
                this.LifeSkillXp = new Dictionary<LAB2D.Enum.LifeSkillType, float>();
                this.Mind = new Domain.Worker.WorkerMindData();
            }

            /// <summary>
            /// 生活技能字典兜底：读档后为 null 时补建（BinaryFormatter 不跑构造函数），幂等。
            /// </summary>
            public void EnsureLifeSkills()
            {
                if (this.LifeSkillXp == null)
                {
                    this.LifeSkillXp = new Dictionary<LAB2D.Enum.LifeSkillType, float>();
                }
            }
        }
    }
}
