from __future__ import annotations

from pathlib import Path
from typing import Any

from core.file_utils import read_text, safe_slug
from core.llm_utils import compact_json, extract_json_value, record_model_call
from core.skill import Skill


class DiscoverFeatureGapSkill(Skill):
    name = "discover_feature_gap"
    description = "Create project-specific feature candidates from Unity project analysis."
    input_schema = {"project_scan": "scan result", "script_analysis": "script analysis result"}
    output_schema = {"candidates": "candidate feature list"}

    def run(self, params: dict[str, Any], context: Any) -> dict[str, Any]:
        project_scan = params.get("project_scan") or {}
        script_analysis = params.get("script_analysis") or {}
        memory = context.get_service("memory")

        local_candidates = self._project_feature_candidates(project_scan, script_analysis)

        llm_candidates = self._llm_candidates(project_scan, script_analysis, local_candidates, context)
        candidates = self._merge_candidates(local_candidates, llm_candidates)
        active_candidate_ids = {item.get("candidate_id") for item in candidates if item.get("candidate_id")}

        if memory:
            merged = memory.merge_feature_candidates(candidates)
            candidates = [
                item
                for item in merged
                if item.get("candidate_id") in active_candidate_ids
                or item.get("status") in {"completed", "skipped"}
            ]

        ordered = sorted(candidates, key=self._priority_key)
        return {"candidates": ordered}

    def _project_feature_candidates(
        self,
        project_scan: dict[str, Any],
        script_analysis: dict[str, Any],
    ) -> list[dict[str, Any]]:
        counts = project_scan.get("counts", {})
        scripts = script_analysis.get("scripts", [])
        summary = script_analysis.get("summary", {})
        names, text_index = self._project_terms(scripts)

        candidates: list[dict[str, Any]] = []

        has_character_system = self._has_any(text_index, names, ["character", "player", "enemy"])
        has_status_system = self._has_any(text_index, names, ["statuseffect", "status_effect", "buff", "debuff"])
        if has_character_system and not has_status_system:
            candidates.append(
                self._candidate(
                    "auto_001_status_effect_controller",
                    "Status Effect Controller",
                    (
                        "Add a reusable runtime component for timed buffs, debuffs, slows, "
                        "and regeneration that can be attached to character prefabs manually."
                    ),
                    "Extends the existing character/combat foundation without editing existing scripts.",
                    "low",
                    "StatusEffectController",
                    {
                        "matched_system": "character",
                        "mono_behaviour_classes": summary.get("mono_behaviour_classes", 0),
                    },
                )
            )

        has_world_or_map = self._has_any(text_index, names, ["tilemap", "map", "world", "resource"])
        has_weather = self._has_any(text_index, names, ["weather", "season", "climate"])
        if has_world_or_map and not has_weather:
            candidates.append(
                self._candidate(
                    "auto_002_world_weather_cycle",
                    "World Weather Cycle Controller",
                    (
                        "Add a runtime weather-cycle component that rotates weather states, "
                        "exposes current intensity, and raises events other systems can subscribe to."
                    ),
                    "Creates a new world-simulation hook that can be integrated gradually.",
                    "low",
                    "WorldWeatherCycleController",
                    {
                        "matched_system": "world_map",
                        "scenes": counts.get("scenes", 0),
                    },
                )
            )

        has_worker_tasks = self._has_any(text_index, names, ["worker", "workertask", "gather", "hungry", "sleep"])
        has_morale = self._has_any(text_index, names, ["morale", "happiness", "satisfaction", "mood"])
        if has_worker_tasks and not has_morale:
            candidates.append(
                self._candidate(
                    "auto_003_worker_morale_controller",
                    "Worker Morale Controller",
                    (
                        "Add a standalone runtime morale component with decay, recovery, "
                        "threshold events, and task-readiness helpers for worker gameplay."
                    ),
                    "Builds on the worker/task system with a new balancing lever.",
                    "low",
                    "WorkerMoraleController",
                    {
                        "matched_system": "worker_tasks",
                        "worker_scripts": self._count_term(scripts, "worker"),
                    },
                )
            )

        has_resources = self._has_any(text_index, names, ["resource", "item", "inventory", "drop"])
        has_resource_alerts = self._has_any(text_index, names, ["threshold", "alert", "stockpile"])
        if has_resources and not has_resource_alerts:
            candidates.append(
                self._candidate(
                    "auto_004_resource_threshold_alerts",
                    "Resource Threshold Alert Controller",
                    (
                        "Add a runtime resource-threshold component that tracks named resource "
                        "amounts and raises events when stock falls below configured limits."
                    ),
                    "Provides a low-risk foundation for UI alerts, worker priorities, and colony feedback.",
                    "low",
                    "ResourceThresholdAlertController",
                    {
                        "matched_system": "resources",
                        "scriptable_assets": counts.get("scriptable_assets", 0),
                    },
                )
            )

        has_combat = self._has_any(text_index, names, ["attack", "damage", "enemy", "weapon"])
        has_combat_feed = self._has_any(text_index, names, ["combatlog", "battlelog", "damagefeed", "eventfeed"])
        if has_combat and not has_combat_feed:
            candidates.append(
                self._candidate(
                    "auto_005_combat_event_feed",
                    "Combat Event Feed",
                    (
                        "Add a runtime event-feed component for damage, healing, deaths, and combat notes "
                        "so UI or debugging tools can subscribe without coupling to combat classes."
                    ),
                    "Makes combat feedback easier to surface while keeping the generated code isolated.",
                    "low",
                    "CombatEventFeed",
                    {
                        "matched_system": "combat",
                        "prefabs": counts.get("prefabs", 0),
                    },
                )
            )

        if not candidates:
            candidates.append(
                self._candidate(
                    "auto_001_gameplay_event_channel",
                    "Gameplay Event Channel",
                    (
                        "Add a small runtime event-channel component that can broadcast named gameplay "
                        "events to UI, debugging, or future systems."
                    ),
                    "Creates a safe extension point when no stronger project-specific gap is detected.",
                    "low",
                    "GameplayEventChannel",
                    {"matched_system": "fallback", "csharp_files": counts.get("csharp_files", 0)},
                )
            )

        return candidates

    def _candidate(
        self,
        candidate_id: str,
        feature_name: str,
        description: str,
        value: str,
        risk_level: str,
        suggested_class_name: str,
        signals: dict[str, Any],
    ) -> dict[str, Any]:
        return {
            "candidate_id": candidate_id,
            "feature_name": feature_name,
            "description": description,
            "value": value,
            "risk_level": risk_level,
            "affected_files": [],
            "implementation_type": "runtime_feature",
            "status": "pending",
            "suggested_class_name": suggested_class_name,
            "source": "project_heuristic",
            "signals": signals,
        }

    def _project_terms(self, scripts: list[dict[str, Any]]) -> tuple[set[str], str]:
        names: set[str] = set()
        text_parts: list[str] = []
        for script in scripts:
            path = str(script.get("path", "")).lower()
            text_parts.append(path)
            for item in script.get("classes", []):
                name = str(item.get("name", ""))
                if name:
                    names.add(name.lower())
                    text_parts.append(name.lower())
                for inherited in item.get("inherits", []):
                    text_parts.append(str(inherited).lower())
            for namespace in script.get("namespaces", []):
                text_parts.append(str(namespace).lower())
            for unity_type in script.get("unity_types", []):
                text_parts.append(str(unity_type).lower())
        return names, "\n".join(text_parts)

    def _has_any(self, text_index: str, names: set[str], terms: list[str]) -> bool:
        lowered_terms = [term.lower() for term in terms]
        return any(term in text_index or term in names for term in lowered_terms)

    def _count_term(self, scripts: list[dict[str, Any]], term: str) -> int:
        lowered = term.lower()
        return sum(1 for script in scripts if lowered in str(script.get("path", "")).lower())

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
                    "Review this Unity project summary and propose up to 4 additional new "
                    "gameplay or project-system features that can be developed as isolated new "
                    "C# files. Prefer standalone runtime MonoBehaviour components that can be "
                    "attached manually. Avoid requiring edits to existing scenes, prefabs, "
                    "ScriptableObjects, StreamingAssets, Addressables, save data, networking, "
                    "build settings, or destructive file changes.\n\n"
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
                    "      \"implementation_type\": \"runtime_feature|editor_tool|report_tool\",\n"
                    "      \"status\": \"pending\",\n"
                    "      \"suggested_class_name\": \"PascalCaseClassName\",\n"
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

        implementation_type = str(item.get("implementation_type") or "runtime_feature").strip().lower()
        if implementation_type in {"gameplay_feature", "runtime_component", "system_feature"}:
            implementation_type = "runtime_feature"
        if implementation_type not in {
            "editor_tool",
            "report_tool",
            "runtime_feature",
        }:
            implementation_type = "runtime_feature"

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
            "value": str(item.get("value") or "LLM-proposed project feature.").strip(),
            "risk_level": risk_level,
            "affected_files": affected_files[:12],
            "implementation_type": implementation_type,
            "status": "pending",
            "suggested_class_name": str(item.get("suggested_class_name") or "").strip(),
            "source": "llm",
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
        type_order = {"runtime_feature": 0, "editor_tool": 1, "report_tool": 2, "readonly_tool": 3}
        status_penalty = 10 if candidate.get("status") != "pending" else 0
        return (
            risk_order.get(candidate.get("risk_level"), 9) + status_penalty,
            type_order.get(candidate.get("implementation_type"), 9),
            candidate.get("candidate_id", ""),
        )
