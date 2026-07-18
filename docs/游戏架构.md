# RandomWorld 项目业务架构分析报告

> 生成日期: 2026-07-12  
> 分支: feature/dev-0.1.2-arch  
> 项目: 2D 像素风生存/殖民地建设多人游戏

---

## 一、项目概述

**RandomWorld** 是一款基于 Unity 的 **2D 像素风生存/殖民地建设多人游戏**，使用 **Photon PUN 2** 实现网络多人同步，内置基于本地 LLM (Qwen2.5-0.5B) 的 AI NPC 对话系统。代码命名空间为 `LAB2D`，整体采用 **洁净架构 (Clean Architecture) + 领域驱动设计 (DDD)** 模式。

---

## 二、分层架构图

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          📊 表现层 (Presentation Layer)                       │
│  ┌───────────┐ ┌──────────┐ ┌───────────┐ ┌──────────┐ ┌──────────────────┐ │
│  │   Scenes  │ │   UI/    │ │ Character/│ │  Item/   │ │   AI/Dialogue/   │ │
│  │  场景管理  │ │ 面板/HUD │ │ 角色表现   │ │ 物品表现  │ │   LLM对话系统    │ │
│  └───────────┘ └──────────┘ └───────────┘ └──────────┘ └──────────────────┘ │
│  ┌───────────┐ ┌──────────┐ ┌───────────┐                                  │
│  │   Map/    │ │   MVC/   │ │ Gameplay/ │  ← 运行时系统 (Manager层)         │
│  │ 6层地图   │ │ 背包/建造│ │ 战斗/波次  │                                  │
│  └───────────┘ └──────────┘ └───────────┘                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                          🔌 适配器层 (Adapter Layer)                         │
│  ┌─────────────────┐ ┌──────────────────┐ ┌──────────────────────────────┐  │
│  │ UnityMapAdapter │ │ UnityPlayerInput │ │ UnityEnemySpawn / TipHelper  │  │
│  │ (地图可达性/出生)│ │ (WASD→命令对象)  │ │ (敌人创建/提示分发)           │  │
│  └─────────────────┘ └──────────────────┘ └──────────────────────────────┘  │
│  ┌─────────────────┐ ┌──────────────────┐ ┌──────────────────────────────┐  │
│  │ UnityGameTime   │ │ UnityVectorAdapter│ │ UnityItemDefinition           │  │
│  │ (时间抽象)       │ │ (向量类型转换)    │ │ (物品类型查找)                 │  │
│  └─────────────────┘ └──────────────────┘ └──────────────────────────────┘  │
├─────────────────────────────────────────────────────────────────────────────┤
│                     🧠 领域层 (Domain Layer) — 纯 C#，零 Unity 依赖           │
│  ┌───────────────┐ ┌─────────────┐ ┌──────────────┐ ┌───────────────────┐  │
│  │   Common/     │ │ Character/  │ │  Gameplay/   │ │    Worker/        │  │
│  │ EventBus      │ │ 伤害计算    │ │ 连击/死亡    │ │ 任务分配/状态     │  │
│  │ 接口契约(12)  │ │ 等级成长    │ │ 装备掉落     │ │ 补给/拥堵诊断    │  │
│  │ 值对象/命令   │ │             │ │ 技能/天气    │ │                   │  │
│  └───────────────┘ └─────────────┘ └──────────────┘ └───────────────────┘  │
│  ┌───────────────┐ ┌─────────────┐ ┌──────────────┐ ┌───────────────────┐  │
│  │  Inventory/   │ │   Player/   │ │    Wave/     │ │   Dialogue/       │  │
│  │ 库存堆叠/预留 │ │ 移动策略    │ │ 波次配置/Boss│ │ 提示词工程        │  │
│  │               │ │ 无敌帧/预警 │ │ 难度曲线     │ │                   │  │
│  └───────────────┘ └─────────────┘ └──────────────┘ └───────────────────┘  │
├─────────────────────────────────────────────────────────────────────────────┤
│                        ⚙️ 核心引擎层 (Core Engine Layer)                      │
│  ┌──────────────────┐ ┌─────────────────┐ ┌──────────────────────────────┐  │
│  │ Core/Seek/       │ │ Core/KDTree/    │ │ Core/ServiceLocator          │  │
│  │ A* 寻路 (多线程) │ │ 2D空间索引      │ │ 轻量DI容器                   │  │
│  │ WalkabilityCache │ │ KNN/范围搜索    │ │                              │  │
│  └──────────────────┘ └─────────────────┘ └──────────────────────────────┘  │
├─────────────────────────────────────────────────────────────────────────────┤
│                      🏗️ 基础设施层 (Infrastructure Layer)                    │
│  ┌──────────┐ ┌──────────┐ ┌───────────┐ ┌──────────┐ ┌────────────────┐  │
│  │ LogManager│ │ResourceMgr│ │ArchiveMgr │ │WeatherMgr│ │CoroutineMgr    │  │
│  │ 日志系统  │ │ 资源加载  │ │ 存档/读档 │ │ 天气控制  │ │ 协程调度       │  │
│  └──────────┘ └──────────┘ └───────────┘ └──────────┘ └────────────────┘  │
│  ┌──────────┐ ┌──────────┐ ┌───────────┐ ┌──────────┐ ┌────────────────┐  │
│  │  Enum/   │ │Constant/ │ │  Data/    │ │   SO/    │ │Serializable/   │  │
│  │ 15个枚举 │ │19个常量类│ │ 数据定义  │ │ 资源容器  │ │ 可序列化向量   │  │
│  └──────────┘ └──────────┘ └───────────┘ └──────────┘ └────────────────┘  │
├─────────────────────────────────────────────────────────────────────────────┤
│                         🌐 网络层 (Networking Layer)                         │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │              Photon PUN 2 — RPC / IPunObservable / PhotonView         │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 三、核心业务模块架构图

