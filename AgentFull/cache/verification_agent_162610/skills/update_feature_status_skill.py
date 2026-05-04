from __future__ import annotations

from typing import Any

from core.skill import Skill


class UpdateFeatureStatusSkill(Skill):
    name = "update_feature_status"
    description = "Update memory/feature_candidates.json to prevent duplicate work."
    input_schema = {"candidate_id": "candidate id", "status": "completed|skipped"}
    output_schema = {"candidate_id": "updated candidate id", "status": "new status"}

    def run(self, params: dict[str, Any], context: Any) -> dict[str, Any]:
        candidate_id = params.get("candidate_id")
        status = params.get("status")
        note = params.get("note", "")
        if not candidate_id:
            raise ValueError("candidate_id is required.")
        if status not in {"pending", "completed", "skipped"}:
            raise ValueError("status must be pending, completed, or skipped.")

        memory = context.get_service("memory")
        if not memory:
            raise RuntimeError("Memory service is unavailable.")
        candidates = memory.update_feature_status(candidate_id, status, note)
        context.set("feature_candidates", candidates)
        return {
            "candidate_id": candidate_id,
            "status": status,
            "note": note,
            "candidate_count": len(candidates),
        }
