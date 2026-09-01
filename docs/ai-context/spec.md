# Technical Specification

## Project Overview

RandomWorld 是一款 2D 像素风生存殖民地建设游戏。玩家在随机生成的地图上建立殖民地，管理工人完成建造、采集、搬运等任务，抵御波次敌人进攻，并通过 LLM 驱动的 NPC 对话系统与角色互动。

## Platform Support

- Windows PC (主要)
- Android (PackageType.Android 已定义)

## Core Features

### 殖民地管理
- **工人系统:** 招募工人执行建造、搬运、采集、种植、吃饭、睡觉、锻炼、穿戴、挖掘 9 种任务
- **任务队列:** 优先级队列(0-3)，KD 树空间查询分配最近任务
- **补给监控:** 饥饿/疲劳状态机 (Healthy → Hungry → Tired → Exhausted → Critical)；疲劳 `CurTired` 为累积疲劳值（越大越疲，初始 0；工作/空闲累积、睡眠降低，空闲累积受天气/温度倍率加速），疲劳值 > `MaxTired - ThresholdTired`(20) 判定需休息——所有疲劳阈值判断方向是 `>` 而非 `<`
- **殖民地指挥中心 (F10):** 实时诊断报告 — 人力分析、任务阻塞原因、补给缺口、拥堵等级
- **建造任务恢复:** 游戏重启/场景加载后自动找回原建造者恢复建造任务
- **建造卡死重试:** `MovementStuckDetector` 每秒位移检测，累计位移 < 期望 40%（Sliding）先预防性重寻路，< 15%（Stuck）判硬卡死窗口，最多 3 次重试，避免任务误放弃
- **建造位置预注册:** 建造位置冲突时自我预留跳过，配合建造者名称参数实现任务恢复

### 战斗系统
- **波次敌人:** 普通波 + Boss 波(每 3 波)，难度渐进缩放
- **主动技能 (8 槽):** 默认 Q/E/R/F（旋风斩、冲刺、力量爆发、治疗之光）+ 功法外功/异能动态注册槽 Z/X/C/V（见成长系统节）
- **连击系统:** 多阶连击伤害/经验加成
- **装备稀有度:** Common → Uncommon → Rare → Epic → Legendary → Mythic，属性倍率递增
- **死亡惩罚:** 经验损失 + 复活计时器
- **装备对比弹窗:** 拾取装备时自动弹出对比面板，显示当前装备 vs 新装备属性差异，支持替换/丢弃操作

### Worker 自动生存
- **血瓶自动使用:** HP 低于 30% 时自动消耗背包中的血瓶（3 秒冷却），战斗结束后和低血量检测时触发
- **自主交易:** 背包满时自动出售多余资源，饥饿时自主寻找食物卖家
- **自主决策:** 空闲时 WorkerBrain 根据人格/目标/状态自主选择行动

### 地形系统
- **地形挖掘:** Worker 可挖掘可挖掘地形（如山），复用 GatherTask + GatherMap 认领机制防止多人同时挖掘
- **地形生成:** 程序化地形生成（7 种地形），随机散布 + 最近邻填充
- **地形配置:** TerrainTileConfig ScriptableObject，支持可行走/可建造/可生长树木/可生成资源配置

### LLM NPC 对话
- 本地 llama-server 驱动的 NPC 对话
- 短期记忆 + 长期记忆压缩(每 8 轮)
- RAG 游戏知识检索
- 对话期间工人任务暂停

### 天气系统
- 晴/雨/雪三种天气
- 影响玩家/工人移动速度、任务进度、灵气恢复

### Worker 经济系统
- **货币系统:** `CurrencyAmount` 值对象（Domain 层纯 C#），Worker/Player 双钱包，`CurrencyManager` 管理
- **市场交易:** `MarketService` 价格表，Worker 自主出售资源换金币，Player 购买物品
- **Worker 自主交易 AI:** `WorkerTradeService` — 背包满时自动出售多余资源，饥饿时自主寻找食物卖家购买
- **悬赏任务:** `BountyData` 数据结构 + `PlayerBountyService` — Player 发布悬赏→Worker 领取执行→完成结算/超时退款
- **Worker 人格:** `WorkerPersonality` 4 维值对象（心情/事业心/勤奋/社交），动态影响工作效率、交易决策、社交行为
- **Worker 目标:** `WorkerGoal` 目标驱动（赚钱/建筑/囤食物/做装备），驱动自主行为而非纯被动接任务
- **物品所有权:** `ItemOwnershipService` 追踪物品归属，Worker 采集/制作/购买获得所有权
- **Worker 大脑:** `WorkerDecisionService` 空闲时根据人格/目标/状态自主选择行动（采集/出售/买食物/接悬赏）