```
┌──────────────────────────────────────────────────────────────────────────┐
│                          🎮 RandomWorld 业务模块                          │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌─────────────────────── 战斗系统 ───────────────────────┐              │
│  │  WaveManager ──→ 波次生成/难度曲线/Boss战              │              │
│  │  SkillManager ──→ 4技能 (旋风斩/冲刺/力量爆发/治疗)    │              │
│  │  ComboBonusManager ──→ 6阶连击倍率 (1.0x~2.5x)       │              │
│  │  DeathPenaltyManager ──→ 死亡经验扣除/复活倒计时       │              │
│  │  EnemyLootManager ──→ 6级稀有度装备掉落               │              │
│  └───────────────────────────────────────────────────────┘              │
│                                                                          │
│  ┌─────────────────────── 殖民地模拟 ────────────────────┐              │
│  │  WorkerTaskManager ──→ 8种任务类型/4级优先级分配      │              │
│  │  WorkerConditionManager ──→ 5阶段饥饿/疲劳状态机      │              │
│  │  WorkerSupplyIssueManager ──→ 食物/床位补给诊断       │              │
│  │  WorkerTaskCongestionAdvisor ──→ 任务队列拥堵分析      │              │
│  │  ColonyCommandCenterManager ──→ F8全局指挥中心面板     │              │
│  └───────────────────────────────────────────────────────┘              │
│                                                                          │
│  ┌─────────────────────── 地图与建造 ────────────────────┐              │
│  │  TileMap ──→ 程序化地形生成 (7种地形)                 │              │
│  │  BuildMap ──→ 建造放置/完成/取消                       │              │
│  │  ResourceMap ──→ 树木资源再生 (5分钟协程)             │              │
│  │  RoomManager ──→ 射线检测封闭房间/温湿度               │              │
│  │  FarmlandManager ──→ 农田种植/生长追踪                 │              │
│  │  FurnitureManager ──→ 床-工人绑定                      │              │
│  └───────────────────────────────────────────────────────┘              │
│                                                                          │
│  ┌─────────────────────── AI 对话系统 ────────────────────┐              │
│  │  DialogueManager ──→ 本地LLM驱动的NPC对话管道          │              │
│  │  PromptBuilder ──→ 角色人设/游戏状态/模板填充          │              │
│  │  GameKnowledgeRetriever ──→ 关键词RAG知识检索          │              │
│  │  DialogueMemoryManager ──→ 短期FIFO/长期摘要压缩       │              │
│  │  LlamaServerClient ──→ 本地llama-server HTTP/SSE通信   │              │
│  └───────────────────────────────────────────────────────┘              │
│                                                                          │
│  ┌─────────────────────── UI 框架 ────────────────────────┐              │
│  │  PanelController ──→ 面板栈管理 (Show/Close/暂停)      │              │
│  │  MVC背包/建造 ──→ 通用泛型MVC + 拖拽交换               │              │
│  │  20+ HUD面板 ──→ 技能/成就/天气/工人/波次Boss等       │              │
│  │  PixelUITheme ──→ 统一像素风色彩体系                   │              │
│  └───────────────────────────────────────────────────────┘              │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## 四、角色层次与状态机架构

```
                      MonoBehaviourPun (Photon 网络)
                              │
                     ┌────────┴────────┐
                     │   Character     │  抽象基类
                     │ (战斗属性/HP)   │  CharacterData
                     │ CharacterHealth │  DamageCalculator
                     │   Component     │  LevelProgression
                     └───────┬─────────┘
                             │
          ┌──────────────────┼──────────────────┐
          │                  │                  │
    ┌─────┴─────┐    ┌──────┴──────┐    ┌──────┴──────┐
    │  Player   │    │   AEnemy    │    │  AWorker    │
    │ (无状态机) │    │ +感知系统   │    │ +饥饿/疲劳   │
    │ WASD输入  │    │ +EnemyData  │    │ +WorkerData  │
    │ Animator  │    └──────┬──────┘    │ +ASeek路径   │
    └───────────┘           │           │ +任务引用    │
                   ┌────────┴────────┐  └──────┬──────┘
                   │                 │         │
            ┌──────┴──────┐  ┌──────┴──────┐  │
            │ACommonEnemy │  │ ASeekEnemy  │  │
            │ 固定漫游     │  │ A*网格巡逻   │  │
            │ 5状态机      │  │ 4状态机     │  │
            └─────────────┘  └─────────────┘  │
                                        ┌──────┴──────┐
                                        │ Worker_Lv1  │
                                        │ 6状态机     │
                                        └─────────────┘

    ┌─── 状态机对比 ───────────────────────────────────────┐
    │                                                      │
    │  CommonEnemy:  Wander → Seek → Chase → Attack → Dead│
    │  SeekEnemy:    Seek → Move → Attack → Dead          │
    │  Worker:       Seek → Move → Work → Attack/Escape   │
    │                      ↘ Dead                         │
    │  Player:        直接输入轮询 (无状态机)              │
    └──────────────────────────────────────────────────────┘
