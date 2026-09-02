namespace LAB2D.Character
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Render;
    using LAB2D.Item.Backpack.Equipment;
    using LAB2D.Item.Backpack.Equipment.Weapon;
    using LAB2D.Network;
    using LAB2D.Serializable;
    using LAB2D.UnityAdapter;
    using LAB2D.Domain.Character;
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Common;
    using System;
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;

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
        /// 是否为玩家角色 — 用于属性计算时区分玩家和敌人/Worker 的等级加成逻辑。
        /// Player 子类重写为返回 true；其他子类保持默认 false。
        /// 消除 CharacterData.ComputeAttribute 中 <c>this is PlayerData</c> 的反向类型检查。
        /// </summary>
        public virtual bool IsPlayerCharacter => false;

        /// <summary>
        /// Worker 子类重写为返回 true；用于等级属性加成（每级 +5%）。
        /// </summary>
        public virtual bool IsWorkerCharacter => false;

        /// <summary>
        /// 移动速度
        /// </summary>
        public float MoveSpeed = 6f;

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
        /// 获取角色在当前环境下的有效移动速度（含地形等被动修正，不含主动技能/跑步）。
        /// 子类可重写以叠加天气、波次奖励等额外修正。
        /// </summary>
        public virtual float GetEffectiveMoveSpeed()
        {
            float terrainMultiplier = 1.0f;
            try
            {
                terrainMultiplier = ServiceLocator.Get<ITerrainEffectService>().GetMoveSpeedMultiplier(this);
            }
            catch
            {
                // 服务未注册时降级为基础速度
            }

            return this.MoveSpeed * terrainMultiplier;
        }

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

            this.NetworkView = Core.GameServices.NetworkIsOnlineProvider()
                ? new PunNetworkViewAdapter(this.pv)
                : ServiceLocator.Get<OfflineNetworkView>();

            CharacterRootParentProvider(this);

            this.collisionBugDetector = new CollisionBugDetector();
        }

        /// <summary>
        /// 头顶 UI 提升到的排序层（项目最高层，保证 UI 永远在世界物体之上）。
        /// </summary>
        private const string HeadUiSortingLayer = "Highest";

        /// <summary>
        /// 头顶 UI 排序层兜底：HeadUI（单 WorldSpace Canvas，收拢 Name/State/Progress/Hp/Dialog）
        /// 已内置于各角色 prefab，此处仅做防御——prefab 漏设排序层时提升到项目最高层，
        /// 避免 UI 落在 Default 层被 Character 层的树/建筑（WorldYSortManager 动态 sortingOrder）盖住。
        /// 运行时创建/迁移逻辑已随 prefab 化删除。
        /// </summary>
        private void EnsureHeadUiSorting()
        {
            Canvas[] canvases = this.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas canvas in canvases)
            {
                if (canvas.isRootCanvas && canvas.sortingLayerName != HeadUiSortingLayer)
                {
                    canvas.sortingLayerName = HeadUiSortingLayer;
                    AWorkerTask.LogProvider($"[UIDiag] {this.name} Canvas '{canvas.name}' 排序层提升至 {HeadUiSortingLayer}", LogManager.LogLevelEnum.Debug);
                }
            }
        }

        /// <summary>
        /// 头顶 UI 子节点查找：HeadUI 已内置于 prefab，统一从 HeadUI 下取；
        /// 兜底旧层级（直接子节点）以兼容漏改的 prefab 变体。
        /// </summary>
        protected Transform FindHeadChild(string name)
        {
            return this.transform.Find("HeadUI/" + name) ?? this.transform.Find(name);
        }

        public virtual void Start()
        {
            this.EnsureHeadUiSorting();
            SpriteRendererSetupProvider(this);
            if (this.spriteRenderer == null)
            {
                return;
            }

            YSortRegisterProvider(this);

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

            GameVector2 worldPos = WorldPositionProvider(this);
            EventBusPublishProvider(new CharacterDamagedEvent
            {
                TargetId = this.CharacterDataLAB.Id,
                AttackerId = attacker?.CharacterDataLAB?.Id ?? 0,
                Damage = capturedDamage,
                IsCritical = isCRT,
                IsCombo = capturedCombo,
                RemainingHp = this.CharacterDataLAB.Hp,
                WorldPosX = worldPos.X,
                WorldPosY = worldPos.Y,
            });

            // 好感度：Player 击伤敌方 → 附近 Worker 对玩家好感上升（启发式，30s 冷却/Worker）
            if (attacker != null && attacker.IsPlayerCharacter && !this.IsPlayerCharacter && !this.IsWorkerCharacter)
            {
                Core.ServiceLocator.Get<FavorabilityManager>()?.NotifyPlayerHelpsNearby(worldPos.X, worldPos.Y);
            }

            if (healthResult.IsDead)
            {
                this.Death();
            }
        }

        /// <summary>
        /// 成长系统触发的属性重算入口（境界突破/功法激活等）—
        /// 用当前基础属性重算，成长源（词条/境界永久加成等）一并生效。
        /// </summary>
        public void RecomputeGrowthAttributes()
        {
            if (this.CharacterDataLAB != null)
            {
                this.CharacterDataLAB.ComputeAttribute(this.basicAttribute, this.IsPlayerCharacter, this.IsWorkerCharacter);
            }
        }

        /// <summary>
        /// 回血统一入口（词条吸血等成长系统使用）— 默认只改数据并钳制上限；
        /// Player override 额外刷新 HUD。
        /// </summary>
        /// <param name="hp">回复量</param>
        public virtual void Heal(float hp)
        {
            if (hp <= 0 || this.CharacterDataLAB == null)
            {
                return;
            }

            if (this.CharacterDataLAB.Hp < this.CharacterDataLAB.MaxHp)
            {
                this.CharacterDataLAB.Hp = System.Math.Min(
                    this.CharacterDataLAB.Hp + hp,
                    this.CharacterDataLAB.MaxHp);
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

        /// <summary>
        /// 世界坐标提供者 — 获取角色当前世界位置。
        /// 默认实现访问 Transform.position；可在测试中替换为固定坐标桩。
        /// </summary>
        public static System.Func<Character, GameVector2> WorldPositionProvider { get; set; }
            = (c) => new GameVector2(c.transform.position.x, c.transform.position.y);

        // --- Unity 组件初始化 Provider（可替换为测试桩，隔离 UnityEngine 依赖） ---

        /// <summary>
        /// CharacterRoot 父节点设置提供者 — 将角色挂载到场景中的 CharacterRoot 节点下。
        /// 默认实现使用 GameObject.FindGameObjectWithTag("CharacterRoot")。
        /// 可在测试中替换为无操作桩。
        /// </summary>
        internal static System.Action<Character> CharacterRootParentProvider { get; set; }
            = (c) =>
            {
                if (c == null)
                {
                    return;
                }

                GameObject characterRoot = GameObject.FindGameObjectWithTag("CharacterRoot");
                if (characterRoot != null)
                {
                    c.transform.SetParent(characterRoot.transform);
                }
                else
                {
                    AWorkerTask.LogProvider(
                        "CharacterRoot GameObject not found in scene, character will be placed at root",
                        LogManager.LogLevelEnum.Error);
                }
            };

        /// <summary>
        /// SpriteRenderer 初始化提供者 — 获取角色身上的 SpriteRenderer 组件并捕获原始颜色。
        /// 默认实现使用 GetComponent&lt;SpriteRenderer&gt;() + 记录 .color。
        /// 可在测试中替换。
        /// </summary>
        internal static System.Action<Character> SpriteRendererSetupProvider { get; set; }
            = (c) =>
            {
                if (c == null)
                {
                    return;
                }

                c.spriteRenderer = c.GetComponent<SpriteRenderer>();
                if (c.spriteRenderer == null)
                {
                    AWorkerTask.LogProvider("renderer Not Found!!!", LogManager.LogLevelEnum.Error);
                    return;
                }

                c.originalColor = c.spriteRenderer.color;
            };

        /// <summary>
        /// y 排序注册提供者 — 将角色 SpriteRenderer 注册到全局 y 排序器（按视觉底端 y 分配 order）。
        /// 默认实现调用 WorldYSortManager.Ensure().Register；可在测试中替换为无操作桩。
        /// </summary>
        internal static System.Action<Character> YSortRegisterProvider { get; set; }
            = (c) =>
            {
                if (c == null || c.spriteRenderer == null)
                {
                    return;
                }

                WorldYSortManager.Ensure().Register(c.spriteRenderer);
            };

        /// <inheritdoc/>
        public override string ToString()
        {
            Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(this.transform.position);
            return $"{this.GetType().Name}:{this.name}\n" +
                $"血量: {this.CharacterDataLAB.Hp:F0}/{this.CharacterDataLAB.MaxHp:F0}\n" +
                $"蓝量: {this.CharacterDataLAB.Mp}/{this.CharacterDataLAB.MaxMp}\n" +
                $"速度: {this.GetEffectiveMoveSpeed():F1}\n" +
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
                this.CharacterDataLAB.ComputeAttribute(this.basicAttribute, this.IsPlayerCharacter, this.IsWorkerCharacter);
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
                            Core.ServiceLocator.Get<ItemDataManager>().GetById(oldEquipment.Id).Name),
                        new ResourceInfo(oldEquipment.Id, 1));
                };

            /// <summary>
            /// Id
            /// </summary>
            public long Id = 0;

            /// <summary>
            /// 角色名称（持久化，用于跨存档识别）
            /// </summary>
            public string Name;

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

            /// <summary>
            /// 生命上限基数 — MaxHp 变为派生值：MaxHp = BaseMaxHp + 成长加成。
            /// 0 表示未捕获（首次 ComputeAttribute 时以当前 MaxHp 为基数）。
            /// </summary>
            public float BaseMaxHp;

            /// <summary>
            /// 法力上限基数 — 语义同 BaseMaxHp。
            /// </summary>
            public int BaseMaxMp;

            /// <summary>
            /// 成长数据（灵根/境界/功法/异能）— 随 CharacterManager 存档整体序列化。
            /// </summary>
            public GrowthData Growth;

            /// <summary>
            /// 成长源收集提供者 — 汇总词条/内功/灵根/境界等成长系统对本角色的加成。
            /// 默认实现返回空结果（各成长系统接线后由 GrowthBonusService 替换）；
            /// 可替换为测试桩（仿 EquipmentSwapDropProvider 先例）。
            /// </summary>
            public static System.Func<CharacterData, GrowthSourceResult> GrowthCollectProvider { get; set; }
                = data => new GrowthSourceResult();

            /// <summary>
            /// 灵根生成提供者 — 玩家首次属性重算时随机五行灵根（终身不变）。
            /// 默认 null（不生成）；由 GrowthBonusService.Install 注入 LingGenRuleService.RollIfNotGenerated。
            /// </summary>
            public static System.Action<GrowthData, bool> LingGenRollProvider { get; set; }

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
                        this.ComputeAttribute(this.character.basicAttribute, this.character.IsPlayerCharacter, this.character.IsWorkerCharacter);
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
                        this.ComputeAttribute(this.character.basicAttribute, this.character.IsPlayerCharacter, this.character.IsWorkerCharacter);
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

                    // 将旧装备的所有权写入仓库（Worker=instanceId, Player=0）
                    int ownerId = (this.character != null && !this.character.IsPlayerCharacter)
                        ? this.character.GetInstanceID() : 0;
                    Core.ServiceLocator.Get<InventoryManager>().SetOwner(posMap, ownerId);

                    this.equipments[equipment.Type] = equipment;
                }
                else
                {
                    this.equipments.Add(equipment.Type, equipment);
                }

                if (this.character != null)
                {
                    this.ComputeAttribute(this.character.basicAttribute, this.character.IsPlayerCharacter, this.character.IsWorkerCharacter);
                }

            }

            /// <summary>
            /// 计算总属性 — 委托给 Domain/Character/AttributeCalculationService。
            /// </summary>
            /// <param name="basicAttribute">角色基础属性（由 Character.basicAttribute 提供）。</param>
            /// <param name="isPlayer">是否为玩家角色 — 影响等级加成倍率。</param>
            /// <param name="isWorker">是否为 Worker — Worker 获得减半的等级加成。</param>
            public void ComputeAttribute(Attribute basicAttribute, bool isPlayer, bool isWorker = false)
            {
                // 成长数据兜底（BinaryFormatter 反序列化不跑字段初始化器，读档后可能为 null）
                GrowthData.Ensure(ref this.Growth);

                // 玩家与 Worker 首次重算时生成灵根（终身不变；Enemy/已生成为空操作）
                LingGenRollProvider?.Invoke(this.Growth, isPlayer || isWorker);

                // 基数捕获：从未设置（<=0）时以当前值为基数，兼容创建器直接赋 MaxHp/MaxMp 的旧路径；
                // 此后 MaxHp/MaxMp 变为派生值 = 基数 + 成长加成
                if (this.BaseMaxHp <= 0)
                {
                    this.BaseMaxHp = this.MaxHp > 0 ? this.MaxHp : 100f;
                }

                if (this.BaseMaxMp <= 0)
                {
                    this.BaseMaxMp = this.MaxMp > 0 ? this.MaxMp : 100;
                }

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

                GrowthSourceResult growth = GrowthCollectProvider(this);

                BattleStats result = attributeCalcService.ComputeFinalStats(
                    baseStats,
                    this.Level,
                    isPlayer,
                    isWorker,
                    weaponBStats,
                    equipmentBStats,
                    growth.Sources);

                this.ATN = result.ATN;
                this.INT = result.INT;
                this.DEF = result.DEF;
                this.RES = result.RES;
                this.CRT = result.CRT;
                this.CSD = result.CSD;
                this.SPD = result.SPD;
                this.HIT = result.HIT;

                // 生命/法力上限 = 基数 + 成长加成，并钳制当前值不越界
                this.MaxHp = this.BaseMaxHp + growth.Special.MaxHpFlat;
                this.MaxMp = this.BaseMaxMp + (int)growth.Special.MaxMpFlat;
                if (this.Hp > this.MaxHp)
                {
                    this.Hp = this.MaxHp;
                }

                if (this.Mp > this.MaxMp)
                {
                    this.Mp = this.MaxMp;
                }

                // 特殊维度快照（吸血/反伤/回蓝/修炼速度）写回成长数据，供战斗事件点与 Tick 消费
                this.Growth.Special = growth.Special;
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
