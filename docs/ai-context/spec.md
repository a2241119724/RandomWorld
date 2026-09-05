# Technical Specification

## Project Overview

RandomWorld 是一款 2D 像素风生存殖民地建设游戏。玩家在随机生成的地图上建立殖民地，管理工人完成建造、采集、搬运等任务，抵御波次敌人进攻，并通过 LLM 驱动的 NPC 对话系统与角色互动。

## Platform Support

- Windows PC (主要)
- Android (PackageType.Android 已定义)

## Core Features

### 殖民地管理
- **工人系统:** 招募工人执行建造、搬运、采集、种植、吃饭、睡觉、锻炼、穿戴、挖掘 9 种任务
- **任务队列:** 优先级队列(0-3)，每优先级字典线性扫描取最近可分配任务（15 帧节流，队列空早退零分配；`Core/KDTree` 存在但零调用）
- **补给监控:** 饥饿/疲劳状态机 (Healthy → Hungry → Tired → Exhausted → Critical)；疲劳 `CurTired` 为累积疲劳值（越大越疲，初始 0；工作/空闲累积、睡眠降低，空闲累积受天气/温度倍率加速），疲劳值 > `MaxTired - ThresholdTired`(20) 判定需休息——所有疲劳阈值判断方向是 `>` 而非 `<`
- **殖民地指挥中心 (F10):** 实时诊断报告 — 人力分析、任务阻塞原因、补给缺口、拥堵等级
- **建造任务恢复:** 游戏重启/场景加载后自动找回原建造者恢复建造任务
- **建造卡死重试:** `MovementStuckDetector` 每秒位移检测，累计位移 < 期望 40%（Sliding）先预防性重寻路，< 15%（Stuck）判硬卡死窗口，最多 3 次重试，避免任务误放弃
- **建造位置预注册:** 建造位置冲突时自我预留跳过，配合建造者名称参数实现任务恢复

### 战斗系统
- **角色空间索引（SpatialGrid）:** `Domain/Common/SpatialGrid<T>` 纯 C# 均匀网格哈希（cellSize=8=最大查询半径→桶覆盖恒 3×3），惰性全量重建（帧号脏检查，同帧多查询只建一次）。`EnemyManager.EnemyGrid`（重建时 `IsAliveEnemy` 过滤）与 `WorkerManager.WorkerGrid`（只过滤 null）持有。查询 API 带 `filter` 参数做查询时刻实时判活（网格是快照，`ReduceHp` 不挡已死目标）。索敌/邻近查询一律走网格（`SkillTool.GetEnemiesInRadius`/`GetNearestEnemyInRadius`），勿对 Characters 手写线性扫描；新增显著大于 8 的查询半径时重评 cellSize
- **防守夜 Worker 响应:** 入夜 `WorkerDefenceManager`（订阅 GamePhaseChangedEvent）按 `DefenceDraftRuleService.Decide` 纯函数打分三分行为——参战（核心旁待命位轮询驻守）/躲床（有家回 HomePosition、无家原地）/趁乱（当前位置周围随机可通行格溜边）；输入=人格四维+Greed+压力士气+玩家好感+觉醒+境界，觉醒者/高境界优先参战。派发前 `GiveUpTask` 无条件抢占（防旧任务队列占位/认领锁死）；任务时长=距黎明秒数到点自然 Finish；同游戏日防重派，山门核心未放置不部署。**生存打断补派（ITickable 2s 节流）**：防守任务被生存紧急打断（吃饭/睡觉/漫游）后，Worker 脱离生存任务即重新打分拉回防守（剩余时长，不弹气泡）——治参战者整夜人力流失；豁免与 `CheckSurvivalEmergency` 对齐（Eat/Sleep/GroundSleep/Wander 任务中、生存紧急中（`WorkerConditionRuleService.IsSurvivalEmergency` 纯函数单一来源）、Dead/Attack/Escape/对话暂停不补派，防派发-打断死循环）。参战待命期间 `WorkerDefendTask.Execute` 索敌（半径 8、0.5s 节流负随机错相，走敌人空间网格取最近零分配），持有武器即主动进攻击状态——复用被动反击通路（`LastAttacker`→`AttackTarget`，无武器自动转 Escape，打完回 Seek 任务保持续岗）；核心被围死无待命位时 Fight 退化躲床（原核心占用格兜底不可走致弃任务）
- **波次敌人（扩种协议）:** 普通波 + Boss 波(每 3 波)，难度渐进缩放。`WaveEnemyKind` 四种（Common/Seek/Charge/Shoot），`WaveRuleService.PickEnemyKind` 确定性轮转（同波次按 spawnIndex 取模，无 Random 可单测），第 `NewEnemyStartWave`(=3) 波起混池、之前只用旧池；`EnemyManager.Create(pos, kindId)` 经 `EnemyCreator` 静态映射选 prefab 并写存档 `EnemyData.EnemyKindId`（防读档换种，旧档缺省 0=Common）。冲锋野猪 ChargeBoar（ASeekEnemy 系：移速 3.2 + 刀光近战可拆墙，补旧 Seek 系不拆墙缺口）、远程妖狐 ShootFox（CommonEnemy 系复用状态机：射程 9 + 灵弹速度 60）
- **箭塔（防御建筑）:** `ArrowTower : ABuildItem`（1×1 玩家可建，SO 条目 1100005）。`ArrowTowerManager`（ITickable 仿 TechManager，2s 节流扫已建成塔）全塔 1.5s 统一开火：索敌半径 7 走敌人空间网格取最近（零分配+查询后 Hp 复查），直伤走技能公式（DEF/10 减免、下限 1；`ReduceHp(damage, null)`——塔非 Character，ReduceHp 对 null attacker 安全），弹道复用 Bullet 粒子纯视觉（不设 AttackTags/Onwer/Damage，粒子碰撞只 Stop 无伤害，绕开 Onwer NRE）；塔数据在 BuildMap 存档，Manager 无独立存档
- **主动技能 (8 槽):** 默认 Q/E/R/F（旋风斩、冲刺、力量爆发、治疗之光）+ 功法外功/异能动态注册槽 Z/X/C/V（见成长系统节）
- **连击系统:** 多阶连击伤害/经验加成
- **装备稀有度:** Common → Uncommon → Rare → Epic → Legendary → Mythic，属性倍率递增
- **死亡惩罚:** 经验损失 + 复活计时器
- **装备对比弹窗:** 拾取装备时自动弹出对比面板，显示当前装备 vs 新装备属性差异，支持替换/丢弃操作