```

---

## 五、Worker 任务系统

```
    ┌─────────────── WorkerTaskManager ───────────────┐
    │              全局任务分发器 (每帧Update)          │
    │                                                   │
    │  优先级0 ──→ [BuildTask, CarryTask, ...]         │
    │  优先级1 ──→ [GatherTask, PlantTask, ...]        │
    │  优先级2 ──→ [HungryTask, SleepTask, ...]        │
    │  优先级3 ──→ [ExerciseTask, WearTask, ...]       │
    │                                                   │
    │  分配策略: KD-Tree最近邻 + WorkerTaskAssignment   │
    └──────────────────────────────────────────────────┘
                           │
          ┌────────────────┼────────────────┐
          ▼                ▼                ▼
    ┌──────────┐    ┌──────────┐    ┌──────────┐
    │  Build   │    │  Carry   │    │  Gather  │
    │  建造    │    │  搬运    │    │  采集    │
    └──────────┘    └──────────┘    └──────────┘
    ┌──────────┐    ┌──────────┐    ┌──────────┐
    │  Plant   │    │  Hungry  │    │  Sleep   │
    │  种植    │    │  吃饭    │    │  睡觉    │
    └──────────┘    └──────────┘    └──────────┘
    ┌──────────┐    ┌──────────┐
    │ Exercise │    │   Wear   │
    │  锻炼    │    │  穿戴    │
    └──────────┘    └──────────┘

    Worker 状态驱动的工作流:
    Seek(寻路) → Move(移动) → Work(执行任务) → Seek(循环)
                                  │
                          进度完成/体力耗尽
                                  │
                      吃饭/睡觉任务打断
```

---

## 六、数据流架构

```
┌──────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────┐
│  输入层   │────→│   适配器层    │────→│   领域层     │────→│  输出层   │
│          │     │              │     │              │     │          │
│ ⌨️ 键盘  │     │PlayerInput   │     │IGameCommand  │     │EventBus  │
│ 🖱️ 鼠标  │────→│  Adapter     │────→│ • MoveCmd    │────→│• Player  │
│          │     │              │     │ • AttackCmd  │     │  Moved   │
└──────────┘     │UnityMap      │     │ • SkillCmd   │     │• Damaged │
                 │  Adapter     │     │              │     │• Wave    │
                 │              │     │RuleService   │     │  Changed │
