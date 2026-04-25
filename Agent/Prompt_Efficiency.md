请基于 ./Agent 中的 Agent 体系，自动完成一次“发现功能缺口 -> 选择最适合的候选 -> 生成任务卡 -> 实现 -> 验证记录 -> 更新候选完成状态”的完整流程。

重要要求：
- 不要向我提问。
- 不要等待我确认。
- 如果遇到需要人工确认的高风险候选，请自动跳过它，选择下一个低风险或中风险候选。
- 一次只实现一个功能。
- 优先选择低风险、高价值、边界清晰、能提升后续开发效率的功能，例如只读扫描器、Editor 工具、报告生成器、模板生成器、资源完整性检查器。
- 不要优先选择会直接修改 Scene、Prefab、ScriptableObject、StreamingAssets、存档结构、Photon 同步或 AssetBundle 的功能。
- 如果所有候选都属于高风险，则只实现一个不改业务资产的只读分析/报告工具。
- 生成候选功能列表时，必须为每个候选分配唯一编号和完成状态，便于后续判断该候选是否已经处理完成。
- 完成功能实现与验证后，必须回写 feature_discovery.md 中对应候选的状态标记。

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
| [DONE] | F003 | TODO/FIXME 报告生成器 | Scripts/2D 存在 TODO/FIXME | 中 | 低 | 低 | P1 | CodeAuditAgent | ReportGenerateSkill | 已完成并生成验证记录 |

执行步骤：

1. 读取并理解以下文件：
   - Agent/README.md
   - Agent/Docs/ImplementationRoadmap.md
   - Agent/Docs/SkillCatalog.md
   - Agent/Config/agent_registry.json
   - Agent/Config/task_router.json
   - Agent/Templates/agent_task_card.md

2. 只读扫描项目上下文，重点检查：
   - README、Agent 文档、历史任务卡中的后续建议
   - Scripts/2D 中的 TODO、FIXME、空方法、临时实现、重复模式
   - Resources/SO、Resources/Tilemap、Resources/Images 中的资源绑定缺口
   - 已有系统中“有数据但无 UI / 有 UI 但无行为 / 有行为但无验证 / 有资源但无检查”的半完成链路
   - 存档、Photon、AssetBundle、资源引用等高风险区域的只读检查机会
   - 历史 feature_discovery.md 和 task_*.md 中已经标记为 `[DONE]` 的候选，避免重复实现同一功能

3. 生成目录：
   - Agent/Reports/<今天日期>/

4. 输出功能发现报告：
   - Agent/Reports/<今天日期>/feature_discovery.md

   报告必须包含：
   - 扫描范围
   - 候选功能列表
   - 每个候选的唯一候选ID
   - 每个候选的状态标记：`[TODO]`、`[DONE]`、`[SKIPPED]`、`[BLOCKED]` 或 `[PARTIAL]`
   - 每个候选的来源信号、价值、风险、成本、优先级、推荐 Agent、推荐 Skill
   - 推荐优先开发的 1-3 个功能
   - 被跳过的高风险候选及原因
   - 已完成候选的任务卡路径、修改文件和验证记录摘要

5. 自动选择一个最适合立即开发的候选功能：
   - 只能从 `[TODO]` 状态的候选中选择
   - 不得重复选择历史记录中已经 `[DONE]` 的候选
   - 优先选择 P0
   - 其次选择 P1
   - 必须满足：低风险或中风险、边界清晰、可在一个任务卡内完成、不会直接破坏业务数据或 Unity 资源引用
   - 如果候选涉及 Scene、Prefab、ScriptableObject、StreamingAssets、存档、Photon 或 AssetBundle 的直接修改，则跳过，并将状态更新为 `[SKIPPED]`

6. 为选中的候选生成任务卡：
   - Agent/Reports/<今天日期>/task_<候选ID>_<功能名>.md

   任务卡必须包含：
   - 候选ID
   - 原始候选
   - 当前状态
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

7. 按任务卡实现该功能：
   - 只修改和该任务直接相关的文件
   - 优先放在 Agent、Scripts/2D/Editor 或其他低侵入路径
   - 保持现有项目命名、目录结构和代码风格
   - 不做无关重构
   - 不修改用户已有的无关改动
   - Unity 资源相关修改必须保留 .meta；如果无法安全处理，则不要修改该资源

8. 完成后运行可行的验证：
   - 至少做静态检查或编译相关检查
   - 如果不能运行 Unity 编译或 Play Mode，要在任务卡中明确写出未验证原因
   - 如果新增 Editor 工具或报告工具，要验证脚本路径、输出路径和基本扫描逻辑

9. 更新任务卡结果区，写入：
   - 最终状态：`[DONE]`、`[PARTIAL]` 或 `[BLOCKED]`
   - 已完成内容
   - 修改的文件
   - 验证结果
   - 未完成项
   - 剩余风险
   - 后续建议

10. 回写功能发现报告：
   - 打开 Agent/Reports/<今天日期>/feature_discovery.md
   - 找到本次实现的候选ID
   - 将该候选状态从 `[TODO]` 更新为：
     - `[DONE]`：功能已实现且完成可行验证
     - `[PARTIAL]`：功能部分完成
     - `[BLOCKED]`：因环境、依赖或权限问题未能完成
   - 在该候选的“处理说明”中补充：
     - 对应任务卡路径
     - 修改文件
     - 验证结果摘要
     - 是否仍有剩余风险
   - 对自动跳过的候选，将状态更新为 `[SKIPPED]`，并写明跳过原因

11. 最终回复只需要简洁汇总：
   - 自动选择了哪个功能
   - 候选ID
   - 最终状态
   - 修改了哪些文件
   - 验证结果
   - 剩余风险