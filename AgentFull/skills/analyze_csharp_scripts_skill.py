from __future__ import annotations

import re
from pathlib import Path
from typing import Any

from core.cache import build_fingerprint
from core.file_utils import path_to_posix, read_text, resolve_path
from core.skill import Skill


CLASS_RE = re.compile(
    r"\b(?P<kind>class|struct|interface)\s+(?P<name>[A-Za-z_][A-Za-z0-9_]*)(?:\s*:\s*(?P<base>[^{\n]+))?",
    re.MULTILINE,
)
NAMESPACE_RE = re.compile(r"\bnamespace\s+([A-Za-z_][A-Za-z0-9_.]*)")
PUBLIC_FIELD_RE = re.compile(
    r"^\s*public\s+(?!class\b|struct\b|interface\b|enum\b|void\b)[^=(;]+\s+[A-Za-z_][A-Za-z0-9_]*\s*(?:=|;)",
    re.MULTILINE,
)
SERIALIZE_FIELD_RE = re.compile(r"\[SerializeField\]", re.MULTILINE)
LIFECYCLE_METHODS = ["Awake", "OnEnable", "Start", "Update", "FixedUpdate", "LateUpdate", "OnDisable", "OnDestroy"]


class AnalyzeCSharpScriptsSkill(Skill):
    name = "analyze_csharp_scripts"
    description = "Extract classes, namespaces, inheritance, and Unity lifecycle usage from C# files."
    input_schema = {"assets_path": "optional path override"}
    output_schema = {"scripts": "script details", "summary": "aggregate counts"}

    def run(self, params: dict[str, Any], context: Any) -> dict[str, Any]:
        base_dir = context.get_service("base_dir")
        unity_cfg = context.get("configs", {}).get("unity", {}).get("unity_project", {})
        assets_path = resolve_path(base_dir, params.get("assets_path") or unity_cfg.get("assets_path"))
        if assets_path is None:
            raise ValueError("Unity assets_path is not configured.")

        cache = context.get_service("cache")
        fingerprint = build_fingerprint(assets_path, ["*.cs"], excluded_dirs={"AgentFull", "Library", "Temp", ".git"})
        if cache:
            cached = cache.get("script_analysis_cache.json", fingerprint)
            if cached:
                cached["from_cache"] = True
                return cached

        result = self._analyze(assets_path)
        result["fingerprint"] = fingerprint
        result["from_cache"] = False
        if cache:
            cache.set("script_analysis_cache.json", fingerprint, result)
        return result

    def _analyze(self, assets_path: Path) -> dict[str, Any]:
        scripts: list[dict[str, Any]] = []
        summary = {
            "mono_behaviour_classes": 0,
            "scriptable_object_classes": 0,
            "editor_window_classes": 0,
            "plain_classes": 0,
            "public_fields": 0,
            "serialize_fields": 0,
            "lifecycle_methods": {name: 0 for name in LIFECYCLE_METHODS},
        }

        for path in sorted(assets_path.rglob("*.cs")):
            if self._is_excluded(path):
                continue
            content = read_text(path)
            namespaces = NAMESPACE_RE.findall(content)
            classes = []
            unity_types: set[str] = set()
            for match in CLASS_RE.finditer(content):
                base_raw = (match.group("base") or "").strip()
                bases = [item.strip().split("<")[0] for item in base_raw.split(",") if item.strip()]
                class_info = {
                    "kind": match.group("kind"),
                    "name": match.group("name"),
                    "inherits": bases,
                }
                classes.append(class_info)
                inherited_text = ",".join(bases)
                if "MonoBehaviour" in inherited_text:
                    unity_types.add("MonoBehaviour")
                    summary["mono_behaviour_classes"] += 1
                elif "ScriptableObject" in inherited_text:
                    unity_types.add("ScriptableObject")
                    summary["scriptable_object_classes"] += 1
                elif "EditorWindow" in inherited_text:
                    unity_types.add("EditorWindow")
                    summary["editor_window_classes"] += 1
                elif match.group("kind") == "class":
                    summary["plain_classes"] += 1

            lifecycle_methods = []
            for method_name in LIFECYCLE_METHODS:
                if re.search(rf"\b{method_name}\s*\(", content):
                    lifecycle_methods.append(method_name)
                    summary["lifecycle_methods"][method_name] += 1

            public_fields = len(PUBLIC_FIELD_RE.findall(content))
            serialize_fields = len(SERIALIZE_FIELD_RE.findall(content))
            summary["public_fields"] += public_fields
            summary["serialize_fields"] += serialize_fields

            try:
                rel_path = path_to_posix(path.relative_to(assets_path))
            except ValueError:
                rel_path = path_to_posix(path)
            scripts.append(
                {
                    "path": rel_path,
                    "namespaces": namespaces,
                    "classes": classes,
                    "unity_types": sorted(unity_types),
                    "public_fields": public_fields,
                    "serialize_fields": serialize_fields,
                    "lifecycle_methods": lifecycle_methods,
                    "line_count": content.count("\n") + 1 if content else 0,
                }
            )

        return {
            "assets_path": str(assets_path),
            "total_scripts": len(scripts),
            "summary": summary,
            "scripts": scripts,
        }

    def _is_excluded(self, path: Path) -> bool:
        lowered = {part.lower() for part in path.parts}
        return bool(lowered.intersection({"agentfull", "library", "temp", ".git"}))