┌──────────┐     │UnityEnemy    │     │ 计算输出     │     │• Worker  │
│  Photon  │────→│  Spawn       │     │              │     │  Task    │
│  网络同步 │     │  Adapter     │     │              │     │          │
└──────────┘     └──────────────┘     └──────────────┘     └────┬─────┘
                                                                │
                                                 ┌──────────────┴──────┐
                                                 ▼                     ▼
                                          ┌──────────┐         ┌──────────┐
                                          │   UI层    │         │Gameplay层│
                                          │ 面板/HUD  │         │ Manager  │
                                          │  更新     │         │  状态变更│
                                          └──────────┘         └──────────┘
```

---

## 七、模块依赖矩阵

```
               Domain  Core  Manager  Map  Item  Char  Gameplay  UI  AI  MVC  Tool
    Domain        -     ↓      ↓      ↓     ↓     ↓       ↓      ↓   ↓   ↓    ↓
    Core          ✗     -      ↓      ↓     ✗     ✗       ✗      ✗   ✗   ✗    ↓
    Manager       ✗     ↓      -      ↓     ↓     ↓       ↓      ↓   ↓   ↓    ↓
    Map           ✗     ↓      ↓      -     ↓     ↓       ✗      ✗   ✗   ✗    ↓
    Item          ✗     ✗      ↓      ↓     -     ↓       ↓      ✗   ✗   ↓    ↓
    Character     ✗     ↓      ↓      ↓     ↓     -       ↓      ✗   ✗   ✗    ↓
    Gameplay      ✗     ✗      ↓      ↓     ↓     ↓       -      ↓   ✗   ✗    ↓
    UI            ✗     ✗      ↓      ✗     ↓     ↓       ↓      -   ✗   ↓    ↓
    AI            ✗     ✗      ↓      ✗     ✗     ↓       ✗      ↓   -   ✗    ↓
    MVC           ✗     ✗      ↓      ✗     ↓     ✗       ✗      ↓   ✗   -    ↓
    Tool          ✗     ↓      ↓      ↓     ↓     ↓       ↓      ↓   ↓   ↓    -

    图例: ↓ = 依赖下层  ✗ = 无直接依赖  - = 自身