### Worker 自动生存
- **血瓶自动使用:** HP 低于 30% 时自动消耗背包中的血瓶（3 秒冷却），战斗结束后和低血量检测时触发
- **自主交易:** 背包满时自动出售多余资源，饥饿时自主寻找食物卖家
- **自主决策:** 空闲时 WorkerBrain 根据人格/目标/状态自主选择行动；漫游路点 5% 发现随机基础资源掉落（`WanderDiscoveryRuleService` 纯函数均匀 roll Material 池 ×1~2，`ItemMap.PutDownToDrop` 附近可达格放置并自动创建公开搬运任务闭环拾取，SO/tile/落点缺失静默跳过）

### 地形系统
- **地形挖掘:** Worker 可挖掘可挖掘地形（如山），复用 GatherTask + GatherMap 认领机制防止多人同时挖掘
- **地形生成:** 程序化地形生成（7 种地形），随机散布 + 最近邻填充
- **地形配置:** TerrainTileConfig ScriptableObject，支持可行走/可建造/可生长树木/可生成资源配置

### LLM NPC 对话
- 本地 llama-server 驱动的 NPC 对话
- 短期记忆 + 长期记忆压缩(每 8 轮)
- RAG 游戏知识检索
- 对话期间工人任务暂停
- **预设意图结算（M3）:** `DialoguePanelUI` 底部 4 按钮（求教功法/安抚/道歉/赠礼）——点击先走 `DialogueManager.ApplyIntent` 本地纯规则结算（`DialogueIntentRuleService`），副作用即时入账（好感/心智/压力士气/灵气/金币/事件记忆），再把 `PlayerActionText`（含角色扮演引导）走 `SendMessage` 由 LLM 增强 NPC 回复措辞；LLM 不可用时 `FallbackReply` 气泡兜底。防刷：每意图每 Worker 每游戏日限（求教 1/安抚 1/道歉 1/赠礼 2，`Mind.DialogueIntentUses` 跨日重置）+ 赠礼 20 金币门 + 等待中置灰。求教成功门 = NPC 境界高于玩家且好感≥60（婉拒也计日限，防刷 LLM 调用）

### 天气系统
- 晴/雨/雪三种常规天气 + 事件天气灵雨/血月（M4 包 4「每局不一样」）
- 影响玩家/工人移动速度、任务进度、灵气浓度（`EnergyRecoveryMultiplier` 乘进浓度合成）
- 每日天气加权随机：`WeatherGameplayRuleService.RollWeather(rand)` 纯函数（晴 40/雨 25/雪 15/灵雨 12/血月 8），`GameTimeManager.DayRolloverAction → WeatherManager.RandWeather` 调用
- **灵雨**：仅灵气恢复 ×1.5，其余通道默认；无场景视觉节点（`WeatherManager.NoVisualWeathers` 静默跳过）
- **血月**：常规通道无差；当晚波次强化（`WaveConfigModel.IsBloodMoon` → 数量 ×1.5 向上取整、混池门槛提前 1 波、难度 +0.5，`WaveManager.CreateWaveConfigModel` 查天气填充）；夜晚全局光随夜色深度向血红偏移（`DayNightRuleService.GetGlobalLightColor(time, len, isBloodMoon)` 重载，tint 强度连续无相位跳变）

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
- **事件接入点:** 完成/接取悬赏、交易成败、被攻击、玩家救危、对话结束、修仙事件（突破者成就记忆+气泡 `CultivationManager.RecordBreakthroughMind`；工友旁观——`Greed≥阈值` 嫉妒记仇/境界低者敬仰爱慕，均带气泡）、异能觉醒 `AwakenedPowerManager`、地面睡眠（`WorkerSleepTask.Finish` 无床分支，自尊-2 无气泡）、拾获小确幸（`WorkerMindService.RecordFoundItem`，拾取任务高频故按游戏日节流每天首捡一次+气泡）等事件点调 `RecordEvent`（TargetName=`"PLAYER"` 哨兵或 Worker 名），绝不每帧循环；`WorkerMindConstant` 全部事件键均有生产者（`EVT_BOUNTY_REFUSED` 永久搁置未接——权威门拒绝已是心智状态表露，回写构成自反馈螺旋）；`WorkerMindManager.Tick`/`ProcessDayRollover` 驱动
- **反馈:** 拒绝理由/人生事件/关系变化统一走 `AWorker.ShowMindBubble`（语料 `WorkerInnerMonologue`，防被 `ShowRandomMonologue` 覆盖：进口气卫 + `HideDialogText` 清守卫）+ `[MindDiag]` Debug 日志 + `WorkerConditionHUD`「最近想法」行
- **可视化面板（M3）:** F12 开关 `WorkerMindPanel`（`GlobalInputProcessor` 分发，Overlay 不暂停）——左列 Worker 列表 + 右侧详情：好感/服从/怨恨/感恩、信念四轴、人格四维+贪婪懒惰+执念、修仙页（境界/灵气/灵根/内功/功法/异能）、关系网、记忆流（Day 降序前 12 条）。UI 骨架由 Game.unity 场景摆放（`WorkerMindUI` 挂 Panel 上，代码只 `BindReferences` 绑定 Content/EmptyHint 引用；行内容运行时动态生成），1.5s 节流刷新

