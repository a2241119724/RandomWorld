from __future__ import annotations

from pathlib import Path
from typing import Any

from core.file_utils import read_text, safe_slug
from core.llm_utils import compact_json, extract_json_value, record_model_call
from core.skill import Skill


class DiscoverFeatureGapSkill(Skill):
    name = "discover_feature_gap"
    description = "Create low-risk feature candidates from Unity project analysis."
    input_schema = {"project_scan": "scan result", "script_analysis": "script analysis result"}
    output_schema = {"candidates": "candidate feature list"}

    def run(self, params: dict[str, Any], context: Any) -> dict[str, Any]:
        project_scan = params.get("project_scan") or {}
        script_analysis = params.get("script_analysis") or {}
        memory = context.get_service("memory")

        counts = project_scan.get("counts", {})
        summary = script_analysis.get("summary", {})
        local_candidates = [
            {
                "candidate_id": "cand_001_project_overview_editor",
                "feature_name": "Unity Project Readonly Resource And Script Overview Tool",
                "description": (
                    "Generate a readonly EditorWindow that scans scripts, scenes, prefabs, "
                    "materials, and textures, then exports a Markdown report."
                ),
                "value": "Gives the project a safe in-Unity overview tool for future automation work.",
                "risk_level": "low",
                "affected_files": [],
                "implementation_type": "editor_tool",
                "status": "pending",
                "signals": {
                    "csharp_files": counts.get("csharp_files", 0),
                    "scenes": counts.get("scenes", 0),
                    "prefabs": counts.get("prefabs", 0),
                },
            },
            {
                "candidate_id": "cand_002_asset_reference_audit",
                "feature_name": "Readonly Asset Reference Risk Report",
                "description": (
                    "Scan prefab, scene, material, and ScriptableObject YAML files for empty GUIDs, "
                    "missing script markers, and suspicious local references."
                ),
                "value": "Finds project hygiene issues without touching assets.",
                "risk_level": "low",
                "affected_files": [],
                "implementation_type": "report_tool",
                "status": "pending",
                "signals": {
                    "asset_files": counts.get("prefabs", 0)
                    + counts.get("scenes", 0)
                    + counts.get("materials", 0)
                    + counts.get("scriptable_assets", 0)
                },
            },
            {
                "candidate_id": "cand_003_script_lifecycle_report",
                "feature_name": "CSharp Lifecycle And Serialization Summary",
                "description": (
                    "Generate a static report for MonoBehaviour lifecycle methods, public fields, "
                    "and SerializeField usage."
                ),
                "value": "Highlights maintainability hotspots and helps plan refactors.",
                "risk_level": "low",
                "affected_files": [],
                "implementation_type": "report_tool",
                "status": "pending",
                "signals": {
                    "mono_behaviour_classes": summary.get("mono_behaviour_classes", 0),
                    "public_fields": summary.get("public_fields", 0),
                    "serialize_fields": summary.get("serialize_fields", 0),
                },
            },
            {
                "candidate_id": "cand_004_editor_validation_menu",
                "feature_name": "Readonly Editor Validation Menu",
                "description": (
                    "Create an Editor menu command that runs non-destructive checks and writes "
                    "a validation report."
                ),
                "value": "Makes repeat validation accessible inside Unity.",
                "risk_level": "medium",
                "affected_files": [],
                "implementation_type": "editor_tool",
                "status": "pending",
                "signals": {"editor_window_classes": summary.get("editor_window_classes", 0)},
            },
            {
                "candidate_id": "cand_005_runtime_gameplay_tracker",
                "feature_name": "Runtime Gameplay Session Tracker",
                "description": (
                    "Add runtime tracking for play session metrics. This touches gameplay code "
                    "and should be implemented only after manual review."
                ),
                "value": "Could improve telemetry for balancing and debugging.",
                "risk_level": "high",
                "affected_files": [],
                "implementation_type": "runtime_feature",
                "status": "pending",
                "signals": {},
            },
        ]

        llm_candidates = self._llm_candidates(project_scan, script_analysis, local_candidates, context)
        candidates = self._merge_candidates(local_candidates, llm_candidates)

        if memory:
            candidates = memory.merge_feature_candidates(candidates)

        ordered = sorted(candidates, key=self._priority_key)
        return {"candidates": ordered}

    def _llm_candidates(
        self,
        project_scan: dict[str, Any],
        script_analysis: dict[str, Any],
        seed_candidates: list[dict[str, Any]],
        context: Any,
    ) -> list[dict[str, Any]]:
        router = context.get_service("model_router")
        if not router:
            return []

        base_dir = Path(context.get_service("base_dir") or ".")
        system_prompt = read_text(base_dir / "prompts" / "feature_planner_prompt.md")
        project_summary = self._compact_project_summary(project_scan, script_analysis)
        messages = [
            {
                "role": "system",
                "content": (
                    system_prompt
                    + "\nReturn JSON only. Do not include markdown or commentary."
                ),
            },
            {
                "role": "user",
                "content": (
                    "Review this Unity project summary and propose up to 4 additional safe "
                    "automation candidates. Prefer readonly Editor/report tools. Avoid scene, "
                    "prefab, ScriptableObject, StreamingAssets, Addressables, save data, "
                    "networking, build settings, and destructive file changes.\n\n"
                    "JSON schema:\n"
                    "{\n"
                    "  \"candidates\": [\n"
                    "    {\n"
                    "      \"candidate_id\": \"llm_001_short_slug\",\n"
                    "      \"feature_name\": \"Name\",\n"
                    "      \"description\": \"One or two sentences\",\n"
                    "      \"value\": \"Why it helps\",\n"
                    "      \"risk_level\": \"low|medium|high\",\n"
                    "      \"affected_files\": [],\n"
                    "      \"implementation_type\": \"readonly_tool|editor_tool|report_tool|runtime_feature\",\n"
                    "      \"status\": \"pending\",\n"
                    "      \"signals\": {}\n"
                    "    }\n"
                    "  ]\n"
                    "}\n\n"
                    f"Project summary:\n{compact_json(project_summary, 9000)}\n\n"
                    f"Seed candidates:\n{compact_json(seed_candidates, 5000)}"
                ),
            },
        ]
        response = router.chat_for_task("discover_feature_gap", messages)
        parsed = extract_json_value(response.get("content", ""))
        raw_candidates = parsed.get("candidates", []) if isinstance(parsed, dict) else []
        candidates = [
            candidate
            for index, item in enumerate(raw_candidates, start=1)
            if (candidate := self._normalize_llm_candidate(item, index))
        ]
        record_model_call(
            context,
            "discover_feature_gap",
            response,
            used=bool(candidates),
            note="accepted_candidates=%s" % len(candidates),
        )
        return candidates

    def _compact_project_summary(
        self,
        project_scan: dict[str, Any],
        script_analysis: dict[str, Any],
    ) -> dict[str, Any]:
        samples = project_scan.get("samples", {})
        scripts = script_analysis.get("scripts", [])
        notable_scripts = [
            {
                "path": item.get("path"),
                "classes": item.get("classes", []),
                "unity_types": item.get("unity_types", []),
                "lifecycle_methods": item.get("lifecycle_methods", []),
                "public_fields": item.get("public_fields", 0),
                "serialize_fields": item.get("serialize_fields", 0),
                "line_count": item.get("line_count", 0),
            }
            for item in scripts[:40]
        ]
        return {
            "project_counts": project_scan.get("counts", {}),
            "detected_directories": project_scan.get("detected_directories", {}),
            "samples": {
                key: list(value)[:8] if isinstance(value, list) else value
                for key, value in samples.items()
            },
            "script_summary": script_analysis.get("summary", {}),
            "total_scripts": script_analysis.get("total_scripts", 0),
            "notable_scripts": notable_scripts,
        }

    def _normalize_llm_candidate(self, item: Any, index: int) -> dict[str, Any] | None:
        if not isinstance(item, dict):
            return None
        feature_name = str(item.get("feature_name") or "").strip()
        description = str(item.get("description") or "").strip()
        if not feature_name or not description:
            return None

        raw_id = str(item.get("candidate_id") or f"llm_{index:03d}_{feature_name}")
        candidate_id = safe_slug(raw_id, 72)
        if not candidate_id.startswith(("cand_", "llm_")):
            candidate_id = f"llm_{index:03d}_{candidate_id}"

        risk_level = str(item.get("risk_level") or "medium").strip().lower()
        if risk_level not in {"low", "medium", "high"}:
            risk_level = "medium"

        implementation_type = str(item.get("implementation_type") or "report_tool").strip()
        if implementation_type not in {
            "readonly_tool",
            "editor_tool",
            "report_tool",
            "runtime_feature",
        }:
            implementation_type = "report_tool"

        affected_files = item.get("affected_files", [])
        if not isinstance(affected_files, list):
            affected_files = []

        signals = item.get("signals", {})
        if not isinstance(signals, dict):
            signals = {}

        return {
            "candidate_id": candidate_id,
            "feature_name": feature_name,
            "description": description,
            "value": str(item.get("value") or "LLM-proposed safe project automation.").strip(),
            "risk_level": risk_level,
            "affected_files": affected_files[:12],
            "implementation_type": implementation_type,
            "status": "pending",
            "signals": signals,
        }

    def _merge_candidates(
        self,
        local_candidates: list[dict[str, Any]],
        llm_candidates: list[dict[str, Any]],
    ) -> list[dict[str, Any]]:
        merged: dict[str, dict[str, Any]] = {}
        for candidate in local_candidates + llm_candidates:
            candidate_id = candidate.get("candidate_id")
            if not candidate_id:
                continue
            if candidate_id in merged:
                merged[candidate_id] = {**merged[candidate_id], **candidate}
            else:
                merged[candidate_id] = candidate
        return list(merged.values())

    def _priority_key(self, candidate: dict[str, Any]) -> tuple[int, int, str]:
        risk_order = {"low": 0, "medium": 1, "high": 2}
        type_order = {"readonly_tool": 0, "editor_tool": 1, "report_tool": 2, "runtime_feature": 3}
        status_penalty = 10 if candidate.get("status") != "pending" else 0
        return (
            risk_order.get(candidate.get("risk_level"), 9) + status_penalty,
            type_order.get(candidate.get("implementation_type"), 9),
            candidate.get("candidate_id", ""),
        )
