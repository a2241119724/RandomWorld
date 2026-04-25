# Agent Task Card

## 基本信息

- 任务 ID: F002-agent-context-scanner
- 创建时间: 2026-04-26
- 提出人: Codex 自动发现流程
- 当前状态: Done
- 风险等级: Low

## 原始候选

F002: Agent 上下文扫描器与模块索引报告。

来源信号:
- `Agent/Docs/ImplementationRoadmap.md` 阶段 1 推荐 `AgentContextScanner`，用于扫描 `Scripts/2D`、`Resources`、`Scenes` 并生成模块索引。
- 本轮扫描发现 `Agent/Reports/2026-04-26` 已有资源检查任务记录，说明后续 Agent 决策需要更稳定的历史任务卡入口。
- `Scripts/2D` 中存在 TODO、临时实现和 `NotImplementedException`，适合先沉淀为只读报告，不直接修改业务行为。
- 资源、存档、Photon、AssetBundle 都有高风险检查机会，适合先通过只读上下文报告集中暴露。

## 任务分类

- 任务类型: `editor_tooling` / `documentation`
- 负责 Agent: `tool` + `debug`
- 需要的 Skill: `editor_tool` + `document` + `resource_check` + `test`
- 目标: 新增一个 Unity Editor 只读扫描工具，输出 Agent 后续开发用的上下文索引报告。

## 影响路径

- `Scripts/2D/Editor/AgentContextScanner.cs`
- `Scripts/2D/Editor/AgentContextScanner.cs.meta`
- `Agent/Reports/2026-04-26/agent_context_scan.md`
- `Agent/Reports/2026-04-26/agent_context_scan.md.meta`
- `Agent/Reports/2026-04-26/feature_discovery.md`
- `Agent/Reports/2026-04-26/task_agent_context_scanner.md`
- `Agent/Reports/2026-04-26/task_agent_context_scanner.md.meta`

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

## 风险等级

Low。

本任务只新增 Editor 只读扫描器和 Markdown 记录，不修改 Scene、Prefab、ScriptableObject、StreamingAssets、存档结构、Photon 同步或 AssetBundle 内容。

## 执行步骤

1. 新增 `AgentContextScanner` Editor 工具，菜单路径为 `Tools/Agent/导出上下文扫描报告`。
2. 只读扫描 Agent 基础文件、历史任务卡、`Scripts/2D` 模块、TODO/临时/未实现信号、资源目录和高风险目录概况。
3. 将扫描结果导出到 `Agent/Reports/<yyyy-MM-dd>/agent_context_scan.md`。
4. 更新 `feature_discovery.md`，记录候选、选择原因和跳过的高风险候选。
5. 更新本任务卡结果区，记录修改文件、验证结果和剩余风险。

## 验证步骤

1. 静态检查: 确认新增脚本位于 `Scripts/2D/Editor`，只引用 Editor API 和文件只读扫描逻辑。
2. 高风险写入检查: 确认脚本不包含 `SetDirty`、`SaveAssets`、`DeleteAsset`、`MoveAsset`、`CopyAsset`、`CreateAsset`、`PrefabUtility`、`BuildPipeline`、`Photon`、`Archive`、`SaveData` 等写入或高风险行为。
3. 基本扫描逻辑: 使用命令行复核 Agent 文件、任务卡、TODO 信号、资源目录和 `.meta` 检查计数。
4. 编译相关检查: 尝试使用可用的 Unity/dotnet 编译能力；若当前命令环境无法运行 Unity 编译，记录原因。
5. Play Mode: 本任务为 Editor 只读工具，不要求 Play Mode 验证。

## 回滚方案

- 删除 `Scripts/2D/Editor/AgentContextScanner.cs` 和 `.meta`。
- 删除 `Agent/Reports/2026-04-26/agent_context_scan.md` 和 `.meta`。
- 如需回滚记录，恢复 `Agent/Reports/2026-04-26/feature_discovery.md` 和删除本任务卡。
- 回滚后确认 `Tools/Agent/导出上下文扫描报告` 菜单不再出现。

## 结果汇总

- 已完成:
  - 新增 `AgentContextScanner` Editor 只读扫描工具。
  - 新增菜单 `Tools/Agent/导出上下文扫描报告`。
  - 工具扫描 Agent 基础文件、历史任务卡、`Scripts/2D` 模块、TODO/临时/未实现信号、资源目录和高风险目录概况。
  - 输出路径固定为 `Assets/Agent/Reports/<yyyy-MM-dd>/agent_context_scan.md`。
  - 更新 `feature_discovery.md`，记录候选排序、自动选择和跳过的高风险候选。
- 修改的文件:
  - `Scripts/2D/Editor/AgentContextScanner.cs`
  - `Scripts/2D/Editor/AgentContextScanner.cs.meta`
  - `Agent/Reports/2026-04-26/feature_discovery.md`
  - `Agent/Reports/2026-04-26/task_agent_context_scanner.md`
  - `Agent/Reports/2026-04-26/task_agent_context_scanner.md.meta`
  - `Agent/Reports/2026-04-26/agent_context_scan.md`
  - `Agent/Reports/2026-04-26/agent_context_scan.md.meta`
- 验证结果:
  - 路径验证通过: 新脚本位于 `Scripts/2D/Editor`，报告位于 `Agent/Reports/2026-04-26`。
  - 高风险写入检查通过: 未发现 `SetDirty`、`SaveAssets`、`DeleteAsset`、`MoveAsset`、`CopyAsset`、`CreateAsset`、`PrefabUtility`、`BuildPipeline`、`PhotonNetwork`、`RPC`、`ArchiveManager`、`SaveData`、`LoadData` 等调用。
  - 结构静态检查通过: `AgentContextScanner.cs` 花括号数量匹配，当前 442 行。
  - 基本扫描逻辑复核通过: `Resources/SO` 14 个文件、`Resources/Tilemap` 76 个文件、`Resources/Images` 67 个文件，三者缺失 `.meta` 均为 0。
  - 高风险只读目录复核通过: `Scenes`、`StreamingAssets`、`AddressableAssetsData`、`ResourcesLocal` 均只统计文件/目录和 `.meta`，未写入。
  - TODO/临时信号复核通过: `Character` 6 条、`Core` 4 条、`Item` 5 条、`Map` 2 条、`UI` 1 条；扫描器自身已从信号统计中排除。
  - 编译未完成: `dotnet build ..\Assembly-CSharp-Editor.csproj --no-restore` 无法运行，原因是当前机器只安装 .NET runtime，没有 .NET SDK。
  - Unity batchmode 未运行: `Unity` / `Unity.exe` 不在 PATH，常见 Unity Hub 安装路径也未找到 `2022.3.62f2c1` 的 `Unity.exe`。
- 未完成项:
  - 未在 Unity Editor 中实际点击菜单生成报告。
  - 未执行 Unity 编译或 Play Mode。
- 剩余风险:
  - `File.ReadAllLines` 在 Unity Editor 中读取少数非 UTF-8 脚本时可能需要进一步兼容；当前项目脚本读取验证未发现阻塞。
  - 菜单实际显示和 `AssetDatabase.Refresh()` 仍需在 Unity Editor 中最终确认。
- 后续建议:
  - 下次打开 Unity 后执行 `Tools/Agent/导出上下文扫描报告`，确认 Editor 菜单与报告内容。
  - 下一张低风险任务卡可做任务卡自动生成器或存档字段兼容只读扫描报告。