### 成长系统（词条/功法/修仙/异能/灵根/生活技能/科技）
- **统一属性管线:** 被动加成源（装备词条/内功/境界永久加成）→ `GrowthBonusService.CollectFromData` → `GrowthSourceResult`（`Sources`: List&lt;BattleStats&gt; + `Special`: GrowthBonus）→ `AttributeCalculationService.ComputeFinalStats(growthSources)`。特殊维度（回蓝/吸血/反伤/修炼速度）存 `GrowthBonus.Special`（`ComputeAttribute` 写回快照），各系统在战斗事件点消费。**CRT/CSD 是 0-1 比例**（"+3%" 存 0.03，BattleStats 直接加整数会爆数值）
- **MaxHp 派生:** `MaxHp = BaseMaxHp + 成长加成`，由 `CharacterData.ComputeAttribute` 统一计算并钳制 Hp
- **GrowthData（成长容器）:** 随 CharacterData 二进制存档——灵根五行/境界/灵气/永久加成/`LearnedGongFaIds`/`ActiveNeiGongId`(string，空=未激活)/`AwakenedPowerIds`。`GrowthData.Ensure(ref)` 兜底（BinaryFormatter 不跑构造），玩家与 Worker 首次属性计算时生成灵根（终身不变；`ComputeAttribute` 传 `isPlayer || isWorker`），Enemy 不生成。**出生灵根揭晓（包 4）:** `CultivationManager.Tick` 每帧廉价检测（`TryRevealLingGen`），灵根惰性生成后第一时间发开局仪式 Tip（`LingGenRuleService.FormatRevealMessage` 纯函数：名称+稀有度后缀+效果说明），`LingGenRevealed` 标记入档防重发（老档缺省 false 读档补发一次）；K 面板灵根文本同走 `FormatLingGenName` 单一来源
- **修仙:** 凡人→练气→筑基→金丹→元婴→化神六境（`RealmLibrary`，包 5 扩——金丹 QiToNext 0→3600 老档自动可续突破，化神 `IsMax` 封顶），突破永久加成累进；K 面板打坐——灵气公式 `RealmRuleService.ComputeQiGain`（玩家打坐 Tick 与 Worker 睡眠吐纳共用；2/s ×(1+内功修炼加成) × 场景系数 × 环境浓度，见灵气环境系统节），受击/移动打断；打坐回蓝与内功回蓝共用 int Mp 按秒折算累计器模式（`mpRegenCarry`）
- **功法:** 4 内功（木/火/金/水，被动走统一管线 + 回蓝，同时仅激活一本）+ 2 外功（土/金主动技），五行齐。`SkillManager` 槽位 0-7：默认 4 + `RegisterExtraSkill` 动态注册（幂等按 SkillId，SlotIndex=Skills.Count 递增，满 8 拒）；**技能不存档**——`GongFaManager`/`AwakenedPowerManager`（ITickable）Tick 检测玩家就绪后按 `LearnedGongFaIds` **学习序**懒重建注册（保槽位稳定）
- **异能:** 受击 roll 觉醒（基础 3% + 濒死加成至 10%），觉醒池 6 条选 2（包 5 扩，`MaxAwakenedCount=2`）：念力（拉怪，BuffDuration 复用为拉近距离）/火球（`SkillData.ScaleByInt` 选 INT 伤害基数，SingleTarget 无目标不扣蓝不进 CD）/剑气风暴/金光遁/真元爆发/回春术——SkillId 复用 SkillManager 既有技能；Worker 无技能栏，觉醒转 `WorkerPassiveBonus` 被动
- **炼丹（包 5 雏形）:** `Domain/Gameplay/Alchemy`——`PillLibrary` 5 丹方（回气散聚气/培元丹治伤/凝神丹归元/渡劫丹破境辅助/九转金丹洗髓；成本用灵气，不引入药材物品）+ `PillRuleService` 纯函数（品质 roll 凡 60%/上 30%/极 10%，倍率 1.0/1.5/2.0）。落地宿主 `PillManager`：渡劫辅助以等效补灵气实现（`Qi += QiToNext×减免`，零新存档字段，突破扣全额时等效打折；化神巅峰禁炼防白扣）；洗髓走 `PermanentRealmBonus` 累进+属性重算。入口 Editor 菜单「工具/炼丹」（Play 模式）；K 面板按钮行待场景节点接入
- **Worker 成长接入（全自动，无 UI）:** ①睡觉即修炼——`WorkerSleepTask.Finish` 调 `CultivationManager.MeditateFor`（床睡全额/地面睡 ×0.5），被打断走 GiveUpTask 不 Finish 天然中断；②突破/内功全自动——`CultivationManager.Tick` 每 2s 扫描 `WorkerCharactersProvider` 静态缝，`BreakthroughData`（玩家/Worker 共用突破结算）+ `GongFaManager.AutoLearnNeiGongFor`（只学内功并自动运转最新一本，**绝不学外功**——外功注册进全局 SkillManager 会挤占玩家槽位）；③异能觉醒转被动——Worker 无技能栏，`AwakenedPowerDef.WorkerPassiveBonus` 入账 `PermanentRealmBonus` + 气泡反馈；④装备词条——Worker 拾取经 `EnemyLootManager.TakeDropInstanceByPos`（须在 `RemoveDropByMapPosition` 前取出）把掉落实例词条拷进穿戴实例；⑤修仙进度显示在 WorkerConditionHUD 每 worker「修炼」行。前置修复：`CharacterManager.LoadData` 读档后重连 `CharacterData.Character`（[NonSerialized]，不重连则 Worker 换装/成长重算被静默跳过）
- **生活技能（Worker）:** 伐木/采矿/农耕——`AWorkerTask.GrantedLifeSkill` 虚属性（Gather 按 `isTerrainDig` 分 Mining/Felling），Finish 统一 +XP，升级提进度倍率（1.0/1.15/1.3/1.5，`LifeSkillRuleService` 纯函数）；`ProgressMultiplierProvider(task, worker)` 已含该倍率；WorkerConditionHUD 每 worker 追加一行技能进度
- **科技:** `TechManager : ASingletonSaveData`（研究点/已研究列表自动存档，Ensure 兜底）；研究点 = 已建成 ResearchTable 数 × 时间（1 点/分/台，高级研究法 ×2），T 面板研究。**建筑解锁 gating 唯一收口 `ABuildItem.AddBuildTask`**（玩家放置入口；房间墙/农田自动建造走 `BuildMap.AddBuild` 不受限）
- **新建筑三同约定:** 类名 == ItemData 条目 Name == Tile 资产名（`ItemInstanceFactory` 反射扫描 ABuildItem 子类查 `GetByName(type.Name)`，缺条目启动报错）；条目集中在 `BuildOtherItemData.asset`（Bounty/Shop/ResearchTable/SpiritArray/MountainGateCore/ArrowTower），Tile 资产在 `Resources/Tilemap/Item/Build/`

