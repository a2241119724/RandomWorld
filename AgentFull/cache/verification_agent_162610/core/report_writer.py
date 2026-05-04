from __future__ import annotations

from datetime import datetime
from pathlib import Path
from typing import Any

from .file_utils import ensure_dir, safe_slug, save_json, write_text


class ReportWriter:
    def __init__(self, reports_dir: Path) -> None:
        self.reports_dir = ensure_dir(reports_dir)

    def create_run_dir(self, task_id: str) -> Path:
        date_dir = ensure_dir(self.reports_dir / datetime.now().strftime("%Y-%m-%d"))
        run_dir = date_dir / safe_slug(f"run_{datetime.now().strftime('%H%M%S')}")
        return self._unique_dir(run_dir)

    def create_candidate_dir(self, candidate: dict[str, Any]) -> Path:
        date_dir = ensure_dir(self.reports_dir / datetime.now().strftime("%Y-%m-%d"))
        candidate_id = candidate.get("candidate_id", "candidate")
        feature_name = candidate.get("feature_name", "feature")
        folder_name = safe_slug(f"{candidate_id}_{feature_name}", 72)
        return self._unique_dir(date_dir / folder_name)

    def write_report(self, report_dir: Path, markdown: str) -> Path:
        return write_text(report_dir / "report.md", markdown, overwrite=True)

    def write_json(self, report_dir: Path, name: str, data: Any) -> Path:
        return save_json(report_dir / name, data)

    def _unique_dir(self, path: Path) -> Path:
        if not path.exists():
            path.mkdir(parents=True)
            return path
        index = 1
        while True:
            candidate = path.parent / f"{path.name}_{index}"
            if not candidate.exists():
                candidate.mkdir(parents=True)
                return candidate
            index += 1
