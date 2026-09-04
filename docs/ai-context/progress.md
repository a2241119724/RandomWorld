# Progress

## Project Status

v0.2 方向已定（0.1.5 分支推进中）——**修仙小镇生存建造**："小镇为魂，防守为骨"。M1 循环闭环（时间服务/昼夜-波次耦合/山门核心胜负）、M2A（包 2.1 防守夜 Worker 响应 + 包 2.2 修仙事件接心智层）、M2B（包 3 敌人扩种+双妖兽+箭塔+防守接敌）、M3（包 2.3 心智+修仙 F12 面板 + 包 2.4 对话预设意图确定性结算/LLM 增强措辞）与 M4 灵气环境（包 2.5）代码均已完成；下一步 Play 实测各包行为 + M4 包 4 每局不一样与包 5 修仙深化穿插推进（大纲包 2-5）。

## Recent Changes

- 2026-09 — `feat(map)`: M4 包4 地图兴趣点轮 1 危险区全量——`DangerZoneRuleService` 撒点约束/距离场纯函数（距中心≥15 格、圆心距≥r1+r2 不重叠、区内移动×0.7/灵气×1.3）+ `DangerZoneManager` 灵脉同款三路径撒点（每图 2~3 区半径 10~14 格、区内撒 3~5 资源点「险地生灵物」）；接入：`Character.GetEffectiveMoveSpeed`+`Player.Move`/override 双入口减速、`LingQiManager.ComposeAt` 灵气叠乘+`LocalFactors.DangerZone` 分项（点格显示「险地×1.3」）；`ResourceMap.ReservePosition` 系统保留格防与 `GenResource` 逐格扫描撞格（Dictionary.Add 重复 key）；`DangerZoneGlow` 程序化暗紫毒雾常显（LingVeinGlow 管线）；8 单测。方案 `docs/open-issues/m4-p4-map-poi-plan.md`（轮 2 洞府静态/轮 3 探索闭环待做）
- 2026-09 — `feat(cultivation)`: M4 包4 出生灵根揭晓——灵根惰性生成后 `CultivationManager.Tick` 第一时间发开局仪式 Tip（`LingGenRuleService.FormatRevealMessage`：名称+稀有度后缀+`+20%/条`效果说明，纯函数）；`GrowthData.LingGenRevealed` 入档防重发（老档缺省 false 读档补发一次）；K 面板灵根文本改走 `FormatLingGenName` 单一来源消重复；+4 单测。spec 见成长系统节
- 2026-09 — `feat(modifier)`: M4 包4 每局修饰符「本局天机」——开局 roll 2~3 个（8 池 4 通道：灵气/敌方强度/工作速度/战利品，`SessionModifierRuleService` Fisher-Yates 确定性 Roll + `SessionModifierManager` FavorabilityManager 同款存档恢复/新档重 roll）；四接入点全部 TryGet 防御退化 1（灵气浓度合成末位乘、任务进度天气后叠乘、`WaveConfigModel.EnemyStrengthMultiplier` 数量+难度、敌方掉落两处 roll 补偿/缩放）；敌方强化自带战利品补偿通道（妖兽凶猛 1.25/1.40）防纯负面；开局 Tip + `SessionModifierHUD`（H 键，F 系已满）纯代码构建；13 单测。spec 见每局修饰符节
- 2026-09 — `feat(weather)`: M4 包4 事件天气灵雨/血月——Domain `WeatherType`+2 值与 `RollWeather` 加权池（40/25/15/12/8，`RandWeather` 均匀随机改加权）；灵雨=灵气恢复 ×1.5；血月=波次三强化（`WaveConfigModel.IsBloodMoon`：数量 ×1.5 取整/混池提前 1 波/难度 +0.5）+ 夜晚光色随暗度血红 tint（`GetGlobalLightColor` 重载，无相位跳变）；事件天气无视觉节点静默跳过；测试扩展 Weather/Wave 两文件 11 用例。spec 见天气系统节
- 2026-09 — `fix(bounty)`: BountyRestore 每 tick 刷屏（悬赏运行期 ~2400 条/人）——`WorkerBountyTask.Execute` 无条件恢复本体改 `Task == null` 才恢复，语义保留其余帧零开销。存档见 `bug-fixes.md`
- 2026-09 — `fix(item)`: 启动 Warning「没有名字为Seed0的道具」+ id=0 幽灵种子入包——种子 SO 合并为单条 Seed 后 `Seed0.cs` 成有类无条目孤儿，反射兜底注册进背包致 `GetByName` 落空；改 abstract 使 `GetChildByParent` 过滤跳过。存档见 `bug-fixes.md`
- 2026-09 — `feat(lingqi)`: M4 包2.5 灵气环境——空间浓度图 M=地形×灵脉×聚灵阵×天气（`LingQiRuleService` 纯函数 + `LingQiManager` 宿主：灵脉撒点入档三路径恢复、聚灵阵 2s 重扫不入档）；`ComputeQiGain` 加 envMultiplier 乘修炼速率（玩家打坐/Worker 睡眠吐纳采样位置浓度）；地形 SO 加 `qiDensityMultiplier`、科技聚灵阵重定义只解锁建造、EnvironmentManager 退化为浓度展示（点地分项选址工具）、`LingVeinGlow` 程序化灵脉光环。spec 见灵气环境系统节

