# Technical Specification

## Project Overview

RandomWorld 是一款 2D 像素风生存殖民地建设游戏。玩家在随机生成的地图上建立殖民地，管理工人完成建造、采集、搬运等任务，抵御波次敌人进攻，并通过 LLM 驱动的 NPC 对话系统与角色互动。

## Platform Support

- Windows PC (主要)
- Android (PackageType.Android 已定义)

## Core Features

### 殖民地管理
- **工人系统:** 招募工人执行建造、搬运、采集、种植、吃饭、睡觉、锻炼、穿戴 8 种任务
- **任务队列:** 优先级队列(0-3)，KD 树空间查询分配最近任务
- **补给监控:** 饥饿/疲劳状态机 (Healthy → Hungry → Tired → Exhausted → Critical)
- **殖民地指挥中心 (F10):** 实时诊断报告 — 人力分析、任务阻塞原因、补给缺口、拥堵等级

### 战斗系统
- **波次敌人:** 普通波 + Boss 波(每 3 波)，难度渐进缩放
- **主动技能 (Q/E/R/F):** 旋风斩、冲刺、力量爆发、治疗之光
- **连击系统:** 多阶连击伤害/经验加成
- **装备稀有度:** Common → Uncommon → Rare → Epic → Legendary → Mythic，属性倍率递增
- **死亡惩罚:** 经验损失 + 复活计时器

### LLM NPC 对话
- 本地 llama-server 驱动的 NPC 对话
- 短期记忆 + 长期记忆压缩(每 8 轮)
- RAG 游戏知识检索
- 对话期间工人任务暂停

### 天气系统
- 晴/雨/雪三种天气
- 影响玩家/工人移动速度、任务进度、灵气恢复

### Worker 经济系统（v0.1.3 新增）
- **货币系统:** `CurrencyAmount` 值对象（Domain 层纯 C#），Worker/Player 双钱包，`CurrencyManager` 管理
- **市场交易:** `MarketService` 价格表，Worker 自主出售资源换金币，Player 购买物品
- **Worker 自主交易 AI:** `WorkerTradeService` — 背包满时自动出售多余资源，饥饿时自主寻找食物卖家购买
- **悬赏任务:** `BountyData` 数据结构 + `PlayerBountyService` — Player 发布悬赏→Worker 领取执行→完成结算/超时退款
- **Worker 人格:** `WorkerPersonality` 4 维值对象（心情/事业心/勤奋/社交），动态影响工作效率、交易决策、社交行为
- **Worker 目标:** `WorkerGoal` 目标驱动（赚钱/建筑/囤食物/做装备），驱动自主行为而非纯被动接任务
- **物品所有权:** `ItemOwnershipService` 追踪物品归属，Worker 采集/制作/购买获得所有权
- **Worker 大脑:** `WorkerSeekState` 空闲时根据人格/目标/状态自主选择行动（采集/出售/买食物/接悬赏）

### 商店与任务板系统（v0.1.3 新增）
- **商店 NPC:** `ShopNPC` + `ShopNPCGenerator` — 地图就绪后自动生成商店，支持 Worker/Player 买卖交互
- **任务板:** `TaskBoardManager` — 地图中心固定位置，Worker 存取物品的中转站，内存字典存储
- **TaskBoardHUD:** 任务板 UI 面板，展示存取记录

### 成就系统
- 5 类别(战斗/收集/生存/波次/工人)，最多 20 个成就
- F7 切换成就面板

### 存档系统
- 10 槽位多存档，二进制序列化(.lab)
- 自定义存档名、覆盖确认、清除

## Architecture

```
UI/Panel 层
    |
Gameplay/Manager 层 (运行时状态 + 事件驱动)
    |
Tool 层 (格式化、转换、Unity 适配)
    |
Domain 层 (纯 C# 规则引擎，零 Unity 依赖)
    |- 12 接口, 20 RuleService, EventBus
    |- 值对象: GameGridPosition, GameVector2
    |- 委托注入: ColonyDiagnosticContext
```

### 关键设计决策
- **Domain 纯 C#:** 无 UnityEngine 引用，12 个接口定义在最内层
- **UnityAdapter:** 适配器实现 Domain 接口，通过 ServiceLocator 注册
- **Shared Kernel:** `LAB2D.Enum` 含 16 个跨层枚举(DDD 模式)
- **HUD 热键:** 通过 GlobalInit.Update() 统一分发，避免子对象 inactive 时失效
- **Worker 经济:** Domain 层纯 C# 值对象（CurrencyAmount / WorkerPersonality / WorkerGoal / BountyData），Gameplay 层 Manager 驱动运行时
- **Worker AI 自主决策:** WorkerBrain 在空闲时根据人格+目标+状态自主选择行动，而非被动等待任务分配
