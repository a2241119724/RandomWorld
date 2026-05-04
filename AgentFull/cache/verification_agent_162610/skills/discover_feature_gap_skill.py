from __future__ import annotations

from typing import Any

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
        candidates = [
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

        if memory:
            candidates = memory.merge_feature_candidates(candidates)

        ordered = sorted(candidates, key=self._priority_key)
        return {"candidates": ordered}

    def _priority_key(self, candidate: dict[str, Any]) -> tuple[int, int, str]:
        risk_order = {"low": 0, "medium": 1, "high": 2}
        type_order = {"readonly_tool": 0, "editor_tool": 1, "report_tool": 2, "runtime_feature": 3}
        status_penalty = 10 if candidate.get("status") != "pending" else 0
        return (
            risk_order.get(candidate.get("risk_level"), 9) + status_penalty,
            type_order.get(candidate.get("implementation_type"), 9),
            candidate.get("candidate_id", ""),
        )