- 2026-09 — `feat(defense)`: M2B 包3——敌人扩种协议（`WaveEnemyKind` 四种 + `WaveRuleService.PickEnemyKind` 确定性轮转，第 3 波起混池 Common/Seek/Charge/Shoot；存档 `EnemyKindId` 防读档换种，旧档缺省 Common）+ 冲锋野猪/远程妖狐（ASeekEnemy/CommonEnemy 系差异化数值，prefab 复制改造 + 512px PPU1280 素材）+ 箭塔建筑（`ArrowTower : ABuildItem` + `ArrowTowerManager` ITickable 节流扫描，技能直伤公式 `ReduceHp(null)` + Bullet 粒子纯视觉弹道绕开塔非 Character 的 Onwer NRE，SO 条目 1100004）+ 防守接敌治 M2A 罚站空转（站岗索敌有武器主动进攻击状态，复用 LastAttacker→AttackTarget 反击通路；修 NextDefendPosition 核心占用格兜底死循环改躲床位）。spec 见战斗系统节
- 2026-09 — `feat(light)`: 光影系统三阶段——A 昼夜循环（`DayNightLightManager` 运行时自建 GlobalLight + `DayNightRuleService` 强度/色温纯函数曲线，光照职责迁出 GameTimeUI）；B 点光源建筑 Torch/Campfire（`BuildItemData` 光照 4 字段数据驱动 + `TileVisualSpawner.lightResolver/SyncLight` + `LightFlicker` 闪烁；素材程序化顶视火焰 `tmp/gen_flame_sprites.py`——AI 文生图顶视火焰三连败后改程序生成，资产经 `工具/光源建筑资产生成` 一键接入）；C 角色椭圆软影（`BlobShadowProvider`，共享纹理 order -999）+ 玩家夜光环（`PlayerNightGlow` 相位事件淡入淡出）。spec 见光照系统节
- 2026-09 — `test(domain)`: 补三个缺口服务单测（ItemOwnership/TerrainEffect/WorkerSkillProgress，22 用例）+ spec 架构节记录 WorkerMindService 门面例外（Domain 目录内编排类，依赖 Unity/上层，纯规则已拆 RuleService）
- 2026-09 — `fix(ui)`: 仓库面板按钮只增不减+监听器闭包累积（TODO「删除Gameobject」落地）——OnEnter 先清多余按钮、复用按钮 RemoveAllListeners 再绑定
- 2026-09 — `fix(log)`: 消除两条启动期必现 Warning——ForegroundPanel Attack 死查找（按钮已移除+重复二次查找）与 `AItem.Ranges["Resource"]` 上界误含仓库占位值 Null（致 "NullItemData" 幽灵查询）
- 2026-09 — `chore(log)`: 清理诊断遗留刷屏——WalkabilityCache「未构建跳过」（正常路径+节流键含坐标失效，817 条/局）与 YSortRegister（注册早于赋值快照误导，811 条/局）删除；越界跳过/YSortTop 事件点保留
- 2026-09 — `fix(ui)`: GameTimeUI 全局光缺失防御——场景 GlobalLight 未激活致 Awake FindWithTag NRE（error.log 实锤），改全字段容错 + 5s 懒重试 + Update 前置守卫
- 2026-09 — `refactor(worker)`: WorkerBrain 巨石拆分阶段 1-3（3446→2255 行，零行为变化）——`WorkerDecision`/`WorkerHomeLayout`/`WorkerHomeSiteService` 三文件迁出，决策契约/布局纯函数/选址家族各自成域；阶段 4（环境扫描组）暂缓，方案见 `docs/open-issues/h1-workerbrain-refactor.md`
- 2026-09 — `refactor(build)`: 悬赏牌/商店收拢 ABuildItem 管线——Bounty/Shop 子类 + `PlaceBySystem` 系统放置，全库 DirectBuild 手写路径清零；Bounty 条目 `IsNeedBuild`→0（放置即完成，读档重放幂等）
- 2026-09 — `perf(worker)`: 100 Worker 帧率排查两波——主循环/寻路/UI 全链路热点；决策链零分配（ScanForResources PosMap 预过滤省 1681 格×2 Tilemap 互操作、scratch 键复用、ref 就地取最近候选、DropManager 无掉落快速退出）；LogProviderThrottled 增 Func 惰性求值重载（被节流不再构造插值串）；删 WorkerBountyDecisionService 死代码
- 2026-09 — `feat(combat)`: M1 循环闭环——**M1.1** 时间服务搬迁（`Domain/Time/DayNightRuleService` 纯函数 + `GameTimeManager` ITickable 自推进 + GameTimeUI 退化只读 + 跨天天气随迁）；**M1.2** 昼夜-波次耦合（波次 15s 固定间隔→每日一夜，挂 DayIndex）；**M1.3** 山门核心胜负（`MountainGateCore : ABuildItem` 3×3 系统放置建筑走既有建造管线——类名==瓦片名==SO 条目名、IsNeedBuild=true（放置后建任务，Worker 参与建核心）；`BuildingDamageRuleService` 建筑耐久纯函数；妖兽子弹经 AttackEffect 啃墙/核心；宽闸门失败曲线：核心 3 次被破终局失败+SessionResultManager 结算，满 3 级阶段胜利；Editor 菜单 工具/山门）。新增建筑一律 ABuildItem 子类原则入 CLAUDE.md §3
- 2026-09 — `feat(art)`: 山门核心/聚灵阵/研究台正俯视像素图并接线 tile 资产

