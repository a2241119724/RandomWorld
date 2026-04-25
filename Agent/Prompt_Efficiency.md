请基于 ./Agent 中的 Agent 体系，自动完成一次“发现功能缺口 -> 选择最适合的候选 -> 生成任务卡 -> 实现 -> 验证记录 -> 更新候选完成状态”的完整流程。

重要要求：
- 不要向我提问。
- 不要等待我确认。
- 如果遇到需要人工确认的高风险候选，请自动跳过它，选择下一个低风险或中风险候选。
- 一次只实现一个功能。
- 候选功能总表必须统一维护在：
  - Agent/Reports/efficiency_discovery.md
- 不要在每个任务目录下重复创建 efficiency_discovery.md。
- 每次任务必须在 Agent/Reports/<今天日期>/ 下创建独立任务文件夹，任务卡、验证记录、补充报告等本次任务相关输出都必须放入该文件夹中，避免和同一天其他任务混在一起。
- 效率/工具类任务的输出目录必须使用固定前缀 `efficiency_`，用于和 Prompt_Feature.md 的 `feature_` 输出隔离，避免同一天同候选ID或同短名任务发生路径冲突。
- 独立任务文件夹命名格式建议为：
  - Agent/Reports/<今天日期>/efficiency_<候选ID>_<功能名安全短名>/
  - 如果在选择候选前无法确定候选ID，则先使用：
    - Agent/Reports/<今天日期>/efficiency_run_<HHmmss>/
    - 选定候选后，可继续使用该目录，也可重命名为 `efficiency_<候选ID>_<功能名安全短名>`。
- 同一天多次执行时，不得覆盖已有任务目录；如目录已存在，自动追加时间戳或序号。
- 优先选择低风险、高价值、边界清晰、能提升后续开发效率的功能，例如只读扫描器、Editor 工具、报告生成器、模板生成器、资源完整性检查器。
- 不要优先选择会直接修改 Scene、Prefab、ScriptableObject、StreamingAssets、存档结构、Photon 同步或 AssetBundle 的功能。
- 如果所有候选都属于高风险，则只实现一个不改业务资产的只读分析/报告工具。
- 生成候选功能列表时，必须为每个候选分配唯一编号和完成状态，便于后续判断该候选是否已经处理完成。
- 完成功能实现与验证后，必须回写 Agent/Reports/efficiency_discovery.md 中对应候选的状态标记。
- 扫描历史记录时，必须递归检查 Agent/Reports/ 下所有日期目录及其子任务目录中的 task_*.md 和 validation_*.md。
- 同时必须读取 Agent/Reports/efficiency_discovery.md，避免重复实现已经 `[DONE]` 的候选。
- 如果历史任务目录中遗留存在旧版 efficiency_discovery.md，也需要兼容读取，但新的候选总表只允许写入 Agent/Reports/efficiency_discovery.md。

候选状态规则：
- `[TODO]`：待处理，尚未实现。
- `[DONE]`：已完成，已实现并完成可行验证。
- `[SKIPPED]`：已跳过，通常因为高风险、边界不清晰、需要人工确认或涉及敏感资源修改。
- `[BLOCKED]`：受阻，已分析但因缺少环境、依赖、权限或无法验证而未完成。
- `[PARTIAL]`：部分完成，已实现部分能力，但仍存在明确未完成项。

候选功能列表格式必须包含：

| 状态 | 候选ID | 功能名称 | 来源信号 | 价值 | 风险 | 成本 | 优先级 | 推荐 Agent | 推荐 Skill | 处理说明 |
|---|---|---|---|---|---|---|---|---|---|---|

示例：