### 灵气环境系统（M4）
- **浓度模型:** 空间浓度 M(pos) = T(地形) × V(灵脉) × A(聚灵阵) × W(天气) × S(每局修饰符)。合成纯函数 `LingQiRuleService`（`Domain/Gameplay/LingQi`，有单测）；运行时宿主 `LingQiManager`（Gameplay，ASingletonSaveData + ITickable + IInitializable，GlobalInit 注册）。浓度乘修炼速率（`ComputeQiGain` 的 `envMultiplier`，缺省 1 行为不变），基地选址在灵脉旁/聚灵阵覆盖区成为空间策略
- **灵脉:** 每图 8 条撒点（`GenCanReachPos` 可达 + 距地图中心 >15 格 + 脉间距 ≥25 格，重试 500 次容忍受限地图不足）；10 格欧氏距离内 ×1.5 单层不叠。点集入档（`LingQiManagerData.Veins`），三恢复路径：新图 OnMapReady 撒点 / 读档 LoadData 恢复 / 旧档迁移撒点（MapTiles 未就绪置 pendingScatter 等 OnMapReady 兜底）。视觉 `LingVeinGlow`：运行时程序化纹理（径向光晕+三道同心环+斜向亮斑，零 PNG 资产），sortingOrder -995 贴地装饰不参与 y 排序
- **聚灵阵:** 4 格欧氏距离内 ×1.3^min(n,3)（封顶防指数膨胀）。`LingQiManager.Tick` 2s 节流重扫 BuildMap 已建成 SpiritArray 主格（点集不入档，ArrowTowerManager 同款）。科技「聚灵阵」只解锁建造（`MeditateSpeedBonus=0`），局部加成由建筑本体提供；`SpiritArray` 类保持空壳（反射约定占位）
- **地形系数:** `TerrainTileConfig.effectData.qiDensityMultiplier`（SO，Range 0.1-3 默认 1，如雪 1.3/沙 0.7），`TerrainConfigDatabase.GetQiDensityMultiplier` 直通，漏配安全钳 ≥0
- **消费端:** 玩家打坐 `CultivationManager.Tick` 采样玩家位置（`GetDensityAtWorld`）；Worker 睡眠吐纳 `MeditateFor(posMap)` 采样睡觉位置（床的选址价值）。地图未就绪/越界返 1 安全降级
- **展示:** `EnvironmentManager` 只做浓度展示——Tick 采样玩家位置 `CurDensity`（100=草地基准）；点地显示被点格分项「地形×t 灵脉×v 阵×a 天气×w」（=1 省略段），作选址工具