- 2026-09 — `feat(growth-worker)`: Worker 成长接入（全自动）——睡觉即修炼（床睡全额/地面睡半额）、自动突破+自动修习内功（2s 扫描，只内功不外功）、异能觉醒转被动加成（PermanentRealmBonus 入账）、拾取装备保留词条（TakeDropInstanceByPos）；修仙面板热键 F8→K（F8 实测无效禁用）；修复 Worker 读档不重连 Character 反向引用致成长重算静默跳过。spec 见成长系统节 Worker 成长接入行
- 2026-09 — `feat(growth)`: 七大成长系统——统一 GrowthSource 属性管线（词条/内功/境界被动加成 + 吸血/反伤/回蓝特殊维度）、装备随机词条（掉落克隆修复）、修仙三境界+打坐突破（K）、武学功法 3 内功 2 外功（技能槽扩 8，Z/X/C/V）、异能觉醒（受击 roll）、Worker 生活技能（伐木/采矿/农耕）、科技研究（研究台/T 面板/建筑解锁 gating）。spec 见成长系统节
- 2026-08 — `refactor(worker)`: Worker 状态机三层解耦——决策服务（`WorkerDecisionService`）/活动层（FSM 只声明移动意图）/移动层（`WorkerLocomotion` 统一驱动），任务写入收口 `AWorker.SetTask(source)` + 延迟打断，紧急生存检测上移全状态生效，攻击接入移动（追击/风筝走位/打带跑，战斗结束任务保持）。spec 见 Worker 三层架构节
- 2026-08 — `feat(worker)`: Worker 生存与成长数值——压力/士气（困苦度动态）/熟练度（每任务+0.8，进度倍率联动）/贪婪懒惰（入 ML 特征 19/20 维），删除已取代的 WorkerMoraleController。spec 见 Worker 生存与成长数值节
- 2026-08 — `feat(worker)`: Worker 心智层完整实现（五阶段）——自主意志（拒绝/拖延/强制双门 + 怨恨感恩累积 + 强制命令伤感情）、事件记忆 + 信念演化、随机人生事件 + 执念、性格随经历演化（滞回/限流/饱和防横跳）、自发社会关系（友谊/敌意/爱慕/记仇 + 互助/回避/送礼/嫉妒四行为）。数据内嵌 `WorkerData.Mind` 随档一次写入（旧档 Ensure 兜底），7 套领域单测。spec 见心智层节
- 2026-08 — `feat(render)`: 物品地面帧动画——`ItemData.IsAnimation`（默认关）+ `SpriteFrameAnimator` 按英文名 `{Name}_0/1/2...` 序列图 6fps 循环播放（非恒底层独立视觉，预制体视觉自管）；`TileVisualSpawner` 加 `animationResolver`，每次 `CreateOrUpdate` 调和动画组件状态（挂载/切换/同格换物品重载）。ItemMap/ResourceMap/BuildMap 三处 Map 均接入。spec 见渲染排序节
- 2026-08 — `feat(render)`: 遮挡淡化 + 分层模式枚举——`OcclusionFader` 淡化挡住本地玩家的环境视觉（alpha→0.3）；`ItemData.LayerMode` 三态（Bottom 恒底层 / Alpha 参与排序且淡化 / Normal 参与排序不淡化）统一控制 y 排序与透明度。spec 见渲染排序节
- 2026-08 — `feat(render)`: Worker 屋顶接入 y 排序——`RoofManager` 创建的屋顶 SpriteRenderer 注册进 `WorldYSortManager`（层仍 `Highest` 盖住屋内一切），多个房间屋顶之间按"视觉底端世界 y"分配唯一 `sortingOrder`（近处盖远处），不再固定 order=0
- 2026-08 — `refactor(worker)`: 疲劳值语义反转——`CurTired` 从剩余体力式（初始=MaxTired、递减、低于阈值判定疲劳）反转为累积疲劳式（初始 0、工作/空闲累积、睡眠降低、疲劳 > `MaxTired-阈值` 判定需休息），20+ 文件阈值判断对称反转，AI 决策/睡眠/悬赏/状态机同步。旧档 `CurTired` 语义错位，睡一觉自愈（未做迁移）
- 2026-08 — `feat(worker home)`: Worker 建房屋顶——房间注册完成时 `RoofManager` 生成覆盖整个房间矩形的 Roof 屋顶（挂 All/Building、Highest 层、无碰撞），本地玩家进出房间隐藏/显示屋顶；拆除房间边界建筑（墙/门）时移除房间与屋顶
- 2026-08 — `feat(render)`: 掉落物/仓库物品（ItemMap）接入 y 排序——`ItemData` 上移 `LayerMode`（默认 Bottom，`BuildItemData` 继承），`ItemMap` 混合渲染：恒底层物品由 TilemapRenderer 直接渲染在 Map 上（不建视觉）、角色下；非恒底层物品 tile 置透明、单独创建 `ItemVisual_*` SpriteRenderer 参与动态排序。spec 见渲染排序节
- 2026-08 — `feat(favorability)`: 好感度系统——Worker↔Worker/Worker→Player 定向好感（0~100 初始 50），四项行为门控（玩家悬赏<35/Worker悬赏<40 拒接、交易<30 拒卖+价格乘数、对话态度入 LLM 提示词、协作互助），增减触发含攻击/致死/互殴/悬赏/交易/对话(日上限10)/接近共事(3s 扫描)，Mood 联动，F11 好感度 HUD。Domain 纯 C# 规则 + ASingletonSaveData 存档，单测 `FavorabilityRuleServiceTests`
- 2026-08 — `feat(render)`: 按图标底端世界 y 全局渲染排序（`WorldYSortManager` + `YSortAlgorithm` + `TileVisualSpawner`），角色/建筑/树统一 `Character` 层交叉排序；修复 Player 恒最顶（过期 AB 包内旧 sorting layer）。存档见 `docs/ai-context/bug-fixes.md`
- 2026-08 — `fix(worker home)`: 修复床副格碰撞与 sprite 足迹错位（寻路穿过床）+ 门位堵家具封死房间（含门规避重写与兜底回归修复）；Sliding 熔断复用 `HandleMovementStuck` 保留建造重试。存档见 `docs/ai-context/bug-fixes.md`
- 2026-08 — `fix(worker gather)`: 修复 Gather "没有邻居位置"死循环刷屏（25k 次）。根因：失败未调用 `ASeek.RecordFail`，决策 `ScanForResources` 的 `IsRecentFail` 过滤失效，GiveUpTask 释放认领后无限重选同一目标。修复：失败块补 `RecordFail` + Gather 邻居 2→4 正交方向。存档见 `docs/ai-context/bug-fixes.md`
- 2026-08 — `refactor(stuck-detection)`: 用每秒位移检测（MovementStuckDetector）替换失效的 OnCollisionStay2D 卡死检测（IntervalTicks 20ms 与 Fixed Timestep 0.02 相撞导致永不触发）；Worker+SeekEnemy 改由 ASeek.LastStuckResult 驱动，位移不足重寻路、真卡死重试/放弃
- 2026-08 — `refactor(worker home)`: 修复 Worker 建房布局中床与墙/门重叠问题，统一"高2横3"家具块，门 index 避开床所在行列，并修复 interiorW 硬编码 7
- 2026-08 — `refactor(worker build)`: 优化工人建造位置预留和任务恢复逻辑，新增建造者名称参数和自我预留跳过
- 2026-08 — `refactor(worker home build)`: 优化工人建家流程与碰撞逻辑，新增位置预注册机制
- 2026-08 — `refactor(worker)`: 新增建造任务恢复（重启时找回原建造者）和卡死重试逻辑（最多 3 次）
- 2026-08 — `feat(ui)`: 添加房间列表面板（RoomListPanel），展示所有已建造房间及状态
- 2026-08 — `feat(terrain)`: 添加地形挖掘功能，Worker 可挖掘可挖掘地形（如山），复用 GatherMap 认领机制
- 2026-08 — `feat(worker)`: 添加 Worker 自动使用血瓶逻辑（HP<30% 触发，3 秒冷却，战斗结束后/低血量检测时触发）
- 2026-08 — `refactor(worker-task)`: 统一重命名拾取相关任务类型与代码
- 2026-07 — `refactor(Map)`: 重构 GenAvailablePosMap，拆分螺旋搜索和全图随机逻辑
- 2026-07 — `refactor(worker AI)`: 重构工人 AI 与建筑系统，优化自主建造与资源管理
- 2026-07 — `refactor(itemmap/workertask)`: 优化物品拾取与工人任务流程
- 2026-07 — `feat`: 新增建造、种植相关 AI 与任务系统
- 2026-07 — `refactor`: 调整 UI 布局，为结构体添加可序列化特性
- 2026-07 — `feat(ui)`: 新增装备对比弹窗（EquipmentComparePopup），拾取装备时自动弹出对比面板
- 2026-07 — `feat(ui)`: 新增装备面板预制体，改用预制体加载 UI
- 2026-07 — `feat`: 添加商店系统，调整 Worker 士气和货币管理
- 2026-07 — `feat(ui)`: 新增 ScrollText 预制体，新增 TaskBoardHUD 面板
- 2026-07 — `feat(taskboard)`: 添加 TaskBoard 任务板系统和 Bounty 悬赏物品
- 2026-07 — `feat(gameplay)`: 添加 Worker 交易、Player 悬赏、商店 NPC 和 Worker 目标系统
- 2026-07 — `feat(worker)`: 添加 Worker 大脑 AI、物品所有权、个性系统和市场交易服务
- 2026-07 — `refactor(drop)`: 统一掉落物放置逻辑，支持可堆叠物品就近合并
- 2026-07 — `feat(worker)`: 添加 Worker 悬赏任务系统和货币管理
- 2026-07 — `feat`: 添加建造道具快速创建工具，支持 8 方向变体自动生成
- 2026-07 — `refactor`: 重构建造物品体系，移除旧的分层抽象类
- 2026-07 — `refactor`: 替换硬编码 Debug 日志为统一的 GameLoggerFactory 获取方式（31 文件）
- 2026-07 — `refactor(worker task)`: 重构任务优先级系统，统一使用常量管理
- 2026-09 — `feat(battle)`: 洛克王国式回合制战斗（B 键加入大世界交战：Domain 规则引擎 + 快照写回 + 战斗面板）