| [TODO] | F001 | 资源引用完整性只读扫描器 | Resources/SO 中存在未校验引用 | 高 | 低 | 中 | P0 | ResourceAuditAgent | ReadOnlyScanSkill | 推荐优先实现 |
| [SKIPPED] | F002 | 自动修复 Prefab 缺失绑定 | Prefab 存在 Missing Reference | 高 | 高 | 高 | P1 | ResourceFixAgent | PrefabModifySkill | 涉及 Prefab 直接修改，跳过 |
| [DONE] | F003 | TODO/FIXME 报告生成器 | Scripts/2D 存在 TODO/FIXME | 中 | 低 | 低 | P1 | CodeAuditAgent | ReportGenerateSkill | 已完成；任务卡：Agent/Reports/2026-04-26/efficiency_F003_TODO_FIXME_Report/task_efficiency_F003_TODO_FIXME_Report.md；验证记录：Agent/Reports/2026-04-26/efficiency_F003_TODO_FIXME_Report/validation_efficiency_F003.md |

执行步骤：

1. 读取并理解以下文件：
   - Agent/README.md
   - Agent/Docs/ImplementationRoadmap.md
   - Agent/Docs/SkillCatalog.md
   - Agent/Config/agent_registry.json
   - Agent/Config/task_router.json
   - Agent/Templates/agent_task_card.md

2. 读取全局候选功能发现报告：
   - Agent/Reports/efficiency_discovery.md

   如果该文件不存在，则自动创建。
   如果该文件已存在，则必须读取其中已有候选，尤其是 `[DONE]`、`[SKIPPED]`、`[BLOCKED]` 和 `[PARTIAL]` 状态，避免重复生成或重复实现同一功能。

3. 只读扫描项目上下文，重点检查：
   - README、Agent 文档、历史任务卡中的后续建议
   - Scripts/2D 中的 TODO、FIXME、空方法、临时实现、重复模式
   - Resources/SO、Resources/Tilemap、Resources/Images 中的资源绑定缺口
   - 已有系统中“有数据但无 UI / 有 UI 但无行为 / 有行为但无验证 / 有资源但无检查”的半完成链路
   - 存档、Photon、AssetBundle、资源引用等高风险区域的只读检查机会
   - Agent/Reports/efficiency_discovery.md 中已有的候选功能状态
   - Agent/Reports/ 下所有历史日期目录及其子任务目录中的 task_*.md 和 validation_*.md
   - 历史记录中已经标记为 `[DONE]` 的候选，避免重复实现同一功能
   - 历史任务目录中遗留的旧版 efficiency_discovery.md，仅作为兼容读取依据，不再作为新的写入目标

4. 生成或更新全局功能发现报告：
   - Agent/Reports/efficiency_discovery.md

   报告必须包含：
   - 全局候选功能列表
   - 扫描范围
   - 每个候选的唯一候选ID
   - 每个候选的状态标记：`[TODO]`、`[DONE]`、`[SKIPPED]`、`[BLOCKED]` 或 `[PARTIAL]`
   - 每个候选的来源信号、价值、风险、成本、优先级、推荐 Agent、推荐 Skill
   - 推荐优先开发的 1-3 个功能
   - 被跳过的高风险候选及原因
   - 已完成候选的任务卡路径、修改文件和验证记录摘要
   - 历史已完成候选的去重依据

   注意：
   - 不要覆盖已有候选状态。
   - 对已有 `[DONE]` 候选不得重新改为 `[TODO]`。
   - 如果发现相似候选，应合并到已有候选的处理说明中，不要重复分配新候选ID。
   - 新增候选时，应延续已有候选ID编号，例如已有 F001-F006，则新增从 F007 开始。

5. 自动选择一个最适合立即开发的候选功能：
   - 只能从 `[TODO]` 状态的候选中选择
   - 不得重复选择历史记录中已经 `[DONE]` 的候选
   - 优先选择 P0
   - 其次选择 P1
   - 必须满足：低风险或中风险、边界清晰、可在一个任务卡内完成、不会直接破坏业务数据或 Unity 资源引用
   - 如果候选涉及 Scene、Prefab、ScriptableObject、StreamingAssets、存档、Photon 或 AssetBundle 的直接修改，则跳过，并将状态更新为 `[SKIPPED]`

