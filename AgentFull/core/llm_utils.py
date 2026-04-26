from __future__ import annotations

import json
import re
from typing import Any


def compact_json(value: Any, max_chars: int = 12000) -> str:
    try:
        text = json.dumps(value, ensure_ascii=False, indent=2)
    except TypeError:
        text = str(value)
    if len(text) <= max_chars:
        return text
    return text[:max_chars] + "\n...<truncated>"


def extract_json_value(text: str) -> Any | None:
    candidates = _fenced_blocks(text, "json")
    candidates.append(text)

    for candidate in candidates:
        stripped = candidate.strip()
        if not stripped:
            continue
        try:
            return json.loads(stripped)
        except json.JSONDecodeError:
            pass

        balanced = _first_balanced_json(stripped)
        if balanced:
            try:
                return json.loads(balanced)
            except json.JSONDecodeError:
                continue
    return None


def extract_csharp_code(text: str) -> str | None:
    code_blocks = (
        _fenced_blocks(text, "csharp")
        + _fenced_blocks(text, "cs")
        + _fenced_blocks(text, "c#")
    )
    for block in code_blocks:
        code = block.strip()
        if _looks_like_csharp(code):
            return code

    stripped = text.strip()
    if _looks_like_csharp(stripped):
        return stripped
    return None


def record_model_call(
    context: Any,
    purpose: str,
    response: dict[str, Any],
    *,
    used: bool,
    note: str = "",
) -> None:
    calls = list(context.get("model_calls", []))
    entry = {
        "purpose": purpose,
        "call_id": response.get("call_id"),
        "model_profile": response.get("model_profile"),
        "provider": response.get("provider"),
        "model": response.get("model"),
        "mock": response.get("mock", False),
        "used": used,
        "fallback_reason": response.get("fallback_reason", ""),
        "note": note,
        "usage": response.get("usage", {}),
        "request_preview": compact_json(response.get("request_preview", []), 2200),
        "content_preview": safe_text(response.get("content", ""), 1800),
        "request_log_path": response.get("request_log_path", ""),
        "response_log_path": response.get("response_log_path", ""),
        "error_log_path": response.get("error_log_path", ""),
    }
    calls.append(entry)
    context.set("model_calls", calls)
    state = "used" if used else "fallback"
    context.append_event(
        f"LLM {purpose}: {state} provider={entry['provider']} model={entry['model']} mock={entry['mock']}"
    )


def safe_text(value: Any, max_chars: int) -> str:
    text = str(value).replace("\r", "\\r").replace("\n", "\\n")
    if len(text) <= max_chars:
        return text
    return text[:max_chars] + "...<truncated>"


def _fenced_blocks(text: str, language: str) -> list[str]:
    pattern = rf"```{re.escape(language)}\s*(.*?)```"
    return re.findall(pattern, text, flags=re.IGNORECASE | re.DOTALL)


def _looks_like_csharp(text: str) -> bool:
    lowered = text.lower()
    return (
        ("class " in lowered or "struct " in lowered)
        and (";" in text or "{" in text)
        and ("using " in lowered or "namespace " in lowered or "#if unity_editor" in lowered)
    )


def _first_balanced_json(text: str) -> str | None:
    start = -1
    opening = ""
    for index, char in enumerate(text):
        if char in "{[":
            start = index
            opening = char
            break
    if start < 0:
        return None

    closing_for = {"{": "}", "[": "]"}
    stack = [closing_for[opening]]
    in_string = False
    escaped = False

    for index in range(start + 1, len(text)):
        char = text[index]
        if escaped:
            escaped = False
            continue
        if char == "\\" and in_string:
            escaped = True
            continue
        if char == '"':
            in_string = not in_string
            continue
        if in_string:
            continue
        if char in "{[":
            stack.append(closing_for[char])
            continue
        if char in "}]":
            if not stack or char != stack[-1]:
                return None
            stack.pop()
            if not stack:
                return text[start : index + 1]
    return None
