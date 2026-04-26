from __future__ import annotations

import json
import re
from pathlib import Path
from typing import Any

from .file_utils import path_to_posix, read_text, resolve_path


DEFAULT_MAX_CONTEXT_CHARS = 60000
DEFAULT_MAX_SCRIPT_EXCERPTS = 60
DEFAULT_SCRIPT_EXCERPT_CHARS = 1400
DEFAULT_FILE_INDEX_LIMIT = 220

VENDOR_PATH_MARKERS = {
    "photon",
    "reference",
    "textmesh pro",
    "examples & extras",
    "packages",
    "plugins",
}


def build_llm_project_context(
    context: Any,
    purpose: str,
    *,
    selected: dict[str, Any] | None = None,
    task_card: dict[str, Any] | None = None,
    max_chars: int | None = None,
) -> str:
    """Build a broad but bounded context package for LLM calls."""

    context_config = context.get("configs", {}).get("agent", {}).get("context", {})
    max_context_chars = int(
        max_chars or context_config.get("project_context_max_chars", DEFAULT_MAX_CONTEXT_CHARS)
    )
    script_excerpt_chars = int(
        context_config.get("script_excerpt_chars", DEFAULT_SCRIPT_EXCERPT_CHARS)
    )
    max_script_excerpts = int(
        context_config.get("max_script_excerpts", DEFAULT_MAX_SCRIPT_EXCERPTS)
    )
    include_vendor_scripts = bool(context_config.get("include_vendor_scripts", False))

    task = context.get("task", {})
    user_request = context.get("user_request") or task.get("description", "")
    selected_candidate = selected or context.get("selected_candidate") or {}
    current_task_card = task_card or context.get("task_card") or {}
    project_scan = context.get("project_scan", {})
    script_analysis = context.get("script_analysis", {})

    terms = _context_terms(user_request, selected_candidate, current_task_card)
    bundle = {
        "purpose": purpose,
        "user_request": user_request,
        "current_task": task,
        "conversation_history": context.get("conversation_history", [])[-12:],
        "previous_model_calls_in_session": _compact_model_calls(
            context.get("previous_model_calls", []), limit=6
        ),
        "current_run_model_calls": _compact_model_calls(
            context.get("model_calls", []), limit=6
        ),
        "project_scan": _compact_project_scan(project_scan),
        "script_analysis": _compact_script_analysis(script_analysis),
        "feature_candidates": context.get("feature_candidates", [])[:24],
        "selected_candidate": selected_candidate,
        "task_card": current_task_card,
        "project_files": _project_file_context(
            context,
            project_scan,
            script_analysis,
            terms=terms,
            max_script_excerpts=max_script_excerpts,
            script_excerpt_chars=script_excerpt_chars,
            include_vendor_scripts=include_vendor_scripts,
        ),
    }

    return _json_with_fit(bundle, max_context_chars)


def _compact_project_scan(scan: dict[str, Any]) -> dict[str, Any]:
    if not scan:
        return {}
    return {
        "root_path": scan.get("root_path"),
        "assets_path": scan.get("assets_path"),
        "counts": scan.get("counts", {}),
        "detected_directories": scan.get("detected_directories", {}),
        "samples": scan.get("samples", {}),
        "summary": scan.get("summary", ""),
    }


def _compact_script_analysis(analysis: dict[str, Any]) -> dict[str, Any]:
    if not analysis:
        return {}
    scripts = analysis.get("scripts", [])
    return {
        "assets_path": analysis.get("assets_path"),
        "total_scripts": analysis.get("total_scripts", 0),
        "summary": analysis.get("summary", {}),
        "notable_scripts": [
            {
                "path": item.get("path"),
                "classes": item.get("classes", []),
                "namespaces": item.get("namespaces", []),
                "unity_types": item.get("unity_types", []),
                "lifecycle_methods": item.get("lifecycle_methods", []),
                "public_fields": item.get("public_fields", 0),
                "serialize_fields": item.get("serialize_fields", 0),
                "line_count": item.get("line_count", 0),
            }
            for item in scripts[:120]
        ],
    }