### Worker 生存与成长数值（压力/士气/熟练度/贪婪懒惰）
- **压力 `CurStress` / 士气 `CurMorale`:** `WorkerUpdateSystem`（ITickable，GlobalInit 驱动）每帧更新——压力工作期 `Execute` 累积、空闲自然衰减（`StressDecayPerSecond=0.02`），睡眠/进食/锻炼/漫游恢复，`CurStress > MaxStress - 40` 视为高压触发减压决策；士气按困苦度（饥饿不足×0.4 + 疲劳×0.4 + 压力×0.2）下降、安好回升（`MoraleRecoverPerSecond=0.02`）
- **进度倍率联动:** 压力比例 >70% 或士气 <40% 起线性惩罚任务进度（`WorkerConditionRuleService.GetStressWorkMultiplier`/`GetMoraleWorkMultiplier`，下限 0.7/0.75）；熟练度 `SkillProgress` [0,100] 每次完成核心工作类任务 +0.8，进度倍率 = 1 + 熟练度×0.004（100 点 = +40%，`WorkerSkillProgressService` 纯算术）；**吃饭/睡觉/地面睡眠任务豁免**所有进度惩罚
- **贪婪/懒惰 `Greed`/`Laziness`:** 人格数值，入 ML 特征 19/20 维真实值（`WorkerFeatureExtractor` 41 维）；心智层嫉妒行为用 `Greed>60`
- **倍率出口:** `WorkerConditionManager`（Singleton + IWorkerConditionManager）按 Worker 汇总快照，经 `GetAdjustedWorkerMoveSpeed`（移动）与 `GetWorkerTaskProgressMultiplier`（进度）输出，与饥饿/疲劳状态机共用同一快照机制

### 好感度系统
- **定向关系:** Worker↔Worker（双向）+ Worker→Player，数值 [0, 100] 初始 50。运行时键 `Worker.GetInstanceID()`，Player 恒为 `PlayerId=0`（与 `PlayerBountyService.PlayerOwnerId` 一致）；懒初始化零预填
- **分层:** `FavorabilityRuleService`（Domain/Worker 纯 C#，集中阈值/增减量/价格纯函数，可单测）+ `FavorabilityManager`（Gameplay，仿 CurrencyManager：ASingletonSaveData + ITickable）
- **态度标签:** <30 敌对 / 30-49 疏远 / 50-69 友好 / 70-84 亲近 / ≥85 挚友（`GetAttitudeLabel`）
- **行为门控（四项）:**
  - **悬赏接取:** `WorkerBountyTask.IsCanWork` — 对玩家好感 <35（`PlayerBountyRefuseThreshold`）拒接玩家悬赏；对发布 Worker 好感 <40 拒接其悬赏
  - **交易:** 卖者对买者好感 <30（`TradeRefuseThreshold`）拒卖；价格乘数 `1+(50-好感)×0.004` clamp [0.7, 1.3]，高好感折扣/低好感加价（`WorkerTradeService`）
  - **对话:** 好感数值+态度标签写入 LLM 提示词（`GameStateContext.favorabilityText`），LLM 据此调整对话态度
  - **协作互助:** Player 击伤敌方后受击点 4 格半径内 Worker 对玩家好感 +8（30s 冷却/Worker，`Character.ReduceHp` 触发）
- **增减触发:** 攻击（Player 打 Worker 每次命中 -15，致死额外 -10；Worker 互殴受害→肇事 -10）；悬赏完成（Player +8 / 低奖励<40 金币 +4，Worker 间双向 +6，`CurrencyManager.RewardBounty`）；交易（成功 buyer→seller +4 / seller→buyer +2，被拒 buyer→seller -3）；对话（结束 +2，每日上限 10，按游戏日 600s 重置）；接近/共事（3s 节流扫描，半径 4 格，Worker↔Worker 每 tick +0.1、与 Player +0.15，每对累计上限 10）
- **Mood 联动:** 好感变动 |delta| ≥5 时 `WorkerPersonality.AfterFavorabilityChange` 将 Mood ± delta×0.05（clamp ±5）
- **存档:** ASingletonSaveData 二进制，按 Name 稳定匹配（"PLAYER" 哨兵），旧档无此文件零迁移；Worker 死亡 `RemoveDeadWorker` 清理档案与引用
- **UI:** F11 好感度 HUD（`FavorabilityHUD`：对玩家好感+态度标签、Top3 Worker↔Worker 关系、空态提示）；`ItemInfoUI` 点击 Worker 显示"对你好感: X（标签）"
- **初始好感:** 取 `NPCPromptProfile.initialFavorability`（Worker profile），默认 50

