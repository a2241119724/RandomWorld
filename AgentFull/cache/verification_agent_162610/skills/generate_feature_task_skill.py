from __future__ import annotations

from pathlib import Path
from typing import Any

from core.file_utils import csharp_class_name, csharp_output_dir
from core.skill import Skill


class GenerateFeatureTaskSkill(Skill):
    name = "generate_feature_task"
    description = "Select a safe candidate and convert it into an implementation task card."
    input_schema = {"candidates": "candidate list", "report_dir": "current report directory"}
    output_schema = {"selected_candidate": "chosen candidate", "task_card": "implementation card"}

    def run(self, params: dict[str, Any], context: Any) -> dict[str, Any]:
        candidates = list(params.get("candidates") or [])
        configs = context.get("configs", {}).get("unity", {})
        policies = configs
        risk_policy = policies.get("risk_policy", {})
        skip_high = risk_policy.get("skip_high_risk_candidate", True)

        selected = self._select_candidate(candidates, skip_high=skip_high)
        if not selected:
            return {
                "selected_candidate": None,
                "task_card": {
                    "task_goal": "Generate readonly analysis report only.",
                    "reason": "No pending low/medium risk candidate was available.",
                    "verification_steps": ["Review report.md for scan and validation results."],
                },
            }

        report_dir = Path(params.get("report_dir") or context.get("report_dir") or ".")
        base_dir = Path(context.get_service("base_dir") or ".")
        class_name = self._class_name_for_candidate(selected)
        generated_dir = csharp_output_dir(
            base_dir,
            configs,
            report_dir,
            implementation_type=selected.get("implementation_type"),
        )
        generated_file = generated_dir / f"{class_name}.cs"
        task_card = {
            "task_goal": selected.get("feature_name"),
            "candidate_id": selected.get("candidate_id"),
            "description": selected.get("description"),
            "implementation_type": selected.get("implementation_type"),
            "implementation_scope": [
                "Generate C# into the configured Unity script folder.",
                "Do not modify scenes, prefabs, ScriptableObjects, StreamingAssets, or Addressables.",
                "Keep the tool readonly and avoid overwriting existing Unity files.",
            ],
            "modify_files": [],
            "new_files": [str(generated_file)],
            "risk_notes": [
                f"Risk level: {selected.get('risk_level')}",
                "Generated code is written as a new file only; existing Unity files are not overwritten.",
            ],
            "verification_steps": [
                "Review generated C# code at the configured Unity path.",
                "Open Unity and confirm the EditorWindow/menu compiles.",
                "Use the tool to export a Markdown report.",
            ],
            "rollback_plan": [
                "Delete the generated C# file from the configured Unity path.",
            ],
            "selected_at": context.get("started_at"),
        }
        return {"selected_candidate": selected, "task_card": task_card}

    def _select_candidate(self, candidates: list[dict[str, Any]], skip_high: bool) -> dict[str, Any] | None:
        risk_order = {"low": 0, "medium": 1, "high": 2}
        type_order = {"readonly_tool": 0, "editor_tool": 1, "report_tool": 2, "runtime_feature": 3}
        eligible = []
        for candidate in candidates:
            if candidate.get("status") != "pending":
                continue
            if skip_high and candidate.get("risk_level") == "high":
                continue
            eligible.append(candidate)
        eligible.sort(
            key=lambda item: (
                risk_order.get(item.get("risk_level"), 9),
                type_order.get(item.get("implementation_type"), 9),
                item.get("candidate_id", ""),
            )
        )
        return eligible[0] if eligible else None

    def _class_name_for_candidate(self, candidate: dict[str, Any]) -> str:
        candidate_id = candidate.get("candidate_id", "candidate")
        if candidate_id == "cand_001_project_overview_editor":
            return "AgentProjectOverviewWindow"
        return csharp_class_name(candidate.get("feature_name"), "GeneratedUnityTool")
