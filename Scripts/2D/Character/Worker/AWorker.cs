namespace LAB2D.Character.Worker
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Character.Worker.State;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core.Seek;
    using LAB2D.Item;
    using LAB2D.Item.Build.Furniture.Bed;
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
                InventoryManager.Instance.DeleteWorkerPre(worker);
                WorkerTaskManager.Instance.GiveUpTask(task);
            };

        /// <summary>
        /// Worker 死亡回调 — 从管理器移除并记录统计数据。
        /// 默认实现访问 NetworkConnect.Instance / WorkerManager.Instance / WorkerEfficiencyTracker.Instance。
        /// </summary>
        public static System.Action<AWorker> DeathProvider { get; set; }
            = (worker) =>
            {
                if (!AWorkerTask.NetworkIsOnlineProvider() || PhotonNetwork.IsMasterClient)
                {
                    WorkerManager.Instance.Remove(worker);
                }

                WorkerEfficiencyTracker.Instance.RecordWorkerDeath(worker);
            };

        /// <summary>
        /// 日志提供者 — Worker 相关的错误/警告日志。
        /// 默认实现访问 LogManager.Instance。
        /// </summary>
        public static System.Action<string, LogManager.LogLevelEnum> LogProvider { get; set; }
            = (msg, level) => LogManager.Instance.Log(msg, level);

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

                Vector3Int pos = AWorkerTask.AvailablePositionProvider(
                AWorkerTask.TileMapWorldToMapProvider(this.transform.position), 3, true);
                if (pos == default)
                {
                    continue;
                }

                AWorkerTask.ItemMapProvider().PutDownToDrop(pos, AWorkerTask.ItemFactoryByIdProvider(resource.Key).Tile, resource.Value);
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
            this.checkBug.AddColliderCount(DateTime.Now.Ticks);
            if (this.checkBug.IsBug(this.name, 1000))
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
            /// 是否开启做该任务类型的开关(toogle的顺序与TaskType的顺序相关)
            /// </summary>
            public bool[] TaskToggle;

            /// <summary>
            /// 当前状态
            /// </summary>
            public AWorkerState.TypeEnum CurrentStateType;

            /// <summary>
            /// 任务
            /// </summary>
            public AWorkerTask Task;

            public WorkerData()
            {
                // 设置默认可接受任务类型
                this.TaskToggle = new bool[10];
                this.TaskToggle[(int)WorkerTaskType.Eat] = true;
                this.TaskToggle[(int)WorkerTaskType.Wear] = true;
                this.TaskToggle[(int)WorkerTaskType.Carry] = true;
                this.TaskToggle[(int)WorkerTaskType.Gather] = true;
                this.TaskToggle[(int)WorkerTaskType.Exercise] = true;
            }
        }
    }
}
