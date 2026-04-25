# Agent Task Card

## 基本信息

- 任务 ID：F001-item-resource-integrity-validator
- 创建时间：2026-04-26
- 提出人：Codex 自动发现流程
- 当前状态：Done
- 风险等级：Low

## 原始候选

F001：Item/SO/Tile/Image 资源绑定完整性检查器。

来源信号：

- `Agent/Docs/ImplementationRoadmap.md` 推荐首个任务为只读资源检查工具。
- `README.md` 标注可能缺少资源、必须上传 `.meta`、ItemData 与 Tile 存在名称关联绑定。
- `ItemInstanceFactory` 注释说明 `EnName == 对应类名 == 图片名`。
- `ResourceManager` 通过名称加载 SO、Tile、Image，名称不一致会在运行时变成 `not found`。
- 只读扫描已发现 SO `EnName` 与 Tile/Image 之间存在缺口。

## 任务分类

- 任务类型：`editor_tooling` / `resource_check`
- 目标模块：Item 数据与资源绑定检查
- 负责 Agent：`tool`、`resource`、`item_data`
- 需要的 Skill：`editor_tool`、`resource_check`、`document`、`test`

## 影响路径

- `Scripts/2D/Editor/ItemResourceIntegrityValidator.cs`
- `Agent/Reports/2026-04-26/resource_integrity_report.md`
- `Agent/Reports/2026-04-26/task_item_resource_integrity_validator.md`

## 不应触碰路径

- `Scenes`
- `Resources/SO`
- `Resources/Tilemap`
- `Resources/Images`
- `ResourcesLocal/Prefabs`
- `StreamingAssets`
- `AddressableAssetsData`
- `Scripts/2D/Manager/ArchiveManager.cs`
- `Scripts/2D/NetworkConnect.cs`
- `Scripts/2D/Tool/SyncDataTool.cs`

## 风险与约束

- 是否涉及 Scene：否
- 是否涉及 Prefab：否
- 是否涉及 ScriptableObject：只读扫描，不修改
- 是否涉及 AssetBundle/StreamingAssets：否
- 是否涉及存档格式：否
- 是否涉及 Photon/网络同步：否
- 是否需要兼容旧数据：否
- 边界：只能新增 Editor 只读工具与报告，不自动修复资源。

## 执行步骤

1. 在 `Scripts/2D/Editor` 新增 `ItemResourceIntegrityValidator`。
2. 使用 `AssetDatabase` 和只读文件扫描收集 `Resources/SO` 中的 `EnName`。
3. 扫描 `Resources/Tilemap/Item` 中的 `.asset` 文件名作为 Tile 名称。
4. 扫描 `Resources/Images/Item` 中的图片文件名作为 Image 名称。
5. 生成 Markdown 报告，列出统计、缺失 Tile、缺失 Image、重复 `EnName`、缺 `.meta`、原始 Item 记录。
6. 将报告输出到 `Agent/Reports/<yyyy-MM-dd>/resource_integrity_report.md`。

## 验证步骤

1. 静态检查：确认新增脚本路径在 `Scripts/2D/Editor`，只引用 Editor API，不进入运行时程序集。
2. 基本扫描逻辑：用只读脚本复核 SO `EnName`、Tile 文件名、Image 文件名和 `.meta` 检查结果。
3. 输出路径：确认报告路径为 `Agent/Reports/2026-04-26/resource_integrity_report.md`。
4. Unity 编译：如本机无法调用 Unity batchmode，则记录未验证原因。
5. Play Mode：本工具为 Editor 只读工具，不要求 Play Mode。

## 回滚方案

- 回滚路径：删除 `Scripts/2D/Editor/ItemResourceIntegrityValidator.cs` 及其 `.meta`。
- 报告回滚：删除 `Agent/Reports/2026-04-26/resource_integrity_report.md` 及其 `.meta`。
- 保留路径：`feature_discovery.md` 与本任务卡可作为流程记录保留。
- 回滚后验证：确认 `Tools/Data` 菜单中不再出现该检查器，项目无新增编译错误。

## 结果汇总

- 已完成：
  - 新增 `ItemResourceIntegrityValidator` Editor 工具。
  - 新增 Unity 菜单：`Tools/Data/导出Item资源绑定报告`。
  - 工具只读扫描 `Assets/Resources/SO`、`Assets/Resources/Tilemap/Item`、`Assets/Resources/Images/Item`。
  - 工具输出 Markdown 到 `Assets/Agent/Reports/<yyyy-MM-dd>/resource_integrity_report.md`。
  - 生成本次发现报告、任务卡和一次静态资源绑定报告。
- 修改的文件：
  - `Agent/Reports.meta`
  - `Agent/Reports/2026-04-26.meta`
  - `Agent/Reports/2026-04-26/feature_discovery.md`
  - `Agent/Reports/2026-04-26/feature_discovery.md.meta`
  - `Agent/Reports/2026-04-26/resource_integrity_report.md`
  - `Agent/Reports/2026-04-26/resource_integrity_report.md.meta`
  - `Agent/Reports/2026-04-26/task_item_resource_integrity_validator.md`
  - `Agent/Reports/2026-04-26/task_item_resource_integrity_validator.md.meta`
  - `Scripts/2D/Editor/ItemResourceIntegrityValidator.cs`
  - `Scripts/2D/Editor/ItemResourceIntegrityValidator.cs.meta`
- 验证结果：
  - 脚本路径验证通过：新增脚本位于 `Scripts/2D/Editor`，使用 `UnityEditor` 与 `MenuItem`，不会进入运行时逻辑。
  - 高风险写操作检查通过：脚本未出现 `SetDirty`、`SaveAssets`、`DeleteAsset`、`MoveAsset`、`CopyAsset`、`CreateAsset`、`PrefabUtility`、`BuildPipeline`、`Photon`、`Archive`、`LoadData`、`SaveData`。
  - 基础扫描逻辑复核通过：SO `EnName` 61 个，Item Tile 资源 58 个，Item Image 资源 29 个，缺失 Tile 绑定 18 个，缺失 Image 绑定 35 个，缺失 `.meta` 0 个。
  - 输出路径验证通过：已生成 `Agent/Reports/2026-04-26/resource_integrity_report.md` 作为本次只读扫描记录。
  - 语法静态检查通过：新增 C# 文件花括号数量匹配。
  - Unity 编译未验证：当前命令环境未找到 `Unity` / `Unity.exe`；`dotnet build` 不可用，原因是当前 `dotnet` 仅有运行时，没有 SDK。
  - Play Mode 未验证：本任务是 Editor 只读报告工具，不需要 Play Mode 才能验证核心边界。
- 未完成项：
  - 未在 Unity Editor 中实际点击菜单运行。
  - 未执行 Unity batchmode 编译。
- 剩余风险：
  - `SerializedObject` 对 SO 内嵌列表的遍历需在 Unity Editor 中最终确认一次。
  - 静态扫描发现的缺失 Tile/Image 只是命名绑定报告，不代表一定要立即补资源；部分资源可能通过共享图片或运行时规则兜底。
- 后续建议：
  - 在 Unity 中执行 `Tools/Data/导出Item资源绑定报告`，确认菜单报告与静态报告一致。
  - 下一张低风险任务卡可做“Agent 上下文扫描器”或“存档字段兼容只读扫描报告”。
  - 资源修复应单独开任务卡，不要在检查器任务中直接修改 `Resources`。