### 每局修饰符（M4 包 4「每局不一样」）
- **模型:** 开局 roll 2~3 个全局修饰符（8 个池、4 通道：灵气 LingQiRecovery / 敌方强度 EnemyStrength / 工作速度 WorkerWorkSpeed / 战利品 EnemyLoot），整局生效、与事件天气正交叠乘。规则纯函数 `SessionModifierRuleService`（`Domain/Gameplay/SessionModifier`，有单测）；宿主 `SessionModifierManager`（Gameplay，ASingletonSaveData + IInitializable，FavorabilityManager 同款存档模式）
- **Roll:** Fisher-Yates 部分洗牌取前 count、按池序输出（同 seed 确定）；数值幅度 ±15%~40%；敌方强化自带补偿通道（妖兽凶猛 = 敌方 ×1.25 + 战利品 ×1.40，防纯负面体验）
- **接入点:** 灵气浓度合成（`LingQiManager.ComposeAt` 末位乘，`LocalFactors.SessionModifier` 分项可见）/ Worker 任务进度（`AWorkerTask.ProgressMultiplierProvider` 天气之后）/ 波次（`WaveConfigModel.EnemyStrengthMultiplier`，数量与难度同时缩放）/ 敌方掉落两处 roll（`EnemyLootManager`：通用「不掉落」按超出比例补偿重试 + 装备判定概率直接缩放）。全部 TryGet 防御，未初始化/测试环境退化 1
- **时序:** ArchiveManager LoadData（有档恢复 ids / 无档重 roll）→ GlobalInit Initialize（空则兜底 roll，幂等）；未知 id 读档丢弃（前向兼容删池项）
- **展示:** 开局 Tip「本局天机：…」；`SessionModifierHUD`（H 键，纯代码构建 EnsureRuntimePanel 模式，右上角山门 HUD 正下方）列出名/通道数值/描述

### 地图兴趣点（M4 包 4：危险区 + 上古洞府）
- **危险区（空间惩罚常显）:** `DangerZoneManager`（Gameplay，ASingletonSaveData + IInitializable）每图撒 2~3 区（圆心+半径 10~14 格，距中心 ≥15 格、圆心距 ≥r1+r2 不重叠，重试 500 次）；撒点约束/距离场纯函数 `DangerZoneRuleService`（`Domain/Gameplay/DangerZone`，有单测）。视觉 `DangerZoneGlow` 程序化暗紫毒雾常显（中心淡雾+环+角向絮状谐波，自转 3°/s）。惩罚：区内移动 ×0.7（`GetEffectiveMoveSpeed` 基类乘数链 + Player override 双入口，玩家/Worker 双角色生效；A* 不感知，Worker 路过变慢不绕路）；灵气 ×1.3（`LingQiManager.ComposeAt` 叠乘，`LocalFactors.DangerZone` 分项「险地×1.3」可见）。收益：区内圆拒绝采样撒 3~5 资源点（「险地生灵物」，`ResourceMap.ReservePosition` 保留格防与 GenResource 撞 Dictionary.Add）。点集入档三路径同灵脉（新图/读档/旧档迁移 pendingScatter 兜底）
- **上古洞府（探索型揭示 POI）:** `AncientCaveManager`（Gameplay，ASingletonSaveData + IInitializable + ITickable）每图撒 3~4 个（可达 + 距中心 ≥20 格 + 间距 ≥30 格）；规则纯函数 `AncientCaveRuleService`（撒点/揭示判定/淡入进度，有单测）。状态机 `CaveState`：Hidden（无视觉）→ Revealed（Tick 0.5s 节流扫描，玩家 ≤8 格单向揭示：程序化洞口暗斑+土金光环淡入 + Tip）→ Exploring（占用中，拒绝第二探索者）→ Explored（枯竭，视觉灰暗静止）。存档位置+状态；读档 Exploring 归一化 Revealed（任务不存档）
- **探索闭环（双通道）:** 玩家亲自——洞府 2 格内 `CaveExploreHUD` 提示条（纯代码构建，BattlePromptHUD 同款），N 键 30s 读条（移动/受击打断，打坐同款 `MeditateBreakMoveDistance` + `CharacterDamagedEvent`），`GlobalInputProcessor.ProcessCaveExplore` 分发（面板打开守卫同 JoinBattle）。派 Worker——O 键派最近空闲 Worker（`WorkerExploreTask`：DefendTask 同构驻留 60s 吃 ProgressMultiplierProvider 全乘数链，WorkerSpecific 指派者专属；Finish 结算、GiveUpTask 回滚占用）
- **风险/奖励结算（`CaveExploreRuleService` 纯函数，有单测）:** 风险 roll 独立于奖励 roll。35% 惊动妖兽（`EnemyManager.Create` 洞府旁 3 格 2~3 只 Common，白天遇敌走既有 AI 索敌）/ 10% 塌方（探索者 `ReduceHp(maxHp×15%)`）/ 55% 平安。奖励 40% 功法（`GongFaLibrary.All` 随机未学洗牌 `Learn`，境界/槽位不满足自动跳下一本）/ 25% 物资 + 25% 装备（`EnemyLootManager.ForceDrop` 包裹必掉，第 5 波档；ownerId=0 归玩家不入 Worker 拾取队列）/ 10% 双倍（功法+物资）

