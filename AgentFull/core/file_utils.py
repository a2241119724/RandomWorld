from __future__ import annotations

import json
import re
from pathlib import Path
from typing import Any

try:
    import yaml
except ImportError:  # pragma: no cover - handled at runtime with a clear message.
    yaml = None


def ensure_dir(path: Path | str) -> Path:
    target = Path(path)
    target.mkdir(parents=True, exist_ok=True)
    return target


def load_json(path: Path | str, default: Any = None) -> Any:
    target = Path(path)
    if not target.exists():
        return default
    with target.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def save_json(path: Path | str, data: Any) -> Path:
    target = Path(path)
    ensure_dir(target.parent)
    with target.open("w", encoding="utf-8") as handle:
        json.dump(data, handle, ensure_ascii=False, indent=2)
    return target


def load_yaml(path: Path | str) -> dict:
    if yaml is None:
        raise RuntimeError(
            "PyYAML is not installed. Run: pip install -r AgentFull/requirements.txt"
        )
    target = Path(path)
    with target.open("r", encoding="utf-8") as handle:
        return yaml.safe_load(handle) or {}


def save_yaml(path: Path | str, data: dict) -> Path:
    if yaml is None:
        raise RuntimeError(
            "PyYAML is not installed. Run: pip install -r AgentFull/requirements.txt"
        )
    target = Path(path)
    ensure_dir(target.parent)
    with target.open("w", encoding="utf-8") as handle:
        yaml.safe_dump(data, handle, allow_unicode=True, sort_keys=False)
    return target


def read_text(path: Path | str, default: str = "") -> str:
    target = Path(path)
    if not target.exists():
        return default
    return target.read_text(encoding="utf-8", errors="ignore")


def write_text(path: Path | str, content: str, overwrite: bool = False) -> Path:
    target = Path(path)
    ensure_dir(target.parent)
    if target.exists() and not overwrite:
        target = unique_path(target)
    target.write_text(content, encoding="utf-8")
    return target


def unique_path(path: Path | str) -> Path:
    target = Path(path)
    if not target.exists():
        return target
    stem = target.stem
    suffix = target.suffix
    parent = target.parent
    index = 1
    while True:
        candidate = parent / f"{stem}_{index}{suffix}"
        if not candidate.exists():
            return candidate
        index += 1


def safe_slug(value: str, max_length: int = 48) -> str:
    slug = re.sub(r"[^A-Za-z0-9._-]+", "_", value.strip())
    slug = re.sub(r"_+", "_", slug).strip("._-")
    return (slug or "task")[:max_length]


def resolve_path(base_dir: Path, value: str | None) -> Path | None:
    if value in (None, ""):
        return None
    raw = Path(str(value)).expanduser()
    if raw.is_absolute():
        return raw.resolve()
    return (base_dir / raw).resolve()


def is_relative_to(path: Path, parent: Path) -> bool:
    try:
        path.resolve().relative_to(parent.resolve())
        return True
    except ValueError:
        return False


def path_to_posix(path: Path | str) -> str:
    return Path(path).as_posix()


def unity_project_config(configs: dict[str, Any]) -> dict[str, Any]:
    unity_config = configs.get("unity", configs)
    return unity_config.get("unity_project", unity_config)


def unity_generation_policy(configs: dict[str, Any]) -> dict[str, Any]:
    unity_config = configs.get("unity", configs)
    return unity_config.get("generation_policy", {})


def resolve_unity_project_path(
    base_dir: Path,
    configs: dict[str, Any],
    key: str,
    fallback_under_assets: str,
) -> Path:
    unity_project = unity_project_config(configs)
    configured = resolve_path(base_dir, unity_project.get(key))
    if configured is not None:
        return configured

    assets_path = resolve_path(base_dir, unity_project.get("assets_path"))
    if assets_path is None:
        assets_path = base_dir.parent
    return assets_path / fallback_under_assets


def csharp_class_name(value: str | None, fallback: str = "GeneratedUnityTool") -> str:
    slug = safe_slug(value or fallback, 64)
    parts = re.split(r"[-_.]+", slug)
    class_name = "".join(part[:1].upper() + part[1:] for part in parts if part)
    return class_name or fallback


def csharp_output_dir(
    base_dir: Path,
    configs: dict[str, Any],
    report_dir: Path,
    *,
    script_kind: str | None = None,
    implementation_type: str | None = None,
) -> Path:
    output_mode = unity_generation_policy(configs).get("default_output_mode", "project")
    if output_mode == "report_only":
        return report_dir / "generated_code"

    is_editor_code = implementation_type in {"readonly_tool", "editor_tool", "report_tool"}
    is_editor_code = is_editor_code or script_kind in {"Editor", "EditorWindow", "EditorTool"}
    if is_editor_code:
        return resolve_unity_project_path(base_dir, configs, "editor_path", "Editor")
    return resolve_unity_project_path(base_dir, configs, "scripts_path", "Scripts")