def _compact_model_calls(calls: list[dict[str, Any]], limit: int) -> list[dict[str, Any]]:
    compacted = []
    for item in calls[-limit:]:
        compacted.append(
            {
                "purpose": item.get("purpose"),
                "call_id": item.get("call_id"),
                "provider": item.get("provider"),
                "model": item.get("model"),
                "mock": item.get("mock"),
                "used": item.get("used"),
                "note": item.get("note", ""),
                "fallback_reason": item.get("fallback_reason", ""),
                "usage": item.get("usage", {}),
                "request_preview": _safe_text(item.get("request_preview", ""), 1800),
                "content_preview": _safe_text(item.get("content_preview", ""), 2200),
                "request_log_path": item.get("request_log_path", ""),
                "response_log_path": item.get("response_log_path", ""),
            }
        )
    return compacted


def _project_file_context(
    context: Any,
    project_scan: dict[str, Any],
    script_analysis: dict[str, Any],
    *,
    terms: list[str],
    max_script_excerpts: int,
    script_excerpt_chars: int,
    include_vendor_scripts: bool,
) -> dict[str, Any]:
    base_dir = Path(context.get_service("base_dir") or ".")
    assets_path = _assets_path(base_dir, context, project_scan, script_analysis)
    scripts = script_analysis.get("scripts", [])
    ranked_scripts = sorted(
        scripts,
        key=lambda item: _script_rank(item, terms, include_vendor_scripts),
        reverse=True,
    )

    file_index = [
        {
            "path": item.get("path"),
            "classes": item.get("classes", []),
            "unity_types": item.get("unity_types", []),
            "lifecycle_methods": item.get("lifecycle_methods", []),
            "line_count": item.get("line_count", 0),
            "vendor_or_reference": _is_vendor_path(str(item.get("path", ""))),
        }
        for item in ranked_scripts[:DEFAULT_FILE_INDEX_LIMIT]
    ]

    excerpts = []
    for item in ranked_scripts:
        rel_path = str(item.get("path", ""))
        if not rel_path:
            continue
        if not include_vendor_scripts and _is_vendor_path(rel_path):
            continue
        full_path = (assets_path / rel_path).resolve()
        if not full_path.exists() or not full_path.is_file():
            continue
        content = read_text(full_path)
        excerpt = _relevant_excerpt(content, terms, script_excerpt_chars)
        if not excerpt:
            continue
        excerpts.append(
            {
                "path": path_to_posix(rel_path),
                "classes": item.get("classes", []),
                "namespaces": item.get("namespaces", []),
                "unity_types": item.get("unity_types", []),
                "lifecycle_methods": item.get("lifecycle_methods", []),
                "excerpt": excerpt,
                "truncated": len(content) > len(excerpt),
            }
        )
        if len(excerpts) >= max_script_excerpts:
            break

    return {
        "assets_path": str(assets_path),
        "file_index": file_index,
        "script_excerpts": excerpts,
        "asset_samples": project_scan.get("samples", {}),
        "context_policy": {
            "include_vendor_scripts": include_vendor_scripts,
            "max_script_excerpts": max_script_excerpts,
            "script_excerpt_chars": script_excerpt_chars,
            "note": "第三方/Reference 代码默认只进入文件索引，不放入正文片段，避免淹没项目自身逻辑。",
        },
    }


def _assets_path(
    base_dir: Path,
    context: Any,
    project_scan: dict[str, Any],
    script_analysis: dict[str, Any],
) -> Path:
    if script_analysis.get("assets_path"):
        return Path(script_analysis["assets_path"]).resolve()
    if project_scan.get("assets_path"):
        return Path(project_scan["assets_path"]).resolve()
    unity_cfg = context.get("configs", {}).get("unity", {}).get("unity_project", {})
    resolved = resolve_path(base_dir, unity_cfg.get("assets_path"))
    return resolved or base_dir.parent


