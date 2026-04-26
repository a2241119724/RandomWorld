from __future__ import annotations

from pathlib import Path
from typing import Any

from core.file_utils import csharp_class_name, csharp_output_dir, read_text
from core.llm_utils import compact_json, extract_json_value, record_model_call
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

        llm_plan = self._llm_plan(candidates, selected, skip_high, context)
        llm_selected = self._candidate_from_plan(candidates, llm_plan, skip_high)
        if llm_selected:
            selected = llm_selected

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
            "suggested_class_name": selected.get("suggested_class_name") or class_name,
            "implementation_scope": [
                "Generate one new C# runtime feature file into the configured Unity Scripts folder.",
                "Do not modify existing scenes, prefabs, ScriptableObjects, StreamingAssets, or Addressables.",
                "Keep the feature self-contained so it can be attached or wired manually after review.",
            ],
            "modify_files": [],
            "new_files": [str(generated_file)],
            "risk_notes": [
                f"Risk level: {selected.get('risk_level')}",
                "Generated code is written as a new file only; existing Unity files are not overwritten.",
                "Runtime integration is opt-in and requires manual Unity review.",
            ],
            "verification_steps": [
                "Review generated C# code at the configured Unity path.",
                "Open Unity and confirm the new script compiles.",
                "Attach or reference the component in a test scene/prefab after review.",
            ],
            "rollback_plan": [
                "Delete the generated C# file from the configured Unity path.",
            ],
            "selected_at": context.get("started_at"),
        }
        task_card = self._merge_llm_task_card(task_card, llm_plan.get("task_card", {}))
        return {"selected_candidate": selected, "task_card": task_card}

    def _llm_plan(
        self,
        candidates: list[dict[str, Any]],
        default_selected: dict[str, Any],
        skip_high: bool,
        context: Any,
    ) -> dict[str, Any]:
        router = context.get_service("model_router")
        if not router:
            return {}

        base_dir = Path(context.get_service("base_dir") or ".")
        system_prompt = read_text(base_dir / "prompts" / "feature_planner_prompt.md")
        eligible = [
            candidate
            for candidate in candidates
            if candidate.get("status") == "pending"
            and not (skip_high and candidate.get("risk_level") == "high")
        ]
        messages = [
            {
                "role": "system",
                "content": (
                    system_prompt
                    + "\nYou select exactly one implementation candidate and refine the task card. "
                    "Return JSON only."
                ),
            },
            {
                "role": "user",
                "content": (
                    "Choose the safest useful new feature candidate from this eligible list. "
                    "Prefer standalone runtime_feature candidates that can be implemented as a "
                    "new C# file without editing existing project files. "
                    "Do not select high-risk candidates when skip_high_risk_candidate is true. "
                    "Do not add scene, prefab, ScriptableObject, StreamingAssets, Addressables, "
                    "save-data, networking, build, or destructive file changes.\n\n"
                    "Return this JSON schema:\n"
                    "{\n"
                    "  \"selected_candidate_id\": \"candidate id from the list\",\n"
                    "  \"selection_reason\": \"short reason\",\n"
                    "  \"task_card\": {\n"
                    "    \"implementation_scope\": [],\n"
                    "    \"risk_notes\": [],\n"
                    "    \"verification_steps\": [],\n"
                    "    \"rollback_plan\": []\n"
                    "  }\n"
                    "}\n\n"
                    f"skip_high_risk_candidate: {skip_high}\n"
                    f"default_selected_candidate_id: {default_selected.get('candidate_id')}\n"
                    f"eligible_candidates:\n{compact_json(eligible, 10000)}"
                ),
            },
        ]
        response = router.chat_for_task("generate_feature_task", messages)
        parsed = extract_json_value(response.get("content", ""))
        plan = parsed if isinstance(parsed, dict) else {}
        used = bool(self._candidate_from_plan(candidates, plan, skip_high))
        record_model_call(
            context,
            "generate_feature_task",
            response,
            used=used,
            note=f"selected_candidate_id={plan.get('selected_candidate_id', '')}",
        )
        return plan if used else {}

    def _candidate_from_plan(
        self,
        candidates: list[dict[str, Any]],
        plan: dict[str, Any],
        skip_high: bool,
    ) -> dict[str, Any] | None:
        selected_id = plan.get("selected_candidate_id")
        if not selected_id:
            return None
        for candidate in candidates:
            if candidate.get("candidate_id") != selected_id:
                continue
            if candidate.get("status") != "pending":
                return None
            if skip_high and candidate.get("risk_level") == "high":
                return None
            return candidate
        return None

    def _merge_llm_task_card(
        self,
        task_card: dict[str, Any],
        llm_task_card: Any,
    ) -> dict[str, Any]:
        if not isinstance(llm_task_card, dict):
            return task_card

        merged = dict(task_card)
        list_fields = {
            "implementation_scope",
            "risk_notes",
            "verification_steps",
            "rollback_plan",
        }
        for field in list_fields:
            value = llm_task_card.get(field)
            if isinstance(value, list) and value:
                merged[field] = [str(item) for item in value[:8]]

        for field in {"task_goal", "description"}:
            value = llm_task_card.get(field)
            if isinstance(value, str) and value.strip():
                merged[field] = value.strip()

        merged["candidate_id"] = task_card.get("candidate_id")
        merged["implementation_type"] = task_card.get("implementation_type")
        merged["modify_files"] = []
        merged["new_files"] = task_card.get("new_files", [])
        merged["selected_at"] = task_card.get("selected_at")
        return merged

    def _select_candidate(self, candidates: list[dict[str, Any]], skip_high: bool) -> dict[str, Any] | None:
        risk_order = {"low": 0, "medium": 1, "high": 2}
        type_order = {"runtime_feature": 0, "editor_tool": 1, "report_tool": 2, "readonly_tool": 3}
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
        return csharp_class_name(
            candidate.get("suggested_class_name") or candidate.get("feature_name"),
            "GeneratedUnityFeature",
        )