### Worker 心智层（纯规则，无 LLM）
- **定位:** 叠加在好感度之上（读取好感度作数值底座、行为调制在好感门控处汇合），互不写对方核心数值；拒绝/关系是**模型之外的决策门**（`UseModelDecision` 开启也不绕过，ML 41 维特征不动）
- **数据:** `WorkerData.Mind`（`WorkerMindData`，`[Serializable]` 纯值类型）随角色二进制存档一次写入，旧档 `WorkerMindData.Ensure` 兜底零迁移；跨存档引用一律 Name 字符串 + `"PLAYER"` 哨兵；集合用 `List`（字典不落档）
- **自主意志（拒绝/拖延/强制）:** `CommandAcceptanceRuleService.Evaluate` 体验门 + `WorkerBountyTask.DoIsCanWork` 权威门双门——**不能只挂体验门**：`RunTaskAssignmentLoop` 每 15 帧会从优先级 0 绕过 `SeekState` 重派同一玩家悬赏，权威门按怨恨≥85 或拒绝冷却中拦截。Evaluate 优先级：生存硬阻断（饥饿<15/疲劳>Max-15/精气神<10→Delay 交紧急打断）→ 拖延冷却→Delay → 好感<35→Refuse → 怨恨≥85（或 ≥60+随机）→Refuse → 感恩≥65→Accept → 意愿度<25→Delay → 6%（心情<20 时 15%）随机→Delay → Accept。`ForceCommand` 设 60s 放行窗口，代价怨恨+8（冷却中再+3）/信任-5/好感-5/计数++，强制后进 40s 冷却防刷
- **事件记忆 + 信念:** `WorkerMindService.RecordEvent` 统一入口 → `WorkerMemoryRuleService`（记忆上限 24、逐日权重衰减）→ `WorkerBeliefRuleService`（信念 [0,100] 初始 50：信任世界/信任玩家/自尊/归属感）
- **随机人生事件:** `WorkerLifeEventRuleService` 事件表（灵感/横财/领悟/变故/疾病/小确幸/梦魇）+ `WorkerDreamRuleService` 执念（`RefreshGoal` 人格分支前 25% 把 `CurrentGoal` 指到执念映射）。**平衡三原则**：封顶（生存单次 ≤±15、人格漂移 ≤±8）；恩典（濒危当轮不掷，一天最多 1 次）；可恢复（负事件只动士气/精气神/心情软维度，绝不扣饥饿/疲劳致死线）。`WorkerMindManager` 每 2 游戏日按日口径掷 `Random.value < 0.35`
- **性格演化:** `PersonalityDriftRuleService` 四桶（心情/事业心/勤奋/社交）由 `RecordEvent` 按强度累积，`|v|≥12` 迁移 ±2（clamp）归零。**防横跳三机制**：滞回带（反向需积 12+6=18，`*Dir` 字段记忆方向）、每日限流（`Migrate` 日限 1）、桶饱和（±30 上限）
- **社会关系:** `WorkerRelationshipRuleService` + `Mind.Relations`（name 键控），Kind 优先级 Grudge>Enmity>Admiration>Friendship>None；友谊 `Affinity≥40`/敌意 `≤-30`/爱慕 `Admiration≥40`/记仇（被拒交易 30、被攻击 40，每日 -2 衰减）。**四个低频行为（防经济干扰）**：①互助/回避——friend/admiration 好感门前豁免必接、enmity/grudge 拒接（`WorkerBountyTask.DoIsCanWork`）②拒卖——关系否决优先于人格（`WorkerTradeService.WillSell` 前置）③送礼——漫游决策前 5%，双方 Affinity+8、收方好感+5（`WorkerDecisionService`）④嫉妒——`CurrencyManager.CompleteBounty` 节流钩子（≥30s），旁观 `Greed>60` 对完成者 Affinity-4。每日 `Decay` 淡化；死亡清理 `FavorabilityManager.RemoveDeadWorker`
- **事件接入点:** 完成/接取悬赏、交易成败、被攻击、玩家救危、对话结束等事件点调 `RecordEvent`（TargetName=`"PLAYER"` 哨兵或 Worker 名），绝不每帧循环；`WorkerMindManager.Tick`/`ProcessDayRollover` 驱动
- **反馈:** 拒绝理由/人生事件/关系变化统一走 `AWorker.ShowMindBubble`（语料 `WorkerInnerMonologue`，防被 `ShowRandomMonologue` 覆盖：进口气卫 + `HideDialogText` 清守卫）+ `[MindDiag]` Debug 日志 + `WorkerConditionHUD`「最近想法」行