### 回合制战斗（洛克王国式）
- **入口:** `BattleEncounterDetector`（`TurnBattleManager.Detector`，由 `GlobalInputProcessor.ProcessJoinBattle` 每 Tick 驱动）0.5s 轮询大世界交战——Worker Attack 状态锁 Enemy 为主信号 + Enemy 侧 Target/LastAttacker 补扫，连通分量聚合交战对，2 轮滞回防闪烁。玩家 8 格内出现 `BattlePromptHUD` 提示条（"按 B 加入"），**B 键加入**（有面板打开/战斗中/联机时不触发，联机硬边界走 `GameServices.NetworkIsOnlineProvider()`）。`Player.Update` 入口以 `TurnBattleManager.IsActive` 拦截实时技能热键（Q/E/R/F 等在面板打开时仍会响应键盘）
- **面板:** `TurnBattlePanel`（非覆盖面板推栈自动 `timeScale=0` 冻结大世界 + Foreground `blocksRaycasts=false` 物理禁存档，Close 自动恢复）经 `Manager.BattleStarted` 事件打开（非 B 键直接 Show）。UI 纯代码构建 `TurnBattleUI`（卡片舞台/行动菜单/演出队列），演出用 `unscaledDeltaTime`（冻结下照常）；ESC 分层取消（点选目标→子菜单→逃跑确认框），无免费退出
- **快照-写回:** 开战 `TurnBattleUnitFactory` 快照参战者（玩家+交战 Worker 为我方 vs Enemy），战斗内一切结算只读写快照（规避 `ReduceHp` 副作用链：状态切换/仇恨/心智/好感/粒子），结束 `TurnBattleResultWriter` 一次性写回（面板关闭前、timeScale=0 下纯 C# 链安全）。写回规则：存活单位 Hp/Mp 钳制写回；Worker 倒下=重伤退场战后 1 HP（不走死亡管线）；玩家倒下=立即判负 `Player.DeathByTurnBattle`（拨回 lastDamageTime 绕无敌帧致死，走标准 Death 管线）；Enemy 倒下=`LastAttacker=player` 走标准 Dead 管线（经验/掉落归因玩家）；逃跑成功玩家获 2s 无敌帧（`GrantInvincibility`）
- **回合规则（`Domain/TurnBattle/TurnBattleRuleService` 纯函数，随机注入 `RandomFloatProvider`，未注入返回 0.5 中性值）:** SPD 降序定序（同速我方先，每回合重排）；命中率 `clamp(0.90+(攻HIT−守HIT)×0.5, 0.5, 1.0)`；暴击 roll<CRT（CRT 为 0-1 比例）；伤害=基数(ScaleByInt?INT:ATN)×倍率×(1+0.1×(等级−1))×AttackBuff→暴击×CSD→`DamageCalculator.ApplyDefense`→×五行克制；相克环 金→木→土→水→火→金（克 1.30/被克 0.75/中性 1.0，克制优先，`ElementCounterRuleService`）；技能冷却秒→回合（÷3 AwayFromZero，≤3s → 0 只耗蓝）；逃跑成功率 `0.50+双方均速差×0.03−敌存活×0.05+失败次数×0.15` clamp[0.25,0.95]，失败浪费一回合（敌方全体行动）；敌方 AI 选期望伤害最高技能（含克制，MP 不足兜底普攻），Worker 队友第一版只普攻
- **技能/道具快照映射:** 玩家 8 槽过滤 Movement/Pull（无回合制语义）；SelfAOE→敌方全体×0.6；普攻 0 耗蓝永可用（MP 不足兜底）。道具=背包血瓶：治疗量 `AddHp.HealAmount` 单一来源（Use 与回合制共用），扣背包走 `BackpackController.ConsumeConsumableByUid`（立即生效不退还），回血走快照不实时改大世界
- **交互:** 主菜单 攻击/技能/道具/逃跑（点击 + 数字键 1-4）；技能项显示耗蓝/冷却（不可用灰显）；多敌单体行动进入点选模式（敌方卡高亮，右键/ESC 取消）；演出点击加速（0.55s→0.12s/段）；面板内飘字 `TurnBattleFloatingText`（局部坐标 unscaled 上浮渐隐，不复用世界坐标 FloatingTextManager）

### 商店与任务板系统
- **商店 NPC:** `ShopNPC` + `ShopNPCGenerator` — 地图就绪后自动生成商店，支持 Worker/Player 买卖交互
- **任务板:** `TaskBoardManager` — 地图中心固定位置，Worker 存取物品的中转站，内存字典存储
- **TaskBoardHUD:** 任务板 UI 面板，展示存取记录

### 房间系统
- **房间列表面板 (RoomListPanel):** 展示 RoomManager 中所有已建造房间及状态，IsOverlay 模式不暂停游戏
- **房间判定:** 射线检测封闭房间，计算温湿度

#### Worker 建房布局
- **建房流程:** Worker 围墙壁 → 门 → 床 → 4 格仓库，布局由 `WorkerHomeLayout.GenerateRoomLayout` 依据 `HomeRoomWidth/Height/DoorSide/DoorIndex` 动态生成
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

