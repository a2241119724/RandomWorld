namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Worker
    /// </summary>
    public class Worker : Character
    {
        /// <summary>
        /// 最大疲劳值
        /// </summary>
        public const float MaxTired = 100.0f;

        /// <summary>
        /// 疲劳值阈值
        /// </summary>
        public const float ThresholdTired = 10.0f;

        /// <summary>
        /// 最大饥饿值
        /// </summary>
        public const float MaxHungry = 100.0f;

        /// <summary>
        /// 饥饿值阈值
        /// </summary>
        public const float ThresholdHungry = 10.0f;

        /// <summary>
        /// 锁，防止多个Worker同时寻路
        /// </summary>
        public static Lock SeekLock = new ();

        /// <summary>
        /// 角色装备
        /// </summary>
        public WearData WearData;

        /// <summary>
        /// Worker的床
        /// </summary>
        public BedItem BedItem;

        private Dictionary<int, ResourceInfo> resourceInfos; // 携带的资源
        private Slider progress;
        private Text nameUI;
        private CharacterStatusUI statusBar; // 记录实例化血条

        /// <summary>
        /// 状态管理器
        /// </summary>
        [HideInInspector]
        public WorkerStateManager<ICharacterState, WorkerState.WorkerStateTypeEnum, Worker> Manager { get; private set; }

        /// <summary>
        /// Worker状态
        /// </summary>
        public Text WorkerStateText { get; set; }

        /// <summary>
        /// 是否需要做任务的开关
        /// 是否开启做该任务类型的开关(toogle的顺序与TaskType的顺序相关)
        /// </summary>
        public bool[] TaskToggle { get; set; }

        /// <summary>
        /// 当前疲劳值
        /// </summary>
        public float CurTired { get; set; } = 100.0f;

        /// <summary>
        /// 当前饥饿值
        /// </summary>
        public float CurHungry { get; set; } = 100.0f;

        /// <summary>
        /// 最大持有资源数量
        /// </summary>
        public int MaxResourceCount { get; set; } = 30;

        /// <summary>
        /// 寻路
        /// </summary>
        public ASeek Seek { get; set; }

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            this.Manager = new WorkerStateManager<ICharacterState, WorkerState.WorkerStateTypeEnum, Worker>(this);
            this.CharacterDataLAB.MaxHp = this.CharacterDataLAB.Hp = 100;
            this.nameUI = this.transform.Find("Name").GetComponent<Text>();
            this.WorkerStateText = this.transform.Find("State").GetComponent<Text>();
            this.progress = this.transform.Find("Progress").GetComponent<Slider>();
            this.progress.gameObject.SetActive(false);

            // 设置默认可接受任务类型
            this.TaskToggle = new bool[10];
            this.TaskToggle[(int)WorkerTask.WorkerTaskTypeEnum.Eat] = true;
            this.TaskToggle[(int)WorkerTask.WorkerTaskTypeEnum.Wear] = true;
            this.TaskToggle[(int)WorkerTask.WorkerTaskTypeEnum.Carry] = true;
            this.resourceInfos = new Dictionary<int, ResourceInfo>();
            this.statusBar = this.transform.Find("Hp").GetComponent<CharacterStatusUI>();
            if (this.statusBar == null)
            {
                LogManager.Instance.Log("statusBar Not Found!!!", LogManager.LogLevel.Error);
                return;
            }

            this.WearData = new WearData();
            ThreadPool.SetMaxThreads(5, 5);
            this.Seek = new AStar(this);
        }

        /// <inheritdoc/>
        public override void Start()
        {
            base.Start();
            this.nameUI.text = this.name;
            this.statusBar.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
        }

        public void Update()
        {
            this.transform.rotation = Quaternion.identity;

            // 执行当前状态的函数
            this.Manager.CurrentState.OnUpdate();
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

        /// <inheritdoc/>
        public override string ToString()
        {
            string resources = string.Empty;
            foreach (KeyValuePair<int, ResourceInfo> resource in this.resourceInfos)
            {
                resources += resource.Key + ":" + resource.Value.Count + "\n";
            }

            return base.ToString() +
                $"Hungry:{this.CurHungry}\n" +
                $"TargetMap:{this.Seek.TargetMap}\n" +
                resources;
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
                this.resourceInfos.Add(resourceInfo.Id, Tool.DeepCopyByBinary(resourceInfo));
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
                    LogManager.Instance.Log("自身资源不够，仍然建造成功，错误", LogManager.LogLevel.Error);
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
                LogManager.Instance.Log("自身资源不够，仍然建造成功，错误", LogManager.LogLevel.Error);
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
            WorkerTaskManager.Instance.GiveUpTask(this.Manager.Task);
            this.Manager.Task = null;
            this.Manager.ChangeState(WorkerState.WorkerStateTypeEnum.Seek);
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
                    remaining.Add(need.Key, Tool.DeepCopyByBinary(need.Value));
                }
            }

            return remaining;
        }

        /// <summary>
        /// 掉血
        /// </summary>
        /// <param name="hp">所掉的血量</param>
        public override void ReduceHp(float hp)
        {
            if (hp <= 0)
            {
                LogManager.Instance.Log("Hp can't less than zero!!!", LogManager.LogLevel.Error);
                return;
            }

            base.ReduceHp(hp);
            this.statusBar.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
            this.Manager.ChangeState(WorkerState.WorkerStateTypeEnum.Attack);
        }

        /// <inheritdoc/>
        protected override void Death()
        {
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            this.checkBug.AddColliderCount(DateTime.Now.Ticks);
            if (this.checkBug.IsBug(this.name, 100))
            {
                this.Manager.ChangeState(WorkerState.WorkerStateTypeEnum.Seek);
            }
        }
    }

    /// <summary>
    /// Byte的Vector2
    /// </summary>
    public class Vector2SByte
    {
        /// <summary>
        /// X 坐标
        /// </summary>
        public sbyte X;

        /// <summary>
        /// Y 坐标
        /// </summary>
        public sbyte Y;

        public Vector2SByte(sbyte x, sbyte y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    /// <summary>
    /// 角色装备数据
    /// </summary>
    public class WearData
    {
        /// <summary>
        /// 携带的武器
        /// </summary>
        public Weapon Weapon;

        /// <summary>
        /// 身上携带的装备
        /// </summary>
        public Dictionary<Equipment.EquipType, Equipment> Equipments;

        public WearData()
        {
            this.Equipments = new Dictionary<Equipment.EquipType, Equipment>();
        }

        /// <summary>
        /// 添加装备
        /// </summary>
        /// <param name="equipment">装备</param>
        /// <param name="posMap">位置</param>
        public void AddEquipment(Equipment equipment, Vector3Int posMap)
        {
            if (this.Equipments.ContainsKey(equipment.EquipTypeValue))
            {
                // 交换装备
                Equipment equipment1 = this.Equipments[equipment.EquipTypeValue];
                ItemMap.Instance.PutDownToInventory(posMap, ResourceManager.Instance.GetAsset(equipment.ToString()), new ResourceInfo(equipment.Id, 1));
                this.Equipments[equipment.EquipTypeValue] = equipment1;
            }
            else
            {
                this.Equipments.Add(equipment.EquipTypeValue, equipment);
            }
        }
    }
}