### 成长系统（词条/功法/修仙/异能/灵根/生活技能/科技）
- **统一属性管线:** 被动加成源（装备词条/内功/境界永久加成）→ `GrowthBonusService.CollectFromData` → `GrowthSourceResult`（`Sources`: List&lt;BattleStats&gt; + `Special`: GrowthBonus）→ `AttributeCalculationService.ComputeFinalStats(growthSources)`。特殊维度（回蓝/吸血/反伤/修炼速度）存 `GrowthBonus.Special`（`ComputeAttribute` 写回快照），各系统在战斗事件点消费。**CRT/CSD 是 0-1 比例**（"+3%" 存 0.03，BattleStats 直接加整数会爆数值）
- **MaxHp 派生:** `MaxHp = BaseMaxHp + 成长加成`，由 `CharacterData.ComputeAttribute` 统一计算并钳制 Hp
- **GrowthData（成长容器）:** 随 CharacterData 二进制存档——灵根五行/境界/灵气/永久加成/`LearnedGongFaIds`/`ActiveNeiGongId`(string，空=未激活)/`AwakenedPowerIds`。`GrowthData.Ensure(ref)` 兜底（BinaryFormatter 不跑构造），玩家与 Worker 首次属性计算时生成灵根（终身不变；`ComputeAttribute` 传 `isPlayer || isWorker`），Enemy 不生成
- **修仙:** 练气→筑基→金丹（`RealmLibrary`，突破永久加成累进），K 面板打坐——灵气公式 `RealmRuleService.ComputeQiGain`（玩家打坐 Tick 与 Worker 睡眠吐纳共用；2/s ×(1+内功修炼加成+聚灵阵科技)），受击/移动打断；打坐回蓝与内功回蓝共用 int Mp 按秒折算累计器模式（`mpRegenCarry`）
- **功法:** 3 内功（被动走统一管线 + 回蓝，同时仅激活一本）+ 2 外功（主动技）。`SkillManager` 槽位 0-7：默认 4 + `RegisterExtraSkill` 动态注册（幂等按 SkillId，SlotIndex=Skills.Count 递增，满 8 拒）；**技能不存档**——`GongFaManager`/`AwakenedPowerManager`（ITickable）Tick 检测玩家就绪后按 `LearnedGongFaIds` **学习序**懒重建注册（保槽位稳定）
- **异能:** 受击 roll 觉醒（基础 3% + 濒死加成至 10%，上限按各自 GrowthData 计），念力（拉怪，BuffDuration 复用为拉近距离）/火球（`SkillData.ScaleByInt` 选 INT 伤害基数，SingleTarget 无目标不扣蓝不进 CD）
- **Worker 成长接入（全自动，无 UI）:** ①睡觉即修炼——`WorkerSleepTask.Finish` 调 `CultivationManager.MeditateFor`（床睡全额/地面睡 ×0.5），被打断走 GiveUpTask 不 Finish 天然中断；②突破/内功全自动——`CultivationManager.Tick` 每 2s 扫描 `WorkerCharactersProvider` 静态缝，`BreakthroughData`（玩家/Worker 共用突破结算）+ `GongFaManager.AutoLearnNeiGongFor`（只学内功并自动运转最新一本，**绝不学外功**——外功注册进全局 SkillManager 会挤占玩家槽位）；③异能觉醒转被动——Worker 无技能栏，`AwakenedPowerDef.WorkerPassiveBonus` 入账 `PermanentRealmBonus` + 气泡反馈；④装备词条——Worker 拾取经 `EnemyLootManager.TakeDropInstanceByPos`（须在 `RemoveDropByMapPosition` 前取出）把掉落实例词条拷进穿戴实例；⑤修仙进度显示在 WorkerConditionHUD 每 worker「修炼」行。前置修复：`CharacterManager.LoadData` 读档后重连 `CharacterData.Character`（[NonSerialized]，不重连则 Worker 换装/成长重算被静默跳过）
- **生活技能（Worker）:** 伐木/采矿/农耕——`AWorkerTask.GrantedLifeSkill` 虚属性（Gather 按 `isTerrainDig` 分 Mining/Felling），Finish 统一 +XP，升级提进度倍率（1.0/1.15/1.3/1.5，`LifeSkillRuleService` 纯函数）；`ProgressMultiplierProvider(task, worker)` 已含该倍率；WorkerConditionHUD 每 worker 追加一行技能进度
- **科技:** `TechManager : ASingletonSaveData`（研究点/已研究列表自动存档，Ensure 兜底）；研究点 = 已建成 ResearchTable 数 × 时间（1 点/分/台，高级研究法 ×2），T 面板研究。**建筑解锁 gating 唯一收口 `ABuildItem.AddBuildTask`**（玩家放置入口；房间墙/农田自动建造走 `BuildMap.AddBuild` 不受限）。聚灵阵打坐 +50% 按有无不叠乘
- **新建筑三同约定:** 类名 == ItemData 条目 Name == Tile 资产名（`ItemInstanceFactory` 反射扫描 ABuildItem 子类查 `GetByName(type.Name)`，缺条目启动报错）；研究台/聚灵阵条目在 `BuildOtherItemData.asset`（Id 1100002/1100003），Tile 资产在 `Resources/Tilemap/Item/Build/`

