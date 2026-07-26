# Skill 能力清单

Skill 是 Agent 系统的可复用执行能力。每个 Skill 都应该明确输入、处理范围、输出格式和不可做事项。

## Skill 设计规范

- Skill 不直接决定任务优先级，优先级由主 Agent 和子 Agent 决定。
- Skill 输出必须包含影响路径、验证建议和残余风险。
- Skill 可以读取项目上下文，但不能隐式扩大修改范围。
- 同一任务中多个 Skill 可以串联，例如 `ErrorAnalyzeSkill -> CodeReviewSkill -> TestSkill`。
- 涉及 Scene、Prefab、ScriptableObject、StreamingAssets、存档和 Photon 的任务必须标记风险。

## 1. CodeReviewSkill

**用途**：检查 C# 脚本质量、命名规范、职责边界、生命周期、单例初始化、潜在 Bug 和耦合问题。

**适用路径**：

- `Scripts/2D/Character`
- `Scripts/2D/Item`
- `Scripts/2D/Map`
- `Scripts/2D/UI`
- `Scripts/2D/Manager`
- `Scripts/2D/Data`

**重点规则**：

- Unity 生命周期方法中避免隐藏的初始化顺序依赖。
- `Update` 中避免不必要的查找、分配和全量遍历。
- 单例 `Instance` 使用前检查 Awake/Start 顺序。
- Photon 同步代码必须区分本地实例和网络实例。
- 存档字段变更必须说明旧档兼容。

**输出**：问题列表、严重级别、文件路径、建议修复、测试建议。

## 2. ScriptGenerateSkill

**用途**：根据需求生成 MonoBehaviour、ScriptableObject、Editor 脚本、WorkerTask、CharacterState、MVC 类等脚本草案。

**当前项目模板优先级**：

- `AWorkerTask` 派生任务。
- `AWorkerState`、`ACommonEnemyState`、`ASeekEnemyState` 派生状态。
- `ABackpackItem`、`AConsumable`、`AFood`、`AEquipment` 派生物品。
- `ABuildItem`、`ARoom`、`AWall`、`ADoor`、`AFurniture` 派生建造物。
- `ABasePanel<T>` 派生面板。
- MVC 的 `Model`、`Controller`、`NavigationView`、`ItemView`、`InfoView`。
- `EditorWindow`、`MenuItem`、资源扫描器。

**输出**：脚本草案、命名建议、依赖清单、需要绑定的资源/组件。

## 3. ErrorAnalyzeSkill

**用途**：分析 Unity 控制台错误、编译错误、NullReference、MissingReference、资源缺失、Photon 错误和存档异常。

**输入**：

- 控制台日志。
- 堆栈信息。
- 复现步骤。
- 相关路径或类名。

**当前项目常见定位点**：

- `ResourceManager` 的 `prefab not found`、`asset not found`、`image not found`。
- `GlobalInit` 中 Panel/Manager 未初始化。
- `ArchiveManager` 的文件读写和 JSON 反序列化。
- `NetworkConnect` 的 Photon 连接、房间、同步请求。
- Scene 中 Missing Script、未绑定 UI 引用。

**输出**：根因候选、排查顺序、最小修复路径、验证步骤。

## 4. RefactorSkill

**用途**：对现有代码做结构优化，降低重复、拆分职责、抽象接口、整理命名和依赖方向。

**适用场景**：

- Worker 任务树扩展后任务类重复。
- Item 派生类和 SO 数据逐渐膨胀。
- Map 层存档/同步逻辑重复。
- UI Panel 与具体控件强耦合。
- ResourceManager 同时承担 Resources、AB 和 SO 索引。

**输出**：重构目标、阶段拆分、兼容策略、风险和验证。

## 5. SceneAnalyzeSkill

**用途**：分析 Scene 对象层级、组件绑定、Prefab 引用、Missing Script、UI 绑定和入口对象。

**适用场景**：

- Game 场景启动失败。
- Panel 按钮无响应。
- Prefab 实例化后组件缺失。
- Map 层对象没有正确绑定 Tilemap。
- PhotonView 或 Camera 配置异常。

**输出**：场景检查报告、对象路径、缺失组件、修复建议。

## 6. ResourceCheckSkill

**用途**：检查资源缺失、重复资源、命名规范、路径规范、引用丢失、SO/Tile/Prefab 关联。

**当前项目重点规则**：

- `Resources/SO` 中 ItemDataSO 名称不能冲突。
- Tile 名称需要和道具/地图数据规则匹配。
- Prefab 修改后需要重新打 AssetBundle。
- `StreamingAssets/Prefab` 中应包含运行时需要网络实例化或本地实例化的 Prefab。
- `Resources/Images/Item` 图标应和 Item 数据可追踪。
- 所有 Unity 资源应保留 `.meta`。

**输出**：资源清单、缺失列表、重复列表、命名冲突、AB 打包提醒。

