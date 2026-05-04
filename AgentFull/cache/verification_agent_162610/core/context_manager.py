from __future__ import annotations

import json
from copy import deepcopy
from datetime import datetime
from pathlib import Path
from typing import Any


class ContextManager:
    def __init__(self) -> None:
        self.data: dict[str, Any] = {}
        self.services: dict[str, Any] = {}

    def reset(self, initial_data: dict[str, Any] | None = None) -> None:
        self.data = initial_data.copy() if initial_data else {}
        self.data.setdefault("agent_results", {})
        self.data.setdefault("skill_results", {})
        self.data.setdefault("errors", [])
        self.data.setdefault("events", [])

    def set_service(self, name: str, service: Any) -> None:
        self.services[name] = service

    def get_service(self, name: str, default: Any = None) -> Any:
        return self.services.get(name, default)

    def set(self, key: str, value: Any) -> None:
        self.data[key] = value

    def get(self, key: str, default: Any = None) -> Any:
        return self.data.get(key, default)

    def update(self, values: dict[str, Any]) -> None:
        self.data.update(values)

    def append_event(self, message: str, level: str = "info") -> None:
        self.data.setdefault("events", []).append(
            {
                "time": datetime.now().isoformat(timespec="seconds"),
                "level": level,
                "message": message,
            }
        )

    def append_error(self, message: str, detail: Any | None = None) -> None:
        self.data.setdefault("errors", []).append(
            {
                "time": datetime.now().isoformat(timespec="seconds"),
                "message": message,
                "detail": self._safe(detail),
            }
        )

    def append_agent_result(self, agent_name: str, result: dict[str, Any]) -> None:
        self.data.setdefault("agent_results", {})[agent_name] = self._safe(result)

    def append_skill_result(self, skill_name: str, result: dict[str, Any]) -> None:
        self.data.setdefault("skill_results", {})[skill_name] = self._safe(result)

    def to_serializable(self) -> dict[str, Any]:
        return self._safe(deepcopy(self.data))

    def as_json(self, max_chars: int | None = None) -> str:
        content = json.dumps(self.to_serializable(), ensure_ascii=False, indent=2)
        if max_chars and len(content) > max_chars:
            return content[:max_chars] + "\n...<truncated>"
        return content

    def _safe(self, value: Any) -> Any:
        if isinstance(value, Path):
            return str(value)
        if isinstance(value, dict):
            return {str(key): self._safe(item) for key, item in value.items()}
        if isinstance(value, list):
            return [self._safe(item) for item in value]
        if isinstance(value, tuple):
            return [self._safe(item) for item in value]
        if isinstance(value, (str, int, float, bool)) or value is None:
            return value
        return str(value)