### 商店与任务板系统
- **商店 NPC:** `ShopNPC` + `ShopNPCGenerator` — 地图就绪后自动生成商店，支持 Worker/Player 买卖交互
- **任务板:** `TaskBoardManager` — 地图中心固定位置，Worker 存取物品的中转站，内存字典存储
- **TaskBoardHUD:** 任务板 UI 面板，展示存取记录

### 房间系统
- **房间列表面板 (RoomListPanel):** 展示 RoomManager 中所有已建造房间及状态，IsOverlay 模式不暂停游戏
- **房间判定:** 射线检测封闭房间，计算温湿度

#### Worker 建房布局
- **建房流程:** Worker 围墙壁 → 门 → 床 → 4 格仓库，布局由 `WorkerBrain.GenerateRoomLayout` 依据 `HomeRoomWidth/Height/DoorSide/DoorIndex` 动态生成
- **房间尺寸:** 宽高 5~7（5×5 ~ 7×7），门在任意一边（doorSide 0-3）的非角位置
- **家具块 "高2横3":** 仓库 2×2 + 床 1×2，tile 空间 3 列 2 行；5×5 房间内部 3×3 恰好放下
- **床显示方向与足迹:** 床 sprite 永远竖向（上下）显示。物理足迹 = 主格 + 副格 tile-x+1（`BedSecondOffset`，tile 空间横放），转置到屏幕即竖向 1×2；碰撞瓦片注册、寻路与布局打印统一走该足迹（不再用 `ABuildItem.GetOccupiedPositions` 的 tile-y+1 逻辑副格，避免与 sprite 实际阻挡错位）
- **床位置:** `BedOffset=(furnLeft, furnBottom+2)`，床视觉固定在屏幕 `X=furnBottom+2`（右内侧）、`Y=furnLeft..furnLeft+1`，与仓库视觉 `X=furnBottom..furnBottom+1` 不相交
- **门避免堵家具:** `GenerateRandomRoomParams` 逐个候选门位验证"进门第一格"不落在家具占位（床主格/床副格/仓库）上（helper `IsDoorEntryBlockedByFurniture`）；全部门位被堵时换参数重试（最多 8 次），极端几何回退 7×7 可用组合
- **外墙外圈无不可采集碰撞体:** 建房选址 `CanFitRoom` 校验外墙体外一圈（墙外 1 格，含角）不存在"阻挡且不可采集/不可挖掘"的瓦片（水/其他建筑/墙等）；可通行、可采集资源（树/矿）、可挖掘地形（山）允许——Worker 建墙须站外圈，此类瓦片无法清除会导致建造卡死
- **日志级别:** 布局调试用 `LogManager.LogLevelEnum.Info` 才会输出到 Unity Console（Trace/Debug 仅写 game.log）