## Completed

| Phase | Description | Date |
|-------|-------------|------|
| 1 | 核心循环：白天采集/建造 + 夜晚波次防守 + 波后奖励 | 2026-06 |
| 2 | Worker 基础任务系统（8 种任务 + 优先级队列 + KD 树分配） | 2026-06 |
| 3 | Domain 层纯 C# 规则引擎（74 文件，零 Unity 依赖） | 2026-06 |
| 4 | 库存系统重构（Domain 层 InventoryService + 双索引 Grid） | 2026-06 |
| 5 | ServiceLocator DI + UnityAdapter 适配器层注册接入 | 2026-07 |
| 6 | Worker 经济系统（货币/市场/交易/悬赏/人格/目标） | 2026-07 |
| 7 | TaskBoard 任务板 + ShopNPC 商店系统 | 2026-07 |
| 8 | 建造系统重构（8 方向变体 + 快速创建工具） | 2026-07 |
| 9 | 装备对比弹窗 + 装备面板预制体 | 2026-07 |
| 10 | GameLoggerFactory 统一日志工厂（替换硬编码 Debug） | 2026-07 |
| 11 | 地形挖掘功能（Worker 挖掘可挖掘地形） | 2026-08 |
| 12 | Worker 血瓶自动使用（HP<30% 触发，3 秒冷却） | 2026-08 |
| 13 | 房间列表面板（RoomListPanel） | 2026-08 |
| 14 | Worker 建造任务恢复 + 卡死重试逻辑 | 2026-08 |
| 15 | 渲染排序：按视觉底端 y 全局排序（角色/建筑/树） | 2026-08 |
| 16 | 好感度系统（定向关系 + 四项行为门控 + Mood 联动 + HUD） | 2026-08 |
| 17 | Worker 心智层（自主意志/记忆信念/人生事件/性格演化/社会关系） | 2026-08 |
| 18 | Worker 生存与成长数值（压力/士气/熟练度/贪婪懒惰 + 进度倍率联动） | 2026-08 |
| 19 | Worker 状态机三层解耦（决策/活动/移动 + SetTask 收口 + 攻击移动） | 2026-08 |
| 20 | 七大成长系统（属性管线/词条/修仙/功法/异能/生活技能/科技） | 2026-09 |
| 21 | Worker 成长接入（睡觉修炼/自动突破/被动觉醒/词条拾取）+ 修仙热键改 K | 2026-09 |
| 22 | M1 循环闭环：时间服务搬迁 + 昼夜-波次耦合 + 山门核心建筑伤害与胜负 | 2026-09 |
| 23 | M2B 内容表现：敌人扩种协议 + 冲锋野猪/远程妖狐 + 箭塔 + 防守接敌 | 2026-09 |
| 24 | M4 灵气环境：空间浓度图（地形×灵脉×聚灵阵×天气）乘修炼速率 | 2026-09 |
| 25 | 回合制战斗（B 键加入大世界交战：五行克制规则引擎 + 快照写回 + 对战面板） | 2026-09 |