> **已知例外:** `Domain/Worker/WorkerMindService.cs` 是心智层**编排门面**（非纯规则）——使用 `Time.time`/`Mathf`/`Random` 并引用 Character/Gameplay 层（AWorker/FavorabilityManager），不满足"零 Unity 依赖"；纯规则已拆至同目录 `*RuleService`（CommandAcceptance/Memory/Belief/Relationship/PersonalityDrift/LifeEvent/Dream，均纯 C# 有单测）。新增 Domain 代码勿效仿；门面自身依赖 Unity 编排是接受的现状。

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
- **统一层:** TagManager 仅 Default/Middle/Highest 三层；代码传入的 `"Character"` 层名不存在、Unity 静默回落 Default——全部世界物体实际同在 Default 层，交叉排序完全靠 `sortingOrder`。武器（`Item`）、寻路线/光束（`Highest`）、Tool 调试（`Enemy`）不参与。
- **排序:** `WorldYSortManager`（`Scripts/2D/Render/`，MonoBehaviour 单例，DontDestroyOnLoad）每帧 `LateUpdate` 按"视觉底端世界 y"降序分配唯一 `sortingOrder`（0..N-1）：底端 y 大（屏幕上方/远处）→ order 小（先绘制，被覆盖）；底端 y 小（下方/近处）→ order 大（后绘制，盖住）。`YSortAlgorithm.AssignOrders` 是纯函数，可单测。
- **底端 y:** `position.y + bottomOffset`；`bottomOffset = sprite.bounds.min.y - position.y`，仅 sprite 引用或 lossyScale 变化时重算（Player 换动画帧等），不做每帧 bounds。
- **注册:** 角色在 `Character.Start` 经 `YSortRegisterProvider` 注册；建筑/树/掉落物视觉由 `TileVisualSpawner`（`Scripts/2D/Map/`）创建时注册。注销不强制，LateUpdate 懒清扫已销毁 renderer（覆盖延迟销毁/Player 永不销毁）。
- **建筑/树视觉拆分:** `TileVisualSpawner` 把 tile 视觉拆到独立 SpriteRenderer（挂 host 下 `VisualSprites` 子节点，Default 层），参与 y 排序；Tilemap 保留碰撞体/寻路/数据/存档/网络，`TilemapRenderer` 在宿主（BuildMap/ResourceMap）Awake 禁用防双重渲染。多格物品副格（纯碰撞无 tile）不建视觉。
- **掉落物/仓库物品混合渲染（ItemMap）:** `ItemMap`（`Scripts/2D/Map/`）**不**禁用 TilemapRenderer——恒底层物品（`ItemData.LayerMode=Bottom` 默认）由 TilemapRenderer 直接渲染在 Map 上，不建 `ItemVisual_*`；非恒底层物品的 tile 保留数据/碰撞但颜色置透明（`SetColor alpha=0`，`useTilemapColor:false` 保证拆分出的 SpriteRenderer 不透明），视觉由独立 SpriteRenderer（Default 层 `ItemVisual_*`）参与 y 排序。写路径统一在 `AddTile`/`DeleteTile`/`SyncDataResp` 经 `ApplyTileVisual` 分流，先 `RemoveTileFlags(LockColor)` 再 `SetColor`（否则透明隐藏失效）。
- **分层模式:** `ItemData.LayerMode`（SO 枚举，**默认 Bottom**，`BuildItemData` 继承）三态统一控制"是否参与 y 排序"与"角色在后时是否淡化"：`Bottom` 恒底层、`Alpha` 参与 y 排序且淡化、`Normal` 参与 y 排序不淡化。恒底层语义两种呈现：BuildMap/ResourceMap 拆出的 SpriteRenderer 不注册进 `WorldYSortManager`，固定 `sortingOrder = WorldYSortManager.BottomLayerOrder`（-1000）；ItemMap 恒底层物品直接由 TilemapRenderer 渲染（恒底层，不参与 y-sort），根本不出 SpriteRenderer。判定委托：`GetBuildLayerMode` 经 `GetBuildTileData`→`GetBuildItemDataByName`（查不到兜底 `Alpha`）；`GetItemLayerMode`/`GetResourceLayerMode` 经 tile 名→`GetByName`（查不到返回 `ItemData.Empty`，`LayerMode` 默认 Bottom，按恒底层处理）。
- **地面帧动画:** `ItemData.IsAnimation`（SO 开关，**默认 false**，`BuildItemData` 继承）——开启且 `LayerMode != Bottom` 时，独立 SpriteRenderer 视觉挂 `SpriteFrameAnimator`（`Scripts/2D/Render/`），以物品英文名（`Name`）为前缀自动收集 `{Name}_0/_1/_2...` 序列 Sprite（经 `ResourceManager.TryGetImage` 静默探测，首个缺失即止、128 帧兜底），固定 6fps 循环播放；找不到任何帧时回退静态 tile 图。判定委托 `GetItemAnimationPrefix` / `GetResourceAnimationPrefix` / `GetBuildAnimationPrefix`（ItemMap 掉落物/仓库、ResourceMap 树资源、BuildMap 建筑三处 Map 通用；ItemMap/ResourceMap 经 tile 名→`GetByName`，BuildMap 经 `GetBuildTileData`→`GetBuildItemDataByName`，`IsAnimation && LayerMode != Bottom` 返回 `Name`，方向变体继承同一开关取变体自身 Name）。动画格 sprite 由组件接管（`CreateOrUpdate` 跳过静态 tile 图赋值）；预制体视觉（`VisualMode=Prefab`）由预制体自管，不受此开关影响。`TileVisualSpawner` 每次 `CreateOrUpdate` 以 resolver 结果调和组件状态（首次挂载 / 动画↔非动画切换 / 同格换物品重载帧序列）。帧收集逻辑 `SpriteFrameAnimator.CollectFrames` 为纯函数，单测 `SpriteFrameAnimatorTests`
- **遮挡淡化（OcclusionFader）:** `OcclusionFader`（`Scripts/2D/Render/`，MonoBehaviour 单例，`DefaultExecutionOrder(200)` 晚于 y-sort 读本帧最新 order）——玩家 `SpriteRenderer.bounds` 与环境视觉相交且遮挡物 `sortingOrder > playerOrder` 时，遮挡物 alpha 平滑渐变到 `OccludedAlpha`(0.3)，离开恢复原值；`FadeSpeed`(6/秒) 渐变，`CheckRadius`(6) 距离预过滤。候选遮挡物仅 `Alpha` 层视觉——`TileVisualSpawner.CreateOrUpdate` 创建时 `AddOccluder`、`Delete` 销毁时 `RemoveOccluder`；`Bottom`/`Normal` 不注册（恒底层无需淡化，Normal 走到后面不淡化）。
- **约束:** 参与 y-sort 的 renderer 必须全部注册（未注册者 order 固定 0 会错乱）；唯一例外是 `LayerMode=Bottom` 恒底层视觉（固定负 order，恒在最底，不会错乱）。角色 layer 由 prefab 经 AB 包（`StreamingAssets/prefab`）加载——**改 prefab 层后必须重打 AB 包**（`工具/其他/打AB包`），否则加载到旧层导致排序按层隔离而失效。

