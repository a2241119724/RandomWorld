from __future__ import annotations

from pathlib import Path
from typing import Any

from core.file_utils import safe_slug
from core.skill import Skill


class GenerateFeatureTaskSkill(Skill):
    name = "generate_feature_task"
    description = "Select a safe candidate and convert it into an implementation task card."
    input_schema = {"candidates": "candidate list", "report_dir": "current report directory"}
    output_schema = {"selected_candidate": "chosen candidate", "task_card": "implementation card"}

    def run(self, params: dict[str, Any], context: Any) -> dict[str, Any]:
        candidates = list(params.get("candidates") or [])
        policies = context.get("configs", {}).get("unity", {})
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
        class_name = self._class_name_for_candidate(selected)
        generated_file = report_dir / "generated_code" / f"{class_name}.cs"
        task_card = {
            "task_goal": selected.get("feature_name"),
            "candidate_id": selected.get("candidate_id"),
            "description": selected.get("description"),
            "implementation_type": selected.get("implementation_type"),
            "implementation_scope": [
                "Generate code into the report folder first.",
                "Do not modify scenes, prefabs, ScriptableObjects, StreamingAssets, or Addressables.",
                "Keep the tool readonly and suitable for manual review before copying into Unity.",
            ],
            "modify_files": [],
            "new_files": [str(generated_file)],
            "risk_notes": [
                f"Risk level: {selected.get('risk_level')}",
                "Generated code is not inserted into the Unity project automatically.",
            ],
            "verification_steps": [
                "Review generated C# code in generated_code.",
                "Optionally copy the Editor script into Assets/Editor after review.",
                "Open Unity and confirm the EditorWindow/menu compiles.",
                "Use the tool to export a Markdown report.",
            ],
            "rollback_plan": [
                "Delete the generated_code file from the report folder.",
                "If manually copied into Assets/Editor later, remove that copied file.",
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
        base = safe_slug(candidate.get("feature_name", "GeneratedUnityTool"), 40)
        return "".join(part.capitalize() for part in base.replace("-", "_").split("_") if part) or "GeneratedUnityTool"