#### Worker 建房屋顶
- **生成:** 门建完 → `RegisterWorkerRoom` 注册房间后调 `RoofManager.Ensure().AddRoof`，Roof sprite 覆盖整个房间矩形，挂 `All/Building` 下，`sortingLayer=Highest`（盖住屋内一切）、无 Collider（纯视觉，不影响寻路/射线）
- **覆盖尺寸（转置陷阱）:** 世界坐标 = tile 45° 转置，`localScale=(roomHeight/10.24, roomWidth/10.24)`（Roof sprite 原始 1024px/PPU100=10.24 世界单位）；传参顺序不可换，否则覆盖矩形横竖颠倒
- **进出显隐:** `RoofManager.Update` 以本地玩家地图坐标判定——在房间内部（`RoomManager.GetRoomInterior`）**或站在该房间墙壁/门格上**（`RoomInfo.Points` 含墙）即隐藏屋顶，出房恢复；仅可见性切换时打 `[BuildDiag]` 日志
- **拆除清理:** 房间边界建筑（墙/门）被 `WorkerDemolishTask` 拆除 → `RoomManager.NotifyBuildingDemolished` 从 `Rooms` 摘除房间 + `RoofManager.RemoveRoof` 销毁屋顶；拆床/仓库等房内家具不影响房间。`RelocateHomeSite`/`ClearAbandonedBuildTilesCore` 只清未完成瓦片，不触发清理
- **屋顶间 y 排序:** 屋顶 SpriteRenderer 注册进 `WorldYSortManager` 参与 y 排序（层仍 `Highest`，盖住屋内一切的设计不变），多个房间屋顶之间按"视觉底端世界 y"分配唯一 `sortingOrder`（近处屋顶盖住远处），不再固定 order=0；移除时由 `LateUpdate` 懒清扫兜底

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

### Worker 三层架构（决策/活动/移动）
- **分层:** 决策层 `WorkerDecisionService`（接取管线：玩家悬赏→全局任务→自主决策，由 Seek 状态 OnEnter 单次调用）｜活动层 FSM 6 状态（Seek/Move/Work/Attack/Escape/Dead，`TypeEnum` 顺序存档兼容不可改）｜移动层 `WorkerLocomotion`（常驻服务，`AWorker.FixedUpdate` 先于状态逻辑驱动 `TickFixed`）。状态只声明移动意图（`GoTo/Chase/KeepDistance/Stop`），不直接消费寻路结果
- **任务写入唯一入口:** `AWorker.SetTask(task, source)`——五来源语义：`SelfDecision`（自建，不打断）/ `PushAssignment`（分配循环推送，置延迟打断）/ `ChainHandoff`（任务 Finish 栈内接力，置延迟打断）/ `BountyRestore`（恢复悬赏本体，已 Start 过不重启、绝不打断）/ `Clear`（置空）。禁止绕过 SetTask 直写 `workerData.Task`
- **延迟打断:** Push/Chain 来源只置 `HasPendingTaskInterrupt` 标记，`Update` 开头消费（非 Dead/Attack/Escape 时切 Seek 重寻路）——Finish/分配调用栈内绝不同步切状态
- **移动写入唯一出口:** `ASeek` 是唯一合法 velocity 写入点；战斗移动走 `MoveDirect`（同速度管线 + stuckDetector.Feed），不发起异步 A*——短距高频改向与异步寻路时序天然冲突，被墙挡按 Stuck 结算站定兜底
- **时序闸门:** Seek 状态等待寻路期间 `TickFixed` 绝不能驱动 `MoveByPath`（None 意图不驱动）——否则 `CompleteMovement` 清空路径结果，Seek.OnUpdate 误判"没有找到路"→ GiveUpTask 风暴
- **紧急生存检测:** `AWorker.CheckSurvivalEmergency`（每 10 帧，所有状态生效；Dead/Attack/Escape/对话暂停豁免，生存优先于延迟打断消费）——饥饿/疲劳/精气神/压力越阈强制 GiveUpTask，新决策由 ChangeState(Seek)→OnEnter 管线单次完成（无二次决策）
- **战斗移动（寻路驱动）:** Attack 状态按武器状态声明意图——超出攻击距离（×1.2）或冷却就绪→Chase；冷却中→KeepDistance（打带跑，`HitAndRunEnabled` 开关）；带内→站定挥砍。移动层执行：追击向目标当前格寻路；拉开向**背向目标扇形**（±60° 正后优先 × 距离档）采样的可走格寻路（正后是墙 A* 自动绕行），扇形全堵回退 `MoveDirect` 径向后撤；语义评估每帧优先于路径消费（进攻击距离/带内立即站定）；寻路提交节流 0.8s + 等待超时 2s；目标距离超 `CombatBreakRange=8`（对齐状态层续命判定）不再追/拉开，站定由状态层 1.5s 超时自然脱离战斗。战斗结束任务保持，回 Seek 走"有任务"分支重寻路继续