## Current Work

- **0.2 修仙小镇方向（五包路线）** — IN PROGRESS
  - [x] M1 循环闭环（时间服务/昼夜-波次耦合/山门核心胜负）
  - [x] M2A 包2.1/2.2 防守夜 Worker 响应（`DefenceDraftRuleService` 纯函数 + `WorkerDefendTask` + `WorkerDefenceManager` 入夜派发，觉醒优先参战）+ 修仙事件接心智层（突破/觉醒/工友嫉妒·敬仰走 RecordEvent）——代码完成，待 Play 实测行为分化
  - [x] M2B 包3 敌人扩种协议 + 前 2 种妖兽 + 箭塔 + 防守接敌——代码完成、编译 0 错误，待重打 AB 包 + Play 实测（混池出种/塔射击/接敌日志）
  - [x] M3 包2.3/2.4 心智面板 + LLM 对话结算（`WorkerMindPanel` F12 纯代码构建 + `DialogueIntentRuleService` 确定性结算 + `DialogueManager.ApplyIntent` 副作用 + `DialoguePanelUI` 4 意图按钮走 SendMessage LLM 增强措辞 + 单测）——代码完成、主程序集编译 0 错误，待 Play 实测（按钮行位置按 Message 顶边推算可能需手调、面板布局待过目）
  - [x] M4 包2.5 灵气环境——代码完成，待 Play 实测（灵脉撒点/光环/点地浓度分项/修炼增量乘浓度）
  - [ ] M4 包4 每局不一样（兴趣点/事件天气/局修饰符）