### 光照系统（URP 2D）
- **管线:** URP 14 + 2D Renderer（`Assets/URP/`），sprite 默认材质 Sprite-Lit-Default（未显式指定材质即受光）。无激活 Light2D 时 Lit sprite 显示原色；激活任意光后进入真光照计算——全局光正午强度必须 ≈1.0 才与无光时代视觉一致。
- **昼夜循环:** `DayNightLightManager`（`Scripts/2D/Manager/`，ITickable，TickableList 中位于 GameTimeManager 之后）采样 `DayNightRuleService.GetGlobalLightIntensity/GetGlobalLightColor`（强度 sin 曲线重映射 [0.35,1.0]、色温关键帧线性插值，纯函数有单测）驱动全局光。全局光 GO 运行时自建（tag=GlobalLight，FindWithTag 失败即建，跨场景销毁后 Unity 假 null 触发重建）；强度/颜色变化超阈值 0.005 才写（Light2D setter 置脏光照纹理）。时间 UI 只显示文本，不驱动光照。
- **点光源建筑（数据驱动）:** `BuildItemData.LightRadius/Intensity/Color/Flicker`（Radius=0 不发光）；链路 `BuildMap.GetBuildLightConfig`（`IsComplete` 才出配置，建造中不发光）→ `TileVisualSpawner.SyncLight`（构造器 `lightResolver` 委托，幂等调和，光 GO 挂视觉 GO 下随建筑销毁）→ `LightFlicker`（双频 sin 叠加 ±10%，实例随机相位防同步闪）。仅 `LayerMode != Bottom` 生效（恒底层不发光）。Light2D 不注册 y-sort/OcclusionFader。火光穿墙穿屋顶是接受的现状（真实遮挡=阶段 D 未实施）。
- **光源建筑资产:** Torch（LightRadius 2.5，木头×2）/ Campfire（4.0，木头×5+石头×2），素材为程序化顶视像素火焰（`tmp/gen_flame_sprites.py`：同心色环 + 噪声相位旋转 90°/帧，4 帧循环闭合、面积恒定；AI 文生图对顶视火焰全部失败——立式火舌先验压不掉）。资产接入 `工具/光源建筑资产生成`（`BuildLightAssetGenerator`：TextureImporter 设单图 Sprite 100PPU → tile/anim（PPtr 循环）/controller（单状态名=Name）→ SO 条目 SerializedProperty 追加，幂等可重跑）。
- **角色影子:** `Character.Start` 经 `BlobShadowProvider` 创建子 GO「BlobShadow」：`ShadowTextureFactory` 全角色共享 64×64 径向渐变纹理，scale (0.9,0.45) 压椭圆，localPosition (0,-0.12)，`sortingOrder = BottomLayerOrder+1`(-999)——地表(-1000)之上、全部动态(≥0)之下；不注册 y-sort（固定 order 无层内错乱）。
- **玩家夜光环:** `PlayerNightGlow`（`Player.Start` 运行时创建子 GO 挂载，不碰 prefab/AB 包）订阅 `GamePhaseChangedEvent`：Night/Dusk→0.55、Day/Dawn→0，`MoveTowards` 线性 ~2s；夜里进游戏按 `GameTimeManager.CurrentPhase` 直接亮（不等切换事件）。`PlayerNightGlow.Enabled` 静态开关。
- **预算:** `Light_Renderer.asset` m_MaxLightRenderTextureCount=16（同屏点光源上限，半分辨率 RT）；光源稀疏为默认策略，超限再提预算。
