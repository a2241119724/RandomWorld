from __future__ import annotations

import re
from pathlib import Path
from typing import Any

from core.file_utils import path_to_posix, read_text, resolve_path
from core.skill import Skill


GUID_RE = re.compile(r"guid:\s*([0-9a-fA-F]{32})")
META_GUID_RE = re.compile(r"^guid:\s*([0-9a-fA-F]{32})", re.MULTILINE)


class AssetReferenceCheckSkill(Skill):
    name = "asset_reference_check"
    description = "Readonly scan for missing Unity references and suspicious GUIDs."
    input_schema = {"assets_path": "optional path override"}
    output_schema = {"findings": "reference risk findings"}

    def run(self, params: dict[str, Any], context: Any) -> dict[str, Any]:
        base_dir = context.get_service("base_dir")
        unity_cfg = context.get("configs", {}).get("unity", {}).get("unity_project", {})
        assets_path = resolve_path(base_dir, params.get("assets_path") or unity_cfg.get("assets_path"))
        if assets_path is None:
            raise ValueError("Unity assets_path is not configured.")

        meta_guids = self._collect_meta_guids(assets_path)
        findings: list[dict[str, Any]] = []
        scanned_files = 0
        unknown_guid_samples: list[dict[str, str]] = []

        for path in self._iter_asset_yaml_files(assets_path):
            scanned_files += 1
            content = read_text(path)
            rel = path_to_posix(path.relative_to(assets_path))
            file_findings = []
            if "m_Script: {fileID: 0" in content or "Missing (Mono Script)" in content:
                file_findings.append("missing_script_marker")
            if re.search(r"guid:\s*(0{32}|,|\n)", content):
                file_findings.append("empty_or_zero_guid")

            for guid in GUID_RE.findall(content):
                normalized = guid.lower()
                if normalized == "0" * 32:
                    continue
                if normalized not in meta_guids and len(unknown_guid_samples) < 30:
                    unknown_guid_samples.append({"path": rel, "guid": normalized})

            for finding in file_findings:
                findings.append({"path": rel, "risk": finding})
                if len(findings) >= 100:
                    break

        risk_level = "low"
        if any(item["risk"] == "missing_script_marker" for item in findings):
            risk_level = "medium"
        return {
            "assets_path": str(assets_path),
            "scanned_files": scanned_files,
            "finding_count": len(findings),
            "findings": findings[:100],
            "external_or_missing_guid_samples": unknown_guid_samples,
            "risk_level": risk_level,
            "readonly": True,
            "notes": [
                "Unknown GUID samples may include package references; review before treating them as broken.",
                "This check does not modify assets.",
            ],
        }

    def _collect_meta_guids(self, assets_path: Path) -> set[str]:
        guids: set[str] = set()
        for meta_path in assets_path.rglob("*.meta"):
            if self._is_excluded(meta_path):
                continue
            match = META_GUID_RE.search(read_text(meta_path))
            if match:
                guids.add(match.group(1).lower())
        return guids

    def _iter_asset_yaml_files(self, assets_path: Path):
        patterns = ["*.prefab", "*.unity", "*.mat", "*.asset"]
        for pattern in patterns:
            for path in sorted(assets_path.rglob(pattern)):
                if path.is_file() and not self._is_excluded(path):
                    yield path

    def _is_excluded(self, path: Path) -> bool:
        lowered = {part.lower() for part in path.parts}
        return bool(lowered.intersection({"agentfull", "library", "temp", ".git"}))
