namespace LAB2D.Character
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Item.Backpack.Equipment;
    using LAB2D.Item.Backpack.Equipment.Weapon;
    using LAB2D.Network;
    using LAB2D.Serializable;
    using LAB2D.UnityAdapter;
    using LAB2D.Domain.Character;
    using LAB2D.Domain.Common;
    using System;
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;
    using PlayerCharacter = LAB2D.Character.Player.Player;

    /// <summary>
    /// 角色基类。
    /// 继承自 MonoBehaviourPun 以保证向后兼容，同时通过 INetworkView 解耦网络访问。
    /// 新增网络访问代码应使用 NetworkView 而非直接访问 pv/photonView。
    /// 完全移除 MonoBehaviourPun 依赖的后续重构待网络层完全适配后执行。
    /// </summary>
    public abstract class Character : MonoBehaviourPun
    {
        /// <summary>
        /// 网络视图抽象 — 在线时包装 PhotonView，离线时为 NullNetworkView。
        /// 新代码应使用此属性而非直接访问 pv/photonView。
        /// </summary>
        public INetworkView NetworkView { get; private set; }

        /// <summary>
        /// 移动速度
        /// </summary>
        public float MoveSpeed = 2.5f;

        /// <summary>
        /// 攻击的层级
        /// </summary>
        public LayerMask AttackLayers;

        /// <summary>
        /// 攻击的标签
        /// </summary>
        public List<string> AttackTags;

        /// <summary>
        /// 闪烁
        /// </summary>
        protected SpriteRenderer spriteRenderer;

        /// <summary>
        /// 碰撞 Bug 检测器。
        /// </summary>
        protected CollisionBugDetector collisionBugDetector;

        /// <summary>
        /// 角色基础属性
        /// </summary>
        protected Attribute basicAttribute;

        protected Color originalColor; // 原来的自身颜色
        protected IGameTime gameTime = new UnityGameTime();

        /// <summary>
        /// 领域安全的 DeltaTime，供状态机等子模块使用，不直接依赖 UnityEngine.Time。
        /// </summary>
        public float DeltaTime
        {
            get { return this.gameTime.DeltaTime; }
        }
        private readonly DamageCalculator damageCalculator = new DamageCalculator();
        private readonly LevelProgressionService levelProgressionService = new LevelProgressionService();
        protected CharacterHealthComponent healthComponent;

        /// <summary>
        /// 受击闪烁表现提供者 — 在角色受到伤害时触发视觉反馈（红色闪烁）。
        /// 默认实现操作 SpriteRenderer 颜色并通过 Invoke 延迟恢复。
        /// 可在测试中替换为无操作桩，或替换为自定义表现效果。
        /// </summary>
        public static System.Action<Character> DamageFlashProvider { get; set; }
            = (target) =>
            {
                if (target == null || target.spriteRenderer == null)
                {
                    return;
                }

                target.spriteRenderer.color = Color.red;
                target.Invoke(nameof(ResetColor), 0.2f);
            };

        /// <summary>
        /// 移动速度提供者 — 获取角色当前移动速度。
        /// 默认实现返回实例字段 MoveSpeed。
        /// 可在测试中替换以模拟不同速度配置。
        /// </summary>
        public static System.Func<Character, float> MoveSpeedProvider { get; set; }
            = (c) => c.MoveSpeed;

        /// <summary>
        /// 当前装备的武器物体
        /// </summary>
        public GameObject Weapon { get; set; }

        /// <summary>
        /// 最近攻击者
        /// </summary>
        public Character LastAttacker { get; set; }

        /// <summary>
        /// 角色数据
        /// </summary>
        public CharacterData CharacterDataLAB { get; set; }

        public virtual void Awake()
        {
            this.name = this.GetType().Name;

            this.NetworkView = AWorkerTask.NetworkIsOnlineProvider()
                ? new PunNetworkViewAdapter(this.pv)
                : ServiceLocator.Get<OfflineNetworkView>();

            GameObject characterRoot = GameObject.FindGameObjectWithTag("CharacterRoot");
            if (characterRoot != null)
            {
                this.transform.SetParent(characterRoot.transform);
            }
            else
            {
                AWorkerTask.LogProvider("CharacterRoot GameObject not found in scene, character will be placed at root", LogManager.LogLevelEnum.Error);
            }

            this.collisionBugDetector = new CollisionBugDetector();
        }

        public virtual void Start()
        {
            this.spriteRenderer = this.GetComponent<SpriteRenderer>();
            if (this.spriteRenderer == null)
            {
                AWorkerTask.LogProvider("renderer Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.originalColor = this.spriteRenderer.color;
            this.healthComponent = new CharacterHealthComponent(this.damageCalculator, this.levelProgressionService);
        }

        /// <summary>
        /// 角色扣血 — 委托给 CharacterHealthComponent 处理伤害计算。
        /// 发布 CharacterDamagedEvent 供 UI 层消费，不再直接操作 UI。
        /// </summary>
        /// <param name="hp">血量</param>
        /// <param name="attacker">攻击者</param>
        /// <param name="isCRT">是否暴击</param>
        public virtual void ReduceHp(float hp, Character attacker, bool isCRT = false)
        {
            if (hp <= 0)
            {
                return;
            }

            float capturedDamage = hp;
            bool capturedCombo = false;
            CharacterHealthResult healthResult = this.healthComponent.ApplyDamage(
                this, hp, attacker, isCRT,
                (target, finalHp, crit, isCombo) =>
                {
                    capturedDamage = finalHp;
                    capturedCombo = isCombo;
                    DamageFlashProvider(target);
                });

            EventBusPublishProvider(new CharacterDamagedEvent
            {
                TargetId = this.CharacterDataLAB.Id,
                AttackerId = attacker?.CharacterDataLAB?.Id ?? 0,
                Damage = capturedDamage,
                IsCritical = isCRT,
                IsCombo = capturedCombo,
                RemainingHp = this.CharacterDataLAB.Hp,
                WorldPosX = this.transform.position.x,
                WorldPosY = this.transform.position.y,
            });

            if (healthResult.IsDead)
            {
                this.Death();
            }
        }

        /// <summary>
        /// 升级提示提供者 — 当角色升级时显示视觉提示。
        /// 默认实现访问 ServiceLocator.Get&lt;GlobalInit&gt;().ShowTip。
        /// 可替换为测试桩或自定义实现。
        /// </summary>
        public static System.Action<string> LevelUpTipProvider { get; set; }
            = (tip) => ServiceLocator.Get<GlobalInit>().ShowTip(tip);

        internal static System.Action<IGameEvent> EventBusPublishProvider { get; set; }
            = (e) => ServiceLocator.Get<EventBus>().PublishInternal(e);

        /// <inheritdoc/>
        public override string ToString()
        {
            Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(this.transform.position);
            return $"{this.GetType().Name}:{this.name}\n" +
                $"血量: {this.CharacterDataLAB.Hp:F0}/{this.CharacterDataLAB.MaxHp:F0}\n" +
                $"蓝量: {this.CharacterDataLAB.Mp}/{this.CharacterDataLAB.MaxMp}\n" +
                $"速度: {this.MoveSpeed}\n" +
                $"位置: ({posMap.x},{posMap.y})\n" +
                $"物理攻击力: {this.CharacterDataLAB.ATN:F1}\n" +
                $"魔法攻击力: {this.CharacterDataLAB.INT:F1}\n" +
                $"物理防御力: {this.CharacterDataLAB.DEF:F1}\n" +
                $"魔法防御力: {this.CharacterDataLAB.RES:F1}\n" +
                $"暴击率: {this.CharacterDataLAB.CRT:P1}\n" +
                $"暴击伤害: {this.CharacterDataLAB.CSD:F1}\n" +
                $"速度/回避: {this.CharacterDataLAB.SPD:F1}\n" +
                $"命中/连击: {this.CharacterDataLAB.HIT:F1}\n" +
                $"等级: {this.CharacterDataLAB.Level}\n" +
                $"经验值: {this.CharacterDataLAB.CurExperience}/{this.CharacterDataLAB.MaxExperience}\n";
        }

        /// <summary>
        /// 增加经验值 — 委托给 CharacterHealthComponent。
        /// </summary>
        /// <param name="experience">经验值.</param>
        public virtual void AddExperienceValue(int experience)
        {
            LevelProgressionResult result = this.healthComponent.AddExperience(this.CharacterDataLAB, experience);

            if (result.LeveledUp)
            {
                LevelUpTipProvider.Invoke("UP " + this.CharacterDataLAB.Level);
                this.CharacterDataLAB.ComputeAttribute(this.basicAttribute);
            }
        }

        /// <summary>
        /// 攻击.
        /// </summary>
        public abstract void Attack();

        /// <summary>
        /// 重置状态中的属性
        /// </summary>
        public virtual void ResetState()
        {
        }

        /// <summary>
        /// 敌人死亡
        /// </summary>
        protected virtual void Death()
        {
        }

        /// <summary>
        /// 恢复颜色 — 由 DamageFlashProvider 的默认实现通过 Invoke 延迟调用。
        /// 保留以兼容旧代码和子类直接调用；新代码应通过 DamageFlashProvider 控制受击表现。
        /// </summary>
        protected void ResetColor()
        {
            if (this.spriteRenderer != null)
            {
                this.spriteRenderer.color = this.originalColor;
            }
        }

        /// <summary>
        /// 角色数据
        /// </summary>
        [Serializable]
        public class CharacterData : Attribute
        {
            private static readonly DamageCalculator DamageCalculator = new DamageCalculator();
            private static readonly AttributeCalculationService attributeCalcService = new AttributeCalculationService();

            /// <summary>
            /// 装备交换时的卸装回调 — 将旧装备放回地图。
            /// 默认实现访问 ItemMap.Instance / ResourceManager.Instance / ItemDataManager.Instance。
            /// 可替换为测试桩或自定义实现。
            /// </summary>
            public static System.Action<AEquipment, Vector3Int> EquipmentSwapDropProvider { get; set; }
                = (oldEquipment, posMap) =>
                {
                    Core.ServiceLocator.Get<ItemMap>().PutDownToInventory(
                        posMap,
                        Core.ServiceLocator.Get<ResourceManager>().GetAsset(
                            Core.ServiceLocator.Get<ItemDataManager>().GetById(oldEquipment.Id).EnName),
                        new ResourceInfo(oldEquipment.Id, 1));
                };

            /// <summary>
            /// Id
            /// </summary>
            public long Id = 0;

            /// <summary>
            /// 血量
            /// </summary>
            public float Hp = 100;

            /// <summary>
            /// 最大血量
            /// </summary>
            public float MaxHp = 100;

            /// <summary>
            /// 玩家当前经验值
            /// </summary>
            public int CurExperience = 0;

            /// <summary>
            /// 玩家当前等级最大经验值
            /// </summary>
            public int MaxExperience = 4;

            /// <summary>
            /// 当前等级
            /// </summary>
            public int Level = 1;

            /// <summary>
            /// 玩家蓝量
            /// </summary>
            public int Mp = 100;

            /// <summary>
            /// 玩家最大蓝量
            /// </summary>
            public int MaxMp = 100;

            /// <summary>
            /// 位置
            /// </summary>
            public Vector3LAB Pos;

            /// <summary>
            /// 生成的Id
            /// </summary>
            private static long generateId = 0;

            /// <summary>
            /// 生成的寻路Id
            /// </summary>
            private int generateSeekId = 0;

            /// <summary>
            /// 寻路Id
            /// </summary>
            private int seekId = 0;

            /// <summary>
            /// 携带的武器
            /// </summary>
            private AWeapon weapon;

            /// <summary>
            /// 身上携带的装备
            /// </summary>
            private Dictionary<AEquipment.EquipTypeEnum, AEquipment> equipments;

            [NonSerialized]
            private Character character;

            public CharacterData()
            {
                this.equipments = new Dictionary<AEquipment.EquipTypeEnum, AEquipment>();
                this.Id = CharacterData.generateId++;
            }

            public Character Character
            {
                get
                {
                    return this.character;
                }

                set
                {
                    this.character = value;
                    if (this.character != null)
                    {
                        this.ComputeAttribute(this.character.basicAttribute);
                    }
                }
            }

            public AWeapon Weapon
            {
                get
                {
                    return this.weapon;
                }

                set
                {
                    this.weapon = value;
                    if (this.character != null)
                    {
                        this.ComputeAttribute(this.character.basicAttribute);
                    }
                }
            }

            public string SeekId
            {
                get
                {
                    return this.Id + ":" + this.seekId;
                }
            }

            public Dictionary<AEquipment.EquipTypeEnum, AEquipment> GetEquipments()
            {
                return this.equipments;
            }

            /// <summary>
            /// 获取伤害
            /// </summary>
            /// <param name="isCRT">是否暴击</param>
            /// <returns>伤害值</returns>
            public float GetDamage(bool isCRT)
            {
                return DamageCalculator.GetOutgoingDamage(this.ATN, this.CSD, isCRT);
            }

            /// <summary>
            /// 添加装备
            /// </summary>
            /// <param name="equipment">装备</param>
            /// <param name="posMap">位置</param>
            public void AddEquipment(AEquipment equipment, Vector3Int posMap)
            {
                if (this.equipments.ContainsKey(equipment.Type))
                {
                    // 交换装备：卸下旧装备放入地图，装备新装备
                    AEquipment oldEquipment = this.equipments[equipment.Type];
                    EquipmentSwapDropProvider(oldEquipment, posMap);
                    this.equipments[equipment.Type] = equipment;
                }
                else
                {
                    this.equipments.Add(equipment.Type, equipment);
                }

                if (this.character != null)
                {
                    this.ComputeAttribute(this.character.basicAttribute);
                }
            }

            /// <summary>
            /// 计算总属性 — 委托给 Domain/Character/AttributeCalculationService。
            /// </summary>
            /// <param name="basicAttribute">角色基础属性（由 Character.basicAttribute 提供）。</param>
            public void ComputeAttribute(Attribute basicAttribute)
            {
                bool isPlayer = this is PlayerCharacter.PlayerData;
                BattleStats baseStats = ConvertAttributeToBattleStats(basicAttribute);
                BattleStats? weaponBStats = null;
                if (this.weapon != null)
                {
                    weaponBStats = ConvertAttributeToBattleStats(this.weapon.Attribute);
                }

                System.Collections.Generic.List<BattleStats> equipmentBStats = null;
                if (this.equipments != null && this.equipments.Count > 0)
                {
                    equipmentBStats = new System.Collections.Generic.List<BattleStats>();
                    foreach (var item in this.equipments)
                    {
                        equipmentBStats.Add(ConvertAttributeToBattleStats(item.Value.Attribute));
                    }
                }

                BattleStats result = attributeCalcService.ComputeFinalStats(
                    baseStats,
                    this.Level,
                    isPlayer,
                    weaponBStats,
                    equipmentBStats);

                this.ATN = result.ATN;
                this.INT = result.INT;
                this.DEF = result.DEF;
                this.RES = result.RES;
                this.CRT = result.CRT;
                this.CSD = result.CSD;
                this.SPD = result.SPD;
                this.HIT = result.HIT;
            }

            private static BattleStats ConvertAttributeToBattleStats(Attribute attr)
            {
                if (attr == null)
                {
                    return BattleStats.Zero;
                }

                return new BattleStats(attr.ATN, attr.INT, attr.DEF, attr.RES, attr.CRT, attr.CSD, attr.SPD, attr.HIT);
            }

            /// <summary>
            /// 生成寻路Id
            /// </summary>
            /// <returns>寻路Id</returns>
            public string GenerateSeekId()
            {
                this.seekId = ++this.generateSeekId;
                return this.SeekId;
            }
        }

        [Serializable]
        public class Attribute
        {
            /// <summary>
            /// 物理攻击力
            /// </summary>
            public float ATN;

            /// <summary>
            /// 魔法攻击力
            /// </summary>
            public float INT;

            /// <summary>
            /// 物理防御力
            /// </summary>
            public float DEF;

            /// <summary>
            /// 魔法防御力
            /// </summary>
            public float RES;

            /// <summary>
            /// 暴击率
            /// </summary>
            public float CRT;

            /// <summary>
            /// 暴击伤害
            /// </summary>
            public float CSD;

            /// <summary>
            /// 速度，回避物理攻击之类的
            /// </summary>
            public float SPD;

            /// <summary>
            /// 命中率或者连击之类的
            /// </summary>
            public float HIT;

            public Attribute()
            {
                this.ATN = 0;
                this.INT = 0;
                this.DEF = 0;
                this.RES = 0;
                this.CRT = 0;
                this.CSD = 0;
                this.SPD = 0;
                this.HIT = 0;
            }

            public Attribute(float atn, float int_, float def, float res, float crt, float csd, float spd, float hit)
            {
                this.ATN = atn;
                this.INT = int_;
                this.DEF = def;
                this.RES = res;
                this.CRT = crt;
                this.CSD = csd;
                this.SPD = spd;
                this.HIT = hit;
            }
        }

    }
}
