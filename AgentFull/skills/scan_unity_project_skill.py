from __future__ import annotations

from pathlib import Path
from typing import Any

from core.cache import build_fingerprint
from core.file_utils import path_to_posix, resolve_path
from core.skill import Skill


class ScanUnityProjectSkill(Skill):
    name = "scan_unity_project"
    description = "Scan Unity project folders and count common asset types."
    input_schema = {"project_root": "optional path override"}
    output_schema = {"counts": "asset counts", "detected_directories": "known Unity dirs"}

    def run(self, params: dict[str, Any], context: Any) -> dict[str, Any]:
        base_dir = context.get_service("base_dir")
        unity_cfg = context.get("configs", {}).get("unity", {}).get("unity_project", {})
        root_path = resolve_path(base_dir, params.get("project_root") or unity_cfg.get("root_path"))
        assets_path = resolve_path(base_dir, unity_cfg.get("assets_path"))
        if root_path is None or assets_path is None:
            raise ValueError("Unity root_path/assets_path are not configured.")

        cache = context.get_service("cache")
        fingerprint = build_fingerprint(
            assets_path,
            ["*.cs", "*.prefab", "*.unity", "*.mat", "*.asset", "*.png", "*.jpg", "*.jpeg", "*.tga", "*.psd"],
            excluded_dirs={"AgentFull", "Library", "Temp", ".git"},
        )
        if cache:
            cached = cache.get("project_scan_cache.json", fingerprint)
            if cached:
                cached["from_cache"] = True
                return cached

        result = self._scan(root_path, assets_path, unity_cfg, base_dir)
        result["fingerprint"] = fingerprint
        result["from_cache"] = False
        if cache:
            cache.set("project_scan_cache.json", fingerprint, result)
        return result

    def _scan(
        self,
        root_path: Path,
        assets_path: Path,
        unity_cfg: dict[str, Any],
        base_dir: Path,
    ) -> dict[str, Any]:
        known_dirs = {
            "Assets": assets_path,
            "ProjectSettings": root_path / "ProjectSettings",
            "Packages": root_path / "Packages",
            "Scenes": self._resolve_configured(base_dir, unity_cfg.get("scenes_path")),
            "Scripts": self._resolve_configured(base_dir, unity_cfg.get("scripts_path")),
            "Prefabs": self._resolve_configured(base_dir, unity_cfg.get("prefabs_path")),
            "Resources": self._resolve_configured(base_dir, unity_cfg.get("resources_path")),
            "StreamingAssets": self._resolve_configured(base_dir, unity_cfg.get("streaming_assets_path")),
            "Addressables": self._resolve_configured(base_dir, unity_cfg.get("addressables_path")),
        }
        detected = {
            name: {"path": str(path), "exists": path.exists()}
            for name, path in known_dirs.items()
        }

        counts = {
            "csharp_files": self._count(assets_path, "*.cs"),
            "prefabs": self._count(assets_path, "*.prefab"),
            "scenes": self._count(assets_path, "*.unity"),
            "materials": self._count(assets_path, "*.mat"),
            "textures": self._count_many(assets_path, ["*.png", "*.jpg", "*.jpeg", "*.tga", "*.psd", "*.tif", "*.tiff"]),
            "scriptable_assets": self._count(assets_path, "*.asset"),
            "meta_files": self._count(assets_path, "*.meta"),
        }
        samples = {
            "scripts": self._sample(assets_path, "*.cs"),
            "scenes": self._sample(assets_path, "*.unity"),
            "prefabs": self._sample(assets_path, "*.prefab"),
            "materials": self._sample(assets_path, "*.mat"),
            "scriptable_assets": self._sample(assets_path, "*.asset"),
        }
        summary = (
            f"Unity 项目扫描发现 {counts['csharp_files']} 个 C# 文件、"
            f"{counts['scenes']} 个场景、{counts['prefabs']} 个 Prefab、"
            f"{counts['materials']} 个材质、{counts['textures']} 个贴图。"
        )
        return {
            "root_path": str(root_path),
            "assets_path": str(assets_path),
            "detected_directories": detected,
            "counts": counts,
            "samples": samples,
            "summary": summary,
        }

    def _resolve_configured(self, base_dir: Path, value: str | None) -> Path:
        if not value:
            return base_dir
        resolved = resolve_path(base_dir, value)
        return resolved or base_dir

    def _iter_files(self, root: Path, pattern: str):
        for path in root.rglob(pattern):
            if not path.is_file():
                continue
            lowered = {part.lower() for part in path.parts}
            if lowered.intersection({"agentfull", "library", "temp", ".git"}):
                continue
            yield path

    def _count(self, root: Path, pattern: str) -> int:
        return sum(1 for _ in self._iter_files(root, pattern))

    def _count_many(self, root: Path, patterns: list[str]) -> int:
        return sum(self._count(root, pattern) for pattern in patterns)

    def _sample(self, root: Path, pattern: str, limit: int = 12) -> list[str]:
        items = []
        for path in self._iter_files(root, pattern):
            try:
                items.append(path_to_posix(path.relative_to(root)))
            except ValueError:
                items.append(path_to_posix(path))
            if len(items) >= limit:
                break
        return items