```

---

## 八、关键架构特征总结

| 特征 | 实现方式 |
|------|----------|
| **分层架构** | 基础设施 → 核心引擎 → 领域 → 适配器 → 表现层，严格的单向依赖 |
| **领域驱动设计** | Domain/ 层零 UnityEngine 引用，纯 C# 无状态 RuleService |
| **依赖反转** | 12 个接口 (IGameTime, IMapWalkabilityQuery, IEnemySpawnService...) 定义在 Domain/Common/ |
| **适配器模式** | UnityAdapter/ 将 Unity 特有类型转换为领域值对象 |
| **EventBus** | 发布/订阅解耦 (PlayerMoved, Damaged, WaveChanged, WorkerTask...) |
| **命令模式** | IGameCommand (PlayerMoveCommand, PlayerAttackCommand, ActivateSkillCommand) |
| **状态机** | 泛型 `CharacterStateManager<CS, CST, C>` — Enemy 4-5 状态，Worker 6 状态 |
| **对象池** | A* 寻路的 SpendPool、FloatingText 对象池 (30-60 实例) |
| **快照模式** | WorkerAgentSnapshot / WorkerTaskSnapshot 隔离领域与 Unity 对象 |
| **多线程寻路** | A* 在后台线程运行，Semaphore 控制并发，WalkabilityCache 无锁读取 |
| **网络同步** | Photon PUN 2 — RPC / IPunObservable / PhotonView，二进制序列化同步 |
| **存档系统** | ArchiveManager 反射发现 ASaveData 子类，二进制序列化，最多 10 个槽位 |
| **MVC 框架** | 泛型 MVC 基类 → Backpack(背包) / Build(建造) 两个具体模块 |
| **LLM 对话** | 本地 llama-server + SSE 流式输出 + RAG 知识检索 + 短期/长期记忆 |
| **HUD 热键** | F1-F8 功能键切换各 HUD 面板 (技能/成就/天气/工人状态/补给/任务队列/指挥中心) |

---

## 九、Domain 层详细分解

Domain/ 包含 8 个子域，共 49 个 C# 文件，全部零 UnityEngine 引用。

### 9.1 Common/ — 共享内核 (17 文件)

| 类/接口 | 职责 |
|----------|------|
| `EventBus` | 集中式发布/订阅事件总线 |
| `GameGridPosition` | 引擎无关的网格位置值对象 |
| `GameVector2` | 引擎无关的 2D 向量，支持平方距离 |
| `IGameEvent` | 领域事件标记接口 (PlayerMoved / CharacterDamaged / InventoryChanged / WaveStateChanged / WorkerTask) |
| `IGameCommand` | 命令接口 (PlayerMove / PlayerAttack / ActivateSkill) |
| `MathHelper` | 共享数学工具 (RoundToInt / Clamp01 / CeilToInt) |
| `WeatherType` | 领域天气枚举 (Clear / Rain / Snow) |
| `IGameTime` | 时间抽象接口 (DeltaTime / Time / RealtimeSinceStartup) |
| `IItemDefinitionProvider` | 物品类型查询接口 |
| `IMapWalkabilityQuery` | 地图通行性查询接口 |
| `IMapSpawnPointProvider` | 地图出生点提供接口 |
| `IEnemySpawnService` | 敌人创建与统计接口 |
| `IGameLogger` | 日志记录抽象 |
| `ITipService` | 提示消息接口 |
| `ITileMapQuery` | 地图坐标转换/边界查询接口 |
| `IResourceMapQuery` | 资源节点查询接口 |
| `IInitializable` | 服务生命周期接口 (Initialize / Tick) |

### 9.2 Character/ — 角色战斗与成长 (2 文件)

| 类 | 职责 |
|-----|------|
| `DamageCalculator` | 伤害计算: `ApplyDefense(damage, defense)` → `damage - damage * defense / 10` |
| `LevelProgressionService` | 经验值升级: 满经验后升级，所需经验翻倍 |

### 9.3 Gameplay/ — 核心游戏循环规则 (9 文件)

| 类 | 职责 |
|-----|------|
| `AchievementRuleService` | 成就进度计算 |
| `ComboBonusRuleService` | 6 阶连击倍率 (1.0x / 1.2x / 1.5x / 1.8x / 2.2x / 2.5x) |
| `DeathPenaltyRuleService` | 复活倒计时 / 经验扣除 / 复活治疗量 |
| `EquipmentLootRuleService` | 6 级稀有度 (Common~Mythic) + 8属性倍率 (1.0x~3.2x) |
| `GameplaySessionStatsRuleService` | 会话统计 (连击超时/时长/最大连击数) |
| `SessionResultRuleService` | 终局评分: 5维度加权 → S/A/B/C/D + 1-5星 |
| `SkillRuleService` | 技能伤害/冷却/升级消耗 (最高5级) |
| `WeatherGameplayRuleService` | 天气惩罚: 雨雪对移动/工作/能量恢复的影响 |
| `ColonyCommandCenterRuleService` | **814行** — 14种任务阻塞原因诊断 / 4级预警 / 行动建议 |

### 9.4 Inventory/ — 库存管理 (3 文件)

| 类 | 职责 |
|-----|------|
| `InventoryStackingService` | 槽位堆叠容量计算 (cellCapacity - current - reserved) |
| `InventoryFoodReservationService` | 食物预留 (工人饥饿值 → 所需食物数) |
| `InventoryTakeReservationService` | 并发取出预留，防止双重领取 |

### 9.5 Player/ — 玩家专属机制 (3 文件)

| 类 | 职责 |
|-----|------|
| `PlayerMovementPolicy` | 跑步速度钳制 (不低于 1.0x) |
| `PlayerDamagePolicy` | 无敌帧判断 (伤害为零/复活中/无敌窗口内 → 忽略) |
| `PlayerVitalAlertRuleService` | 5级生命预警 (Safe/Wounded/Critical/Respawning)，阈值 35%/18% |

### 9.6 Wave/ — 波次系统 (3 文件)

| 类 | 职责 |
|-----|------|
| `WaveConfigModel` | 配置模型: 基础敌人数/每波增量/最大存活数/总波数/难度倍率 |
| `WaveRuleService` | 难度曲线: `1 + completed * scalePerWave`; 敌人数: `base + (wave-1) * increase` |
| `WaveBossRuleService` | Boss波判定: 每N波取模; Boss属性倍率; 6种奖励计算 |

### 9.7 Worker/ — 工人AI核心 (7 文件)

| 类 | 职责 |
|-----|------|
| `WorkerTaskAssignmentService<T>` | 基于KD-Tree最近邻的任务分配 |
| `WorkerTaskProgressService` | 任务进度推进 + 疲劳消耗 |
| `WorkerTaskSnapshot<T>` | 只读任务候选快照 |
| `WorkerAgentSnapshot` | 只读工人状态快照 |
| `WorkerConditionRuleService` | 5阶段状态 (Healthy/Hungry/Tired/Exhausted/Critical)，速度/效率倍率 |
| `WorkerSupplyRuleService` | 补给缺口诊断 (食物/床位/危重工人) |
| `WorkerTaskCongestionRuleService` | 4级拥堵 (Smooth/Busy/Congested/Critical)，主导积压检测 |

### 9.8 Dialogue/ — AI对话 (2 文件)

| 类 | 职责 |
|-----|------|
| `DialoguePromptProfileModel` | NPC 人设配置 (名称/角色/性格/背景/说话风格) |
| `PromptAssemblyService` | 模板填充: 角色数据 + 游戏状态 + 知识库 → LLM消息列表 |

---

## 十、目录文件统计

```
Scripts/2D/
├── Domain/          49 个 C# 文件   (8 个子域)
├── Character/       40+ 个 C# 文件  (Player/Worker/Enemy 三级层次)
├── Gameplay/        30+ 个 C# 文件  (战斗/成就/波次/殖民地管理)
├── UI/              30+ 个 C# 文件  (面板栈/HUD/交互/效果)
├── AI/              15+ 个 C# 文件  (LLM对话/RAG/记忆)
├── Tool/            17 个静态类     (规则计算/格式化/UI构建)
├── Map/             7 个 C# 文件    (6层Tilemap系统)
├── Item/            15+ 个 C# 文件  (物品分类/库存/掉落)
├── Manager/         5 个 C# 文件    (日志/资源/存档/天气/协程)
├── Core/            8 个 C# 文件    (A*/KDTree/DI)
├── MVC/             6 基类 + 8 子类 (通用框架 + 背包/建造)
├── Data/            10 个 C# 文件   (数据定义/接口)
├── SO/              5 个 C# 文件    (ScriptableObject)
├── Enum/            15 个枚举
├── Constant/        19 个常量类
├── UnityAdapter/    8 个适配器
├── Serializable/    1 文件 (4 向量类型)
├── Attributes/      1 文件
└── Flag/            1 文件
```

---

## 十一、设计模式应用汇总

| 模式 | 应用位置 | 说明 |
|------|----------|------|
| **洁净架构** | 全局 | Domain 层零 Unity 依赖，通过接口反转依赖方向 |
| **领域驱动设计** | Domain/ | 8 个子域，无状态 RuleService，值对象 |
| **适配器模式** | UnityAdapter/ | 8 个适配器桥接 Unity 类型与领域接口 |
| **命令模式** | Domain/Common/ | IGameCommand → PlayerMove/Attack/Skill 命令 |
| **事件驱动** | Domain/Common/ | EventBus + IGameEvent 发布/订阅解耦 |
| **状态机模式** | Character/ | 泛型 `CharacterStateManager<CS, CST, C>` |
| **工厂模式** | Character/ | CharacterCreator<T> 模板方法 |
| **管理器/注册表** | Manager/ Gameplay/ | 单例管理器模式 (CharacterManager, WaveManager...) |
| **MVC 模式** | MVC/ | 泛型 MVC 基类 → Backpack / Build 模块 |
| **对象池** | Core/Seek/ UI/ | SpendPool (A*), FloatingText 对象池 |
| **建造者模式** | Data/ | ItemDataBuilder |
| **服务定位器** | Core/ | ServiceLocator (静态 DI 容器) |
| **快照模式** | Domain/Worker/ | WorkerAgentSnapshot / WorkerTaskSnapshot |
| **策略模式** | Domain/ | 各 RuleService 封装可变业务规则 |
| **模板方法** | Character/ | CharacterCreator.Create() → DoCreate() |