### 关键设计决策
- **45° 转置坐标系:** 世界坐标 = tile 坐标转置（`TileMap.MapPosToWorldPos`：world = (tile.y, tile.x)）。所有 tile↔世界转换遵循此规则；房间/家具/门的布局必须在转置后视角验证，否则床/门/墙会错位。注意方向错位：tile 顶墙=屏幕右墙、tile 右墙=屏幕顶墙、tile 左墙=屏幕底墙、tile 底墙=屏幕左墙
- **Domain 纯 C#:** 无 UnityEngine 引用，12 个接口定义在最内层
- **UnityAdapter:** 适配器实现 Domain 接口，通过 ServiceLocator 注册
- **Shared Kernel:** `LAB2D.Enum` 含 16 个跨层枚举(DDD 模式)
- **HUD 热键:** 通过 GlobalInit.Update() 统一分发，避免子对象 inactive 时失效
- **Worker 经济:** Domain 层纯 C# 值对象（CurrencyAmount / WorkerPersonality / WorkerGoal / BountyData），Gameplay 层 Manager 驱动运行时
- **好感度:** 规则纯函数在 Domain（`FavorabilityRuleService`，零 Unity 依赖可单测），运行时状态在 Gameplay（`FavorabilityManager`，仿 CurrencyManager：ASingletonSaveData + ITickable）；定向好感（Worker↔Worker/Worker→Player）懒初始化零预填
- **Worker 心智层:** 纯规则（无 LLM）叠加在好感度之上，行为调制在好感门控处汇合；拒绝双门（体验+权威，防优先级 0 绕过）；ML 41 维不动，拒绝/关系是模型之外决策门
- **Worker AI 自主决策:** WorkerBrain 在空闲时根据人格+目标+状态自主选择行动，而非被动等待任务分配
- **日志统一:** `GameLoggerFactory` 统一获取 `IGameLogger`，替换所有硬编码 `Debug.Log`（31 文件迁移）
- **TaskPriority 常量管理:** 任务优先级统一使用常量类（`TaskPriorityConstant`），避免魔法数字
- **UI 预制体加载:** 装备面板改用预制体加载（`ResourceManager.Instantiate`），替代硬编码 UI 层级

