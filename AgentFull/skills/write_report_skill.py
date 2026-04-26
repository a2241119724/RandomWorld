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
        lines.append("# AgentFull Development Report")
        lines.append("")
        lines.append(f"- Generated At: {datetime.now().isoformat(timespec='seconds')}")
        lines.append(f"- Task: `{task.get('task', '')}`")
        lines.append(f"- Task ID: `{task.get('task_id', '')}`")
        lines.append(f"- Report Directory: `{data.get('report_dir', '')}`")
        lines.append("")

        lines.append("## Model Calls")
        lines.append("")
        if model_calls:
            lines.append("| Purpose | Provider | Model | Mock | Used | Fallback |")
            lines.append("| --- | --- | --- | --- | --- | --- |")
            for call in model_calls:
                lines.append(
                    "| {purpose} | {provider} | {model} | {mock} | {used} | {fallback} |".format(
                        purpose=call.get("purpose", ""),
                        provider=call.get("provider", ""),
                        model=call.get("model", ""),
                        mock=call.get("mock", ""),
                        used=call.get("used", ""),
                        fallback=call.get("fallback_reason", "") or call.get("note", ""),
                    )
                )
        else:
            lines.append("No model calls recorded.")
        lines.append("")

        lines.append("## Project Scan Summary")
        lines.append("")
        if scan:
            lines.append(scan.get("summary", "Project scan completed."))
            lines.append("")
            lines.append("| Item | Count |")
            lines.append("| --- | ---: |")
            for key, value in scan.get("counts", {}).items():
                lines.append(f"| {key} | {value} |")
        else:
            lines.append("No project scan result.")
        lines.append("")

        lines.append("## CSharp Script Analysis")
        lines.append("")
        if scripts:
            lines.append(f"- Total Scripts: {scripts.get('total_scripts', 0)}")
            summary = scripts.get("summary", {})
            lines.append(f"- MonoBehaviour Classes: {summary.get('mono_behaviour_classes', 0)}")
            lines.append(f"- ScriptableObject Classes: {summary.get('scriptable_object_classes', 0)}")
            lines.append(f"- EditorWindow Classes: {summary.get('editor_window_classes', 0)}")
            lines.append(f"- Public Fields: {summary.get('public_fields', 0)}")
            lines.append(f"- SerializeField Fields: {summary.get('serialize_fields', 0)}")
        else:
            lines.append("No C# script analysis result.")
        lines.append("")

        lines.append("## Candidate Features")
        lines.append("")
        if candidates:
            lines.append("| ID | Feature | Risk | Type | Status |")
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
            lines.append("No candidates generated.")
        lines.append("")

        lines.append("## Selected Feature")
        lines.append("")
        if selected:
            lines.append(f"- ID: `{selected.get('candidate_id')}`")
            lines.append(f"- Name: {selected.get('feature_name')}")
            lines.append(f"- Risk: {selected.get('risk_level')}")
            lines.append(f"- Type: {selected.get('implementation_type')}")
            lines.append(f"- Value: {selected.get('value')}")
        else:
            lines.append("No feature was selected for implementation.")
        lines.append("")

        lines.append("## Task Card")
        lines.append("")
        lines.append("```json")
        lines.append(json.dumps(task_card, ensure_ascii=False, indent=2))
        lines.append("```")
        lines.append("")

        lines.append("## Generated Code Files")
        lines.append("")
        if generated:
            for path in generated:
                lines.append(f"- `{path}`")
        else:
            lines.append("No code files were generated.")
        if generated_meta:
            lines.append("")
            lines.append("Generated Unity meta files:")
            for path in generated_meta:
                lines.append(f"- `{path}`")
        lines.append("")

        lines.append("## Validation Results")
        lines.append("")
        lines.append("### Generated File Static Checks")
        lines.append("")
        lines.append("```json")
        lines.append(json.dumps(validation, ensure_ascii=False, indent=2))
        lines.append("```")
        lines.append("")
        lines.append("### Asset Reference Check")
        lines.append("")
        lines.append("```json")
        lines.append(json.dumps(asset_check, ensure_ascii=False, indent=2))
        lines.append("```")
        lines.append("")

        lines.append("## Risk Notes")
        lines.append("")
        lines.append("- Default policy does not overwrite existing Unity files.")
        lines.append("- Scene, Prefab, ScriptableObject, StreamingAssets, and Addressables modification is disabled.")
        lines.append("- Generated feature C# is written to the configured Unity Scripts folder as a new file when possible.")
        if selected and selected.get("risk_level") == "high":
            lines.append("- High-risk candidate was not implemented by default.")
        lines.append("")

        lines.append("## Errors")
        lines.append("")
        if errors:
            lines.append("```json")
            lines.append(json.dumps(errors, ensure_ascii=False, indent=2))
            lines.append("```")
        else:
            lines.append("No runtime errors recorded.")
        lines.append("")

        lines.append("## Next Suggestions")
        lines.append("")
        lines.append("- Review the generated C# file in its Unity folder.")
        lines.append("- Run Unity compilation after generation.")
        lines.append("- Attach or wire the generated runtime feature manually after code review.")
        lines.append("- Keep using feature_candidates.json to avoid repeating completed feature work.")
        lines.append("")
        return "\n".join(lines)