- **回合制战斗（洛克王国式）** — 代码完成、编译 0 错误，待 Play 实测
  - [x] Domain 规则引擎（SPD 定序/命中暴击/五行克制/冷却映射/逃跑/AI，纯函数 + 30+ 单测）
  - [x] Gameplay 编排（交战检测连通分量聚合 / 快照工厂 / 写回 / Manager 事件驱动，B 键入口 + 联机硬边界）
  - [x] 战斗面板 UI（TurnBattlePanel 非覆盖推栈冻结大世界 + TurnBattleUI 卡片舞台/菜单/演出 + BattlePromptHUD 提示条，纯代码构建）
  - [ ] Play 实测：引怪打 Worker→靠近出提示→B 进入→菜单战斗（技能耗蓝/冷却/克制飘字/演出）→胜利写回（经验/掉落）/逃跑（无敌帧）/战败（死亡管线）
- **Worker 建造系统优化** — IN PROGRESS
  - [x] 建造位置预注册机制
  - [x] 建造任务恢复（重启时找回原建造者）
  - [x] 建造卡死重试逻辑（最多 3 次）
  - [x] 位置预留冲突时的自我跳过逻辑
  - [ ] 建家碰撞逻辑进一步优化
    - [x] 卡死检测改为每秒位移（MovementStuckDetector），替换失效的碰撞回调检测（Worker + SeekEnemy；ACommonEnemy 保留原机制）
