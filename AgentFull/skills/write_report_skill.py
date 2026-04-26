from __future__ import annotations

import json
from datetime import datetime
from pathlib import Path
from typing import Any

from core.skill import Skill


class WriteReportSkill(Skill):
    name = "write_report"
    description = "Write a Markdown report and compressed execution context."
    input_schema = {"report_dir": "target report directory"}
    output_schema = {"report_path": "Markdown report path", "context_path": "JSON context path"}

    def run(self, params: dict[str, Any], context: Any) -> dict[str, Any]:
        report_writer = context.get_service("report_writer")
        compressor = context.get_service("context_compressor")
        report_dir = Path(params.get("report_dir") or context.get("report_dir") or ".")
        report_dir.mkdir(parents=True, exist_ok=True)

        context_data = context.to_serializable()
        compressed = compressor.compress(context_data) if compressor else context_data
        markdown = self._build_markdown(context_data)
        report_path = report_writer.write_report(report_dir, markdown)
        context_path = report_writer.write_json(report_dir, "execution_context.json", compressed)
        return {"report_path": str(report_path), "context_path": str(context_path)}

    def _build_markdown(self, data: dict[str, Any]) -> str:
        task = data.get("task", {})
        scan = data.get("project_scan", {})
        scripts = data.get("script_analysis", {})
        candidates = data.get("feature_candidates", [])
        selected = data.get("selected_candidate") or {}
        task_card = data.get("task_card") or {}
        generated = data.get("generated_files", [])
        generated_meta = data.get("generated_meta_files", [])
        validation = data.get("validation", {})
        asset_check = data.get("asset_reference_check", {})
        model_calls = data.get("model_calls", [])
        errors = data.get("errors", [])

        lines: list[str] = []
        lines.append("# AgentFull 自动开发报告")
        lines.append("")
        lines.append(f"- 生成时间: {datetime.now().isoformat(timespec='seconds')}")
        lines.append(f"- 任务: `{task.get('task', '')}`")
        lines.append(f"- 任务 ID: `{task.get('task_id', '')}`")
        lines.append(f"- 报告目录: `{data.get('report_dir', '')}`")
        lines.append("")

        lines.append("## 模型调用")
        lines.append("")
        if model_calls:
            lines.append("| 用途 | Provider | Model | Mock | 是否采用 | 请求日志 | 响应日志 | 备注 |")
            lines.append("| --- | --- | --- | --- | --- | --- | --- | --- |")
            for call in model_calls:
                lines.append(
                    "| {purpose} | {provider} | {model} | {mock} | {used} | {request_log} | {response_log} | {fallback} |".format(
                        purpose=call.get("purpose", ""),
                        provider=call.get("provider", ""),
                        model=call.get("model", ""),
                        mock=call.get("mock", ""),
                        used=call.get("used", ""),
                        request_log=call.get("request_log_path", ""),
                        response_log=call.get("response_log_path", ""),
                        fallback=call.get("fallback_reason", "") or call.get("note", ""),
                    )
                )
        else:
            lines.append("没有记录模型调用。")
        lines.append("")

        lines.append("## 项目扫描摘要")
        lines.append("")
        if scan:
            lines.append(scan.get("summary", "Project scan completed."))
            lines.append("")
            lines.append("| 项目 | 数量 |")
            lines.append("| --- | ---: |")
            for key, value in scan.get("counts", {}).items():
                lines.append(f"| {key} | {value} |")
        else:
            lines.append("没有项目扫描结果。")
        lines.append("")

        lines.append("## C# 脚本分析")
        lines.append("")
        if scripts:
            lines.append(f"- 脚本总数: {scripts.get('total_scripts', 0)}")
            summary = scripts.get("summary", {})
            lines.append(f"- MonoBehaviour 类: {summary.get('mono_behaviour_classes', 0)}")
            lines.append(f"- ScriptableObject 类: {summary.get('scriptable_object_classes', 0)}")
            lines.append(f"- EditorWindow 类: {summary.get('editor_window_classes', 0)}")
            lines.append(f"- Public 字段: {summary.get('public_fields', 0)}")
            lines.append(f"- SerializeField 字段: {summary.get('serialize_fields', 0)}")
        else:
            lines.append("没有 C# 脚本分析结果。")
        lines.append("")

        lines.append("## 候选功能")
        lines.append("")
        if candidates:
            lines.append("| ID | 功能 | 风险 | 类型 | 状态 |")
            lines.append("| --- | --- | --- | --- | --- |")
            for item in candidates:
                lines.append(
                    "| {candidate_id} | {feature_name} | {risk_level} | {implementation_type} | {status} |".format(
                        **{
                            "candidate_id": item.get("candidate_id", ""),
                            "feature_name": item.get("feature_name", ""),
                            "risk_level": item.get("risk_level", ""),
                            "implementation_type": item.get("implementation_type", ""),
                            "status": item.get("status", ""),
                        }
                    )
                )
        else:
            lines.append("没有生成候选功能。")
        lines.append("")

        lines.append("## 选中功能")
        lines.append("")
        if selected:
            lines.append(f"- ID: `{selected.get('candidate_id')}`")
            lines.append(f"- 名称: {selected.get('feature_name')}")
            lines.append(f"- 风险: {selected.get('risk_level')}")
            lines.append(f"- 类型: {selected.get('implementation_type')}")
            lines.append(f"- 价值: {selected.get('value')}")
        else:
            lines.append("没有选中要实现的功能。")
        lines.append("")

        lines.append("## 任务卡")
        lines.append("")
        lines.append("```json")
        lines.append(json.dumps(task_card, ensure_ascii=False, indent=2))
        lines.append("```")
        lines.append("")

        lines.append("## 生成代码文件")
        lines.append("")
        if generated:
            for path in generated:
                lines.append(f"- `{path}`")
        else:
            lines.append("没有生成代码文件。")
        if generated_meta:
            lines.append("")
            lines.append("生成的 Unity meta 文件:")
            for path in generated_meta:
                lines.append(f"- `{path}`")
        lines.append("")

        lines.append("## 验证结果")
        lines.append("")
        lines.append("### 生成文件静态检查")
        lines.append("")
        lines.append("```json")
        lines.append(json.dumps(validation, ensure_ascii=False, indent=2))
        lines.append("```")
        lines.append("")
        lines.append("### 资源引用检查")
        lines.append("")
        lines.append("```json")
        lines.append(json.dumps(asset_check, ensure_ascii=False, indent=2))
        lines.append("```")
        lines.append("")

        lines.append("## 风险说明")
        lines.append("")
        lines.append("- 默认策略不会覆盖已有 Unity 文件。")
        lines.append("- 默认禁用对场景、Prefab、ScriptableObject、StreamingAssets 和 Addressables 的修改。")
        lines.append("- 生成的功能 C# 会尽量以新文件形式写入配置的 Unity Scripts 目录。")
        if selected and selected.get("risk_level") == "high":
            lines.append("- 默认不实现高风险候选项。")
        lines.append("")

        lines.append("## 错误")
        lines.append("")
        if errors:
            lines.append("```json")
            lines.append(json.dumps(errors, ensure_ascii=False, indent=2))
            lines.append("```")
        else:
            lines.append("没有记录运行时错误。")
        lines.append("")

        lines.append("## 后续建议")
        lines.append("")
        lines.append("- 审查 Unity 目录中的生成 C# 文件。")
        lines.append("- 在 Unity 中触发编译，确认没有编译错误。")
        lines.append("- 代码审查后，手动挂载或接入生成的运行时功能。")
        lines.append("- 持续使用 feature_candidates.json 避免重复开发已完成候选功能。")
        lines.append("")
        return "\n".join(lines)