## 7. PerformanceOptimizeSkill

**用途**：提出渲染、内存、物理、动画、UI、Tilemap、寻路、加载速度优化建议。

**当前项目重点指标**：

- Game 场景加载耗时。
- 存档加载耗时与 GC Alloc。
- Tilemap 批量更新耗时。
- AStar 寻路耗时与节点数。
- Worker 数量增长后的 `GlobalInit.Update` 成本。
- UI 背包/建造列表刷新成本。
- AssetBundle 和 Resources 首次加载耗时。

**输出**：性能风险、Profiler 标记建议、优化方案、验收指标。

## 8. ConfigGenerateSkill

**用途**：生成配置表结构、JSON 模板、ScriptableObject 数据类和校验规则。

**适用场景**：

- 新增 ItemDataSO。
- 新增 BuildItemDataSO。
- 新增 DropItemDataSO。
- 新增 WorkerTask 配置。
- 新增敌人/工人属性配置。
- 新增多存档元数据。

**输出**：字段定义、默认值、校验规则、迁移说明。

## 9. EditorToolSkill

**用途**：生成 Unity Editor 工具、批处理、Inspector 扩展和自动化菜单。

**推荐首批工具**：

- Agent Dashboard。
- SO 批量校验工具。
- Tile/Item 名称绑定检查工具。
- AssetBundle 内容检查工具。
- WorkerTask 模板生成器。
- 存档结构扫描器。
- Scene Missing Reference 检查器。

**输出**：Editor 脚本草案、菜单路径、使用步骤、注意事项。

## 10. TestSkill

**用途**：为关键逻辑设计测试用例、调试步骤、验证方法和验收标准。

**当前项目测试维度**：

- 离线新游戏、继续游戏、多存档切换。
- 联机创建房间、加入房间、地图同步。
- 工人任务入队、寻路、执行、取消、死亡/饥饿/疲劳中断。
- 背包物品拾取、使用、装备、丢弃、保存/加载。
- 建造物放置、房间判定、农田种植、资源采集。
- UI 面板打开/关闭、ESC 返回、拖拽和点击。
- Windows 构建后 `StreamingAssets/Prefab` 存在。

**输出**：测试清单、复现路径、验收标准、回归风险。

## 11. BuildFixSkill

**用途**：分析打包失败、运行资源缺失、平台差异、AB/StreamingAssets、URP 和 Photon 配置问题。

**当前项目重点**：

- Windows 包是否包含完整 `Build` 文件夹和 `StreamingAssets`。
- AssetBundle 名称与加载路径是否匹配。
- PhotonServerSettings 是否正确。
- WebGL 平台对文件 IO 和 Photon 的限制。
- Android 平台对 StreamingAssets 读取方式的差异。
- Shader/URP 设置是否随包生效。

**输出**：构建错误根因、平台差异说明、修复步骤、重新打包检查项。

## 12. DocumentSkill

**用途**：根据代码和功能生成开发文档、模块说明、使用说明和维护手册。

**推荐文档主题**：

- Worker 任务系统说明。
- Item/SO/Tile 绑定规则。
- Map 层级与存档同步说明。
- UI Panel 栈和 MVC 使用说明。
- 资源打包和 AB 更新流程。
- 多存档数据结构说明。
- Photon 同步规则说明。

**输出**：Markdown 文档、模块边界、流程说明、维护注意事项。

## 13. UIRefineSkill

**用途**：对 `Scenes/Game.unity` 和 `ResourcesLocal/Prefabs` 中的 UI 进行视觉审计与美学精炼。自动识别并优化字号层级、颜色语义、尺寸比例、间距对齐、视觉层次等。

**审计维度**：

- **字号层级**：标题/副标题/正文/辅助文字是否形成清晰的 3-4 级梯度。
- **颜色语义**：血量/魔法/金币/警告/成功/信息等语义色是否一致，对比度是否足够。
- **尺寸比例**：面板宽高比、按钮最小点击区、同类元素尺寸是否统一。
- **布局对齐**：GridLayoutGroup 参数、边距对称性、锚点正确性、层次缩进。
- **视觉层次**：前景/背景分离、焦点引导、留白、阴影/描边使用。

**适用路径**：

- `Scenes/Game.unity`
- `ResourcesLocal/Prefabs`
- `ResourcesLocal/Prefabs/ItemBox`
- `ResourcesLocal/Prefabs/Character`

**约束**：

- 不修改节点名称（除非确认无代码引用且命名混乱）。
- 不修改脚本挂载、事件绑定、Prefab 变体嵌套结构。
- 不新增或删除 UI 节点。
- 不修改动画、Animator、AnimationClip。
- 所有修改必须有回滚方案。

**输出**：视觉审计报告、问题分类列表、优化建议（含目标参数值）、优化任务卡、验证记录。

**推荐使用 Prompt**：`Agent/Prompt_UI.md`