### 渲染排序（y-sort）
- **统一层:** 参与排序的 renderer 全部在 `Character` sorting layer（index 3，由 Worker 层改名而来，uniqueID 不变）。武器（`Item`）、寻路线/光束（`Highest`）、Tool 调试（`Enemy`）不参与。
- **排序:** `WorldYSortManager`（`Scripts/2D/Render/`，MonoBehaviour 单例，DontDestroyOnLoad）每帧 `LateUpdate` 按"视觉底端世界 y"降序分配唯一 `sortingOrder`（0..N-1）：底端 y 大（屏幕上方/远处）→ order 小（先绘制，被覆盖）；底端 y 小（下方/近处）→ order 大（后绘制，盖住）。`YSortAlgorithm.AssignOrders` 是纯函数，可单测。
- **底端 y:** `position.y + bottomOffset`；`bottomOffset = sprite.bounds.min.y - position.y`，仅 sprite 引用或 lossyScale 变化时重算（Player 换动画帧等），不做每帧 bounds。
- **注册:** 角色在 `Character.Start` 经 `YSortRegisterProvider` 注册；建筑/树/掉落物视觉由 `TileVisualSpawner`（`Scripts/2D/Map/`）创建时注册。注销不强制，LateUpdate 懒清扫已销毁 renderer（覆盖延迟销毁/Player 永不销毁）。
- **建筑/树视觉拆分:** `TileVisualSpawner` 把 tile 视觉拆到独立 SpriteRenderer（挂 host 下 `VisualSprites` 子节点，Character 层），参与 y 排序；Tilemap 保留碰撞体/寻路/数据/存档/网络，`TilemapRenderer` 在宿主（BuildMap/ResourceMap）Awake 禁用防双重渲染。多格物品副格（纯碰撞无 tile）不建视觉。
- **掉落物/仓库物品混合渲染（ItemMap）:** `ItemMap`（`Scripts/2D/Map/`）**不**禁用 TilemapRenderer——恒底层物品（`ItemData.LayerMode=Bottom` 默认）由 TilemapRenderer 直接渲染在 Map 上，不建 `ItemVisual_*`；非恒底层物品的 tile 保留数据/碰撞但颜色置透明（`SetColor alpha=0`，`useTilemapColor:false` 保证拆分出的 SpriteRenderer 不透明），视觉由独立 SpriteRenderer（Character 层 `ItemVisual_*`）参与 y 排序。写路径统一在 `AddTile`/`DeleteTile`/`SyncDataResp` 经 `ApplyTileVisual` 分流，先 `RemoveTileFlags(LockColor)` 再 `SetColor`（否则透明隐藏失效）。
- **分层模式:** `ItemData.LayerMode`（SO 枚举，**默认 Bottom**，`BuildItemData` 继承）三态统一控制"是否参与 y 排序"与"角色在后时是否淡化"：`Bottom` 恒底层、`Alpha` 参与 y 排序且淡化、`Normal` 参与 y 排序不淡化。恒底层语义两种呈现：BuildMap/ResourceMap 拆出的 SpriteRenderer 不注册进 `WorldYSortManager`，固定 `sortingOrder = WorldYSortManager.BottomLayerOrder`（-1000）；ItemMap 恒底层物品直接由 TilemapRenderer 渲染（Tile 层，低于 Character 层），根本不出 SpriteRenderer。判定委托：`GetBuildLayerMode` 经 `GetBuildTileData`→`GetBuildItemDataByName`（查不到兜底 `Alpha`）；`GetItemLayerMode`/`GetResourceLayerMode` 经 tile 名→`GetByName`（查不到返回 `ItemData.Empty`，`LayerMode` 默认 Bottom，按恒底层处理）。
- **地面帧动画:** `ItemData.IsAnimation`（SO 开关，**默认 false**，`BuildItemData` 继承）——开启且 `LayerMode != Bottom` 时，独立 SpriteRenderer 视觉挂 `SpriteFrameAnimator`（`Scripts/2D/Render/`），以物品英文名（`Name`）为前缀自动收集 `{Name}_0/_1/_2...` 序列 Sprite（经 `ResourceManager.TryGetImage` 静默探测，首个缺失即止、128 帧兜底），固定 6fps 循环播放；找不到任何帧时回退静态 tile 图。判定委托 `GetItemAnimationPrefix` / `GetResourceAnimationPrefix` / `GetBuildAnimationPrefix`（ItemMap 掉落物/仓库、ResourceMap 树资源、BuildMap 建筑三处 Map 通用；ItemMap/ResourceMap 经 tile 名→`GetByName`，BuildMap 经 `GetBuildTileData`→`GetBuildItemDataByName`，`IsAnimation && LayerMode != Bottom` 返回 `Name`，方向变体继承同一开关取变体自身 Name）。动画格 sprite 由组件接管（`CreateOrUpdate` 跳过静态 tile 图赋值）；预制体视觉（`VisualMode=Prefab`）由预制体自管，不受此开关影响。`TileVisualSpawner` 每次 `CreateOrUpdate` 以 resolver 结果调和组件状态（首次挂载 / 动画↔非动画切换 / 同格换物品重载帧序列）。帧收集逻辑 `SpriteFrameAnimator.CollectFrames` 为纯函数，单测 `SpriteFrameAnimatorTests`
- **遮挡淡化（OcclusionFader）:** `OcclusionFader`（`Scripts/2D/Render/`，MonoBehaviour 单例，`DefaultExecutionOrder(200)` 晚于 y-sort 读本帧最新 order）——玩家 `SpriteRenderer.bounds` 与环境视觉相交且遮挡物 `sortingOrder > playerOrder` 时，遮挡物 alpha 平滑渐变到 `OccludedAlpha`(0.3)，离开恢复原值；`FadeSpeed`(6/秒) 渐变，`CheckRadius`(6) 距离预过滤。候选遮挡物仅 `Alpha` 层视觉——`TileVisualSpawner.CreateOrUpdate` 创建时 `AddOccluder`、`Delete` 销毁时 `RemoveOccluder`；`Bottom`/`Normal` 不注册（恒底层无需淡化，Normal 走到后面不淡化）。
- **约束:** Character 层内 renderer 必须全部注册（未注册者 order 固定 0 会错乱）；唯一例外是 `LayerMode=Bottom` 恒底层视觉（固定负 order，恒在最底，不会错乱）。角色 layer 由 prefab 经 AB 包（`StreamingAssets/prefab`）加载——**改 prefab 层后必须重打 AB 包**（`工具/其他/打AB包`），否则加载到旧层导致排序按层隔离而失效。
