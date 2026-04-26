# AgentFull

AgentFull 是一套 Python 版 Unity 游戏项目自动化开发框架。它采用“主 Agent + 子 Agent + Skill”的结构，用于项目扫描、脚本分析、功能候选发现、任务卡生成、C# / Editor 工具生成、资源引用检查、报告输出和候选功能状态管理。

当前仓库中 `AgentFull` 位于 `Assets/AgentFull`，所以默认配置已经按这个位置调整：Unity 项目根目录是 `../..`，Assets 目录是 `..`。

## 目录结构

```text
AgentFull/
├── run.py
├── config/
├── core/
├── agents/
├── skills/
├── prompts/
├── memory/
├── cache/
├── reports/
└── examples/
```

## 安装依赖

```bash
cd Assets/AgentFull
pip install -r requirements.txt
```

依赖包括 `pyyaml`、`requests`、`python-dotenv`。

## 配置模型

模型配置在 `config/model_config.yaml`。已内置 OpenAI、Anthropic、DeepSeek、Qwen、Moonshot、Zhipu、Ollama 等配置格式。

API Key 通过环境变量读取，不会写入代码：

```bash
set OPENAI_API_KEY=your_key
set DEEPSEEK_API_KEY=your_key
```

Linux/macOS:

```bash
export OPENAI_API_KEY=your_key
export DEEPSEEK_API_KEY=your_key
```

本地测试可直接使用 mock，不调用真实模型：

```bash
python run.py --task auto_discover_and_implement --mock
```

## 配置 Unity 项目路径

路径配置在 `config/unity_project_config.yaml`。

当前默认值适配 `Assets/AgentFull`：

```yaml
unity_project:
  root_path: "../.."
  assets_path: ".."
  scripts_path: "../Scripts"
  editor_path: "../Editor"
  scenes_path: "../Scenes"
  prefabs_path: "../Prefabs"
  resources_path: "../Resources"
  streaming_assets_path: "../StreamingAssets"
  addressables_path: "../AddressableAssetsData"
  reports_path: "./reports"
```

也可以运行时覆盖：

```bash
python run.py --task scan_project --project-root D:/LAB/Unity/RandomWorld --mock
```

## 运行方式

```bash
python run.py --task auto_discover_and_implement
python run.py --task scan_project
python run.py --task analyze_scripts
python run.py --task generate_feature
python run.py --model openai
python run.py --model deepseek
python run.py --mock
```

常用组合：

```bash
python run.py --task auto_discover_and_implement --mock
python run.py --task auto_discover_and_implement --model deepseek
python run.py --task scan_project --output ./reports/manual_scan --mock
```

## 任务类型

- `scan_project`：扫描 Unity 目录、统计脚本/场景/Prefab/材质/贴图，并执行只读资源引用检查。
- `analyze_scripts`：扫描项目并分析 C# 脚本结构。
- `generate_feature`：发现候选功能并生成任务卡与代码，但不把候选标记为 completed。
- `auto_discover_and_implement`：完整流程，生成代码、验证、报告，并更新候选功能状态。

## Skill 扩展

新增 Skill：

1. 在 `skills/` 下创建 `xxx_skill.py`。
2. 继承 `core.skill.Skill`。
3. 实现 `name`、`description`、`input_schema`、`output_schema`、`run(params, context)`。
4. 在 `core/main_agent.py` 的 `register_skills()` 中注册。

Skill 可通过 `context.get_service("cache")`、`context.get_service("memory")`、`context.get_service("report_writer")` 访问基础服务。

## 子 Agent 扩展

新增子 Agent：

1. 在 `agents/` 下创建 Agent 文件。
2. 继承 `core.sub_agent.SubAgent`。
3. 设置 `name`、`description`、`available_skills`。
4. 实现 `run(task, context)`。
5. 在 `core/main_agent.py` 的 `register_agents()` 中注册。

## 报告输出

完整流程输出到：

```text
AgentFull/reports/<date>/<candidate_id>_<feature_safe_name>/
```

目录内包含：

- `report.md`：人类可读开发报告。
- `execution_context.json`：压缩后的执行上下文。
- `generated_code/`：生成的 C# 或 Unity Editor 代码。

未确定候选功能前会先使用 `run_<HHmmss>` 风格的任务目录。

## 安全策略

默认策略：

- 不覆盖已有 Unity 文件。
- 不直接修改 Scene。
- 不直接修改 Prefab。
- 不直接修改 ScriptableObject。
- 不修改 StreamingAssets。
- 不修改 Addressables。
- 高风险候选默认跳过。
- 生成代码先放到报告目录 `generated_code`。
- 验证阶段只做静态检查和只读资源扫描。

默认生成的首个低风险功能是“Unity 项目只读资源与脚本概览报告工具”。它生成一个 `EditorWindow`，用于扫描项目统计并导出 Markdown 报告。

## 常见问题

### 没有 API Key 能运行吗？

可以。使用 `--mock` 即可本地运行，不会调用外部模型。

### 会改我的 Unity 项目文件吗？

默认不会。生成的 C# 文件会写入 `AgentFull/reports/.../generated_code`，需要你手动审查后再复制到 `Assets/Editor`。

### 为什么 `root_path` 是 `../..`？

因为本项目当前放在 `Assets/AgentFull`。如果你把 `AgentFull` 移到 Unity 项目根目录，请把配置改为 `root_path: "."`、`assets_path: "./Assets"`。

### 如何避免重复实现同一候选功能？

框架会更新 `memory/feature_candidates.json`。已完成候选会标记为 `completed`，后续选择时会自动跳过。
