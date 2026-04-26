from __future__ import annotations

import json
from typing import Any


class ContextCompressor:
    def __init__(self, max_chars: int = 24000) -> None:
        self.max_chars = max_chars

    def compress(self, context_data: dict[str, Any]) -> dict[str, Any]:
        raw = json.dumps(context_data, ensure_ascii=False)
        if len(raw) <= self.max_chars:
            return context_data

        summary: dict[str, Any] = {
            "task": context_data.get("task"),
            "report_dir": context_data.get("report_dir"),
            "project_scan": self._compact_scan(context_data.get("project_scan", {})),
            "script_analysis": self._compact_scripts(context_data.get("script_analysis", {})),
            "candidates": context_data.get("feature_candidates", [])[:20],
            "selected_candidate": context_data.get("selected_candidate"),
            "task_card": context_data.get("task_card"),
            "generated_files": context_data.get("generated_files", []),
            "model_calls": context_data.get("model_calls", []),
            "validation": context_data.get("validation", {}),
            "errors": context_data.get("errors", []),
            "events": context_data.get("events", [])[-20:],
            "compressed": True,
        }
        return summary

    def _compact_scan(self, scan: dict[str, Any]) -> dict[str, Any]:
        if not scan:
            return {}
        return {
            "root_path": scan.get("root_path"),
            "assets_path": scan.get("assets_path"),
            "detected_directories": scan.get("detected_directories", {}),
            "counts": scan.get("counts", {}),
            "samples": scan.get("samples", {}),
        }

    def _compact_scripts(self, analysis: dict[str, Any]) -> dict[str, Any]:
        if not analysis:
            return {}
        scripts = analysis.get("scripts", [])
        compact_scripts = [
            {
                "path": item.get("path"),
                "classes": item.get("classes", []),
                "unity_types": item.get("unity_types", []),
                "lifecycle_methods": item.get("lifecycle_methods", []),
            }
            for item in scripts[:80]
        ]
        return {
            "total_scripts": analysis.get("total_scripts"),
            "summary": analysis.get("summary", {}),
            "scripts": compact_scripts,
        }
