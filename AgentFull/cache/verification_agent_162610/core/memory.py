from __future__ import annotations

from datetime import datetime
from pathlib import Path
from typing import Any

from .file_utils import ensure_dir, load_json, save_json


class Memory:
    def __init__(self, memory_dir: Path) -> None:
        self.memory_dir = ensure_dir(memory_dir)
        self.short_term_path = self.memory_dir / "short_term_memory.json"
        self.long_term_path = self.memory_dir / "long_term_memory.json"
        self.feature_candidates_path = self.memory_dir / "feature_candidates.json"
        self.ensure_files()

    def ensure_files(self) -> None:
        if not self.short_term_path.exists():
            save_json(self.short_term_path, {})
        if not self.long_term_path.exists():
            save_json(
                self.long_term_path,
                {
                    "project_summary": {},
                    "preferences": {
                        "default_output_mode": "project",
                        "readonly_first": True,
                    },
                    "updated_at": datetime.now().isoformat(timespec="seconds"),
                },
            )
        if not self.feature_candidates_path.exists():
            save_json(self.feature_candidates_path, {"candidates": []})

    def load_short_term(self) -> dict[str, Any]:
        return load_json(self.short_term_path, {})

    def save_short_term(self, data: dict[str, Any]) -> None:
        save_json(self.short_term_path, data)

    def load_long_term(self) -> dict[str, Any]:
        return load_json(self.long_term_path, {})

    def save_long_term(self, data: dict[str, Any]) -> None:
        data["updated_at"] = datetime.now().isoformat(timespec="seconds")
        save_json(self.long_term_path, data)

    def load_feature_candidates(self) -> list[dict[str, Any]]:
        payload = load_json(self.feature_candidates_path, {"candidates": []})
        return list(payload.get("candidates", []))

    def save_feature_candidates(self, candidates: list[dict[str, Any]]) -> None:
        save_json(
            self.feature_candidates_path,
            {
                "updated_at": datetime.now().isoformat(timespec="seconds"),
                "candidates": candidates,
            },
        )

    def merge_feature_candidates(self, candidates: list[dict[str, Any]]) -> list[dict[str, Any]]:
        existing = {item.get("candidate_id"): item for item in self.load_feature_candidates()}
        for candidate in candidates:
            candidate_id = candidate.get("candidate_id")
            if not candidate_id:
                continue
            previous = existing.get(candidate_id, {})
            merged = {**candidate, **{k: v for k, v in previous.items() if k == "status"}}
            if "status" not in merged:
                merged["status"] = "pending"
            existing[candidate_id] = merged
        merged_candidates = list(existing.values())
        self.save_feature_candidates(merged_candidates)
        return merged_candidates

    def update_feature_status(
        self,
        candidate_id: str,
        status: str,
        note: str = "",
    ) -> list[dict[str, Any]]:
        candidates = self.load_feature_candidates()
        found = False
        for candidate in candidates:
            if candidate.get("candidate_id") == candidate_id:
                candidate["status"] = status
                candidate["status_note"] = note
                candidate["updated_at"] = datetime.now().isoformat(timespec="seconds")
                found = True
                break
        if not found:
            candidates.append(
                {
                    "candidate_id": candidate_id,
                    "feature_name": candidate_id,
                    "status": status,
                    "status_note": note,
                    "updated_at": datetime.now().isoformat(timespec="seconds"),
                }
            )
        self.save_feature_candidates(candidates)
        return candidates
