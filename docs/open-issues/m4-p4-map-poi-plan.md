# M4 包 4 切片：地图兴趣点（上古洞府 + 危险区）完整版方案

> 状态：**已实施完毕（2026-09-04，三轮切片全部落地：轮 1 危险区 c3473b06/6abade3e、
> 轮 2 洞府静态 235e15b3/4fac076f、轮 3 探索闭环 6becb2d3）**，当前形态见
> `spec.md` 地图兴趣点节（本文档为历史方案，实施中细节以 spec 为准——如 O 键派最近
> 空闲 Worker 取代了原方案的提示条面板内选项；奖励「灵石」落地为通用物资掉落）。
> 用户拍板：**带交互探索的完整版**——走近才揭示、探索耗时、有风险；危险区玩家和 Worker 都受影响。
> 大纲定位：`docs/游戏大纲.md:647` 包 4「每局不一样」最后一项。

## 可行性结论（调查确认，全部有先例）

- **撒点/存档/程序化视觉三件套**：LingQiManager 灵脉先例（OnMapReady 撒点 + GenCanReachPos
  可达约束 + 重试容错 + LingQiManagerData 点集入档 + LingVeinGlow 程序化纹理零 PNG）。
- **Worker 探索任务**：WorkerDefendTask 范本（Builder 模式 + `WorkerTaskType` 枚举 +
  `Traits.WorkerSpecific` 系统派发 + 进度吃 `ProgressMultiplierProvider` 全乘数链）。
- **出怪**：`EnemyManager.Create(pos, enemyKindId)` 单点入口（波次适配器同款调用，绕过
  WaveManager 即白天出怪，走既有 AI 索敌）。
- **玩家交互**：GlobalInputProcessor.ProcessJoinBattle 模式（Tick 检测 + 热键 + 面板互斥 +
  BattlePromptHUD 提示条）；读条打断复用打坐模式（移动/受击打断，CultivationManager 先例）。
- **减速接入点**：`Character.GetEffectiveMoveSpeed`（玩家/Worker 共用基类，地形乘数 try 块
  之后叠乘一层即可，一处改动双角色生效）。

## 系统一：上古洞府（探索型 POI）

### 撒点与状态机
- `AncientCaveManager`（Gameplay，ASingletonSaveData + IInitializable + ITickable）
- 每图撒 3~4 个：可达格 + 距地图中心 ≥20 格 + 洞府间距 ≥30 格（重试 500 次，灵脉同款容错）
- 状态机：`Hidden`（无视觉）→ `Revealed`（走近揭示）→ `Exploring`（占用中）→ `Explored`（枯竭）

### 揭示（走近才揭示）
- Tick 0.5s 节流扫描玩家位置 vs 各 Hidden 洞府距离，≤8 格 → Revealed
- 揭示时程序化视觉淡入（洞口暗斑+土金光晕，LingVeinGlow 模式）+ Tip「发现上古洞府！」

### 探索（耗时，双通道）
- **玩家亲自**：Revealed 洞府 2 格内屏幕提示条「按 N 探索上古洞府（30 秒）」→ 读条
  （移动/受击打断，打坐同款）→ 完成结算。进度条屏幕中央（纯代码 UI）
- **派 Worker**：提示条面板内选项「派 Worker 探索」→ 发布 `WorkerExploreTask`
  （`WorkerTaskType.Explore`：寻路到洞府邻格 → 驻留 60s 推进度（吃全乘数链）→ Finish 结算）
- 同一洞府同时仅一个探索者（占用标记，Exploring 态拒绝第二个）

### 风险（完成时 roll，Domain 纯函数）
- 35% **惊动妖兽**：洞府旁 3 格生成 2~3 只（`EnemyManager.Create`，白天遇敌成为洞府专属特色；
  Worker 探索者走既有接敌反应 LastAttacker→Attack，玩家可 B 加入回合制——与新系统打通）
- 10% **塌方受伤**：探索者扣 15% 最大生命
- 55% 平安

### 奖励（一次 roll，Domain 纯函数）
- 40% 功法秘籍：GongFaLibrary 随机未学直接习得（Tip「悟得上古残卷：X」）
- 25% 灵石资源：资源掉落管线在洞府格放高价值堆（TryMergeOrPlaceDrop 先例）
- 25% 遗物装备：EquipmentLoot 同款 roll（强度按第 5 波档）
- 10% 双倍：功法+资源各一

### 存档
- `AncientCaveManagerData`：位置+状态+占用者；旧档无数据 OnMapReady 迁移撒点（灵脉三路径同款）

## 系统二：危险区（空间惩罚 + 风险回报）

### 撒点与视觉
- `DangerZoneManager`（独立 Manager，职责分离）：每图 2~3 个（圆心+半径 10~14 格，
  避开中心 15 格、彼此不重叠）
- 程序化毒雾圈视觉（暗紫，LingVeinGlow 模式）**常显**——危险必须可见，与洞府的揭示惊喜互补

### 惩罚（玩家和 Worker 都受影响）
- 移动减速 ×0.7：`DangerZoneManager.GetMoveSpeedMultiplier(pos)` 接进
  `Character.GetEffectiveMoveSpeed` 地形乘数后（一处改动，双角色生效；A* 不感知减速，
  Worker 路过变慢但不会绕路——行为自然）
- 周期伤害**首版不做**（避免 Worker 因掉血频繁触发吃饭/逃跑打断任务，形成行为噪音），
  列为可选增强

### 收益（风险回报闭环）
- 区内必撒 2~3 个高价值资源点（现有资源生成管线，等级 +2——「铁矿长在毒雾里」）
- 区内灵气浓度 ×1.3（叠乘进 LingQiManager.ComposeAt，修饰符同款接入点——「险地生灵物」）

## 改动面（约 12-14 文件）与切分（autopilot 单轮原子性）

- **轮 1｜危险区全量**：DangerZoneRuleService（撒点/距离场纯函数）+ DangerZoneManager
  （撒点/乘数查询/存档）+ 减速接入 + 灵气叠乘 + 程序化视觉 + 单测——独立可玩
- **轮 2｜洞府静态**：AncientCaveRuleService 撒点/roll 纯函数 + AncientCaveManager
  （撒点/揭示扫描/状态机/存档）+ 程序化视觉 + 单测——地图上有可发现物
- **轮 3｜探索闭环**：WorkerTaskType.Explore + WorkerExploreTask + 玩家读条交互
  （热键 N + 提示条 + 进度 UI + 派 Worker 面板）+ 风险出怪 + 奖励结算——完整版收口

## 风险与开放问题

- 白天出怪是否冲击昼夜节奏：出怪量小（2~3 只）且只在洞府探索时，属「自找的」风险；
  妖兽 AI 走既有 Seek 索敌，白天活跃是特色
- WorkerExploreTask 不入存档（任务不存档是既有约定）；读档后洞府状态已持久，任务丢失可重派
- 联机：交互热键与 B 键同款互斥判定