- **地图系统** — IN PROGRESS
  - [x] GenAvailablePosMap 拆分螺旋搜索和全图随机
  - [x] 地形挖掘功能
  - [ ] 地图生成算法进一步优化
- **Worker AI** — IN PROGRESS
  - [x] 优化自主建造与资源管理
  - [x] 优化物品拾取与任务流程
  - [x] 自动血瓶使用
  - [ ] Worker 大脑决策规则完善
- **UI 优化** — IN PROGRESS
  - [x] 场景 UI 颜色对齐 PixelUITheme
  - [x] ScrollText 预制体、TaskBoardHUD
  - [x] 房间列表面板
  - [x] 装备对比弹窗
  - [ ] 字体批量替换（Arial → ark-pixel）
  - [ ] Scale 规范化（0.5 → 1.0 补偿）

## Known Issues / Blockers

- Game.unity 场景文件 63K+ 行，大量运行时脚本依赖，无法安全手改 YAML，需 Editor 工具批处理
- Photon 多人联机同步覆盖不完整，部分新系统（TaskBoard/ShopNPC）尚未接入网络同步
- Domain 层仍有对 Enum/Serializable 的外部命名空间依赖（架构审查 #2 号问题）

## Technical Debt

| Item | Impact | Next Step |
|------|--------|-----------|
| Singleton 泛滥（30+ 个） | 紧耦合，测试困难 | 渐进迁移到 ServiceLocator 接口注入 |
| GlobalInit 上帝对象 | 初始化逻辑集中，难以拆分 | 提取独立 Bootstrap/Composition Root |
| AgentFull Python 框架在 Assets 内 | 非 Unity 运行时却占用项目空间 | 移出 Unity 项目到 Tools/ |
| 并发寻路 WalkabilityCache 非原子写入 | 潜在数据竞争 | 使用不可变快照替代原地刷新 |

## Next Steps

1. Play 实测：M2B（重打 AB 包后验证混池出种/箭塔射击/防守接敌）+ M2A 行为分化 + M3 面板布局
2. M4 收尾：灵气环境 Play 实测 + 包4 每局不一样（兴趣点/事件天气/局修饰符）
3. 旧欠账：Worker 建家碰撞优化、字体批量替换、TaskBoard/ShopNPC 网络同步

## Future Phases

- 光影阶段 D：2D 阴影遮挡（假投影暗斑保守方案 or ShadowCaster2D 先 spike 实证——URP 14 Point 光阴影支持性 + m_MaxShadowRenderTextureCount=1 + 大地图性能均未验证）
- 包5 修仙内容深化（元婴/化神境界、功法异能池扩充、炼丹炼器雏形、修炼-劳动经济张力调参）
- 多人合作模式完善（分工防守、资源共用）
- 敌人类型扩展（冲锋/拆墙/偷窃/远程/感染/Boss 变体）
- 天气事件升级（酸雨/暴雪/大雾/流星夜）
- 建筑联动加成系统
- 世界核心主线目标

## Deferred Work

- PvP 竞技模式（优先级低，先完善合作 PvE）
- 遗迹探索系统（依赖房间判定）
- 手机端适配（Android 已定义 PackageType，未实际适配）