def _context_terms(
    user_request: str,
    selected: dict[str, Any],
    task_card: dict[str, Any],
) -> list[str]:
    raw = " ".join(
        str(value)
        for value in [
            user_request,
            selected.get("candidate_id", ""),
            selected.get("feature_name", ""),
            selected.get("description", ""),
            selected.get("suggested_class_name", ""),
            task_card.get("task_goal", ""),
            task_card.get("description", ""),
        ]
    )
    terms = []
    for item in re.split(r"[^A-Za-z0-9_\u4e00-\u9fff]+", raw.lower()):
        item = item.strip("_")
        if len(item) >= 3 and item not in terms:
            terms.append(item)
    return terms[:32]


def _script_rank(
    item: dict[str, Any],
    terms: list[str],
    include_vendor_scripts: bool,
) -> tuple[int, int, int, str]:
    path = str(item.get("path", "")).lower()
    searchable = " ".join(
        [
            path,
            " ".join(str(cls.get("name", "")) for cls in item.get("classes", [])),
            " ".join(str(value) for value in item.get("namespaces", [])),
            " ".join(str(value) for value in item.get("unity_types", [])),
        ]
    ).lower()
    term_score = sum(1 for term in terms if term and term in searchable)
    project_score = 0 if _is_vendor_path(path) and not include_vendor_scripts else 5
    unity_score = len(item.get("unity_types", [])) + len(item.get("lifecycle_methods", []))
    return (project_score, term_score, unity_score, path)


def _is_vendor_path(path: str) -> bool:
    lowered = path.replace("\\", "/").lower()
    return any(marker in lowered for marker in VENDOR_PATH_MARKERS)


def _relevant_excerpt(content: str, terms: list[str], max_chars: int) -> str:
    normalized = content.replace("\r\n", "\n").replace("\r", "\n").strip()
    if not normalized:
        return ""
    if len(normalized) <= max_chars:
        return normalized

    lowered = normalized.lower()
    for term in terms:
        if not term:
            continue
        index = lowered.find(term.lower())
        if index >= 0:
            start = max(0, index - max_chars // 3)
            end = min(len(normalized), start + max_chars)
            return _trim_to_line_boundaries(normalized[start:end], start > 0, end < len(normalized))

    return _trim_to_line_boundaries(normalized[:max_chars], False, True)


def _trim_to_line_boundaries(text: str, has_prefix: bool, has_suffix: bool) -> str:
    if has_prefix and "\n" in text:
        text = text[text.find("\n") + 1 :]
    if has_suffix and "\n" in text:
        text = text[: text.rfind("\n")]
    if has_prefix:
        text = "...<前文省略>\n" + text
    if has_suffix:
        text = text + "\n...<后文省略>"
    return text


def _json_with_fit(bundle: dict[str, Any], max_chars: int) -> str:
    text = json.dumps(bundle, ensure_ascii=False, indent=2)
    if len(text) <= max_chars:
        return text

    project_files = bundle.get("project_files", {})
    excerpts = list(project_files.get("script_excerpts", []))
    while excerpts and len(text) > max_chars:
        excerpts.pop()
        project_files["script_excerpts"] = excerpts
        bundle["project_files"] = project_files
        text = json.dumps(bundle, ensure_ascii=False, indent=2)

    if len(text) <= max_chars:
        return text

    project_files["file_index"] = project_files.get("file_index", [])[:80]
    bundle["feature_candidates"] = bundle.get("feature_candidates", [])[:12]
    bundle["script_analysis"]["notable_scripts"] = bundle.get("script_analysis", {}).get(
        "notable_scripts", []
    )[:60]
    text = json.dumps(bundle, ensure_ascii=False, indent=2)
    if len(text) <= max_chars:
        return text
    return text[:max_chars] + "\n...<context truncated>"


def _safe_text(value: Any, max_chars: int) -> str:
    text = str(value).replace("\r", "\\r").replace("\n", "\\n")
    if len(text) <= max_chars:
        return text
    return text[:max_chars] + "...<truncated>"