6. 生成本次任务目录：
   - 首先确保日期目录存在：
     - Agent/Reports/<今天日期>/
   - 然后在该日期目录下创建本次任务的独立目录：
     - Agent/Reports/<今天日期>/efficiency_<候选ID>_<功能名安全短名>/
   - 如果候选ID尚未确定，则先创建：
     - Agent/Reports/<今天日期>/efficiency_run_<HHmmss>/
   - 选定候选后，如果当前目录仍是 efficiency_run_<HHmmss>，可继续使用该目录。
   - 如果需要更清晰区分任务，可将目录调整为：
     - Agent/Reports/<今天日期>/efficiency_<候选ID>_<功能名安全短名>/
   - 如果发生目录调整，必须保证任务卡、验证记录和后续补充文件都位于最终的 `<TASK_DIR>` 中。
   - 不允许把多个任务的输出混写到同一个任务目录中。
   - 不允许在 `<TASK_DIR>` 中创建新的 efficiency_discovery.md。
   - 以下路径统一用 `<TASK_DIR>` 表示本次任务目录。

7. 为选中的候选生成任务卡：
   - <TASK_DIR>/task_efficiency_<候选ID>_<功能名安全短名>.md

   任务卡必须包含：
   - 候选ID
   - 原始候选
   - 当前状态
   - 本次任务目录
   - 全局候选报告路径：Agent/Reports/efficiency_discovery.md
   - 任务分类
   - 负责 Agent
   - 需要的 Skill
   - 影响路径
   - 不应触碰路径
   - 风险等级
   - 执行步骤
   - 验证步骤
   - 回滚方案
   - 结果区

8. 按任务卡实现该功能：
   - 只修改和该任务直接相关的文件
   - 优先放在 Agent、Scripts/2D/Editor 或其他低侵入路径
   - 保持现有项目命名、目录结构和代码风格
   - 不做无关重构
   - 不修改用户已有的无关改动
   - Unity 资源相关修改必须保留 .meta；如果无法安全处理，则不要修改该资源

9. 完成后运行可行的验证：
   - 至少做静态检查或编译相关检查
   - 如果不能运行 Unity 编译或 Play Mode，要在任务卡中明确写出未验证原因
   - 如果新增 Editor 工具或报告工具，要验证脚本路径、输出路径和基本扫描逻辑
   - 验证记录必须写入：
     - <TASK_DIR>/validation_efficiency_<候选ID>.md

10. 更新任务卡结果区，写入：
   - 最终状态：`[DONE]`、`[PARTIAL]` 或 `[BLOCKED]`
   - 已完成内容
   - 修改的文件
   - 验证结果
   - 验证记录路径
   - 未完成项
   - 剩余风险
   - 后续建议

11. 回写全局功能发现报告：
   - 打开 Agent/Reports/efficiency_discovery.md
   - 找到本次实现的候选ID
   - 将该候选状态从 `[TODO]` 更新为：
     - `[DONE]`：功能已实现且完成可行验证
     - `[PARTIAL]`：功能部分完成
     - `[BLOCKED]`：因环境、依赖或权限问题未能完成
   - 在该候选的“处理说明”中补充：
     - 本次任务目录路径
     - 对应任务卡路径
     - 验证记录路径
     - 修改文件
     - 验证结果摘要
     - 是否仍有剩余风险
   - 对自动跳过的候选，将状态更新为 `[SKIPPED]`，并写明跳过原因
   - 不要把状态回写到任务目录下的 efficiency_discovery.md，因为该文件不应再存在于任务目录中。

12. 最终回复只需要简洁汇总：
   - 全局候选报告路径
   - 本次任务目录
   - 自动选择了哪个功能
   - 候选ID
   - 最终状态
   - 修改了哪些文件
   - 任务卡路径
   - 验证记录路径
   - 验证结果
   - 剩余风险
