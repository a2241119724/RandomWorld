from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime
from typing import Any

from .file_utils import safe_slug


@dataclass
class AgentTask:
    task_type: str
    description: str = ""
    constraints: list[str] = field(default_factory=list)
    parameters: dict[str, Any] = field(default_factory=dict)
    task_id: str = ""

    @classmethod
    def from_dict(cls, payload: dict[str, Any]) -> "AgentTask":
        task_type = payload.get("task") or payload.get("task_type") or "scan_project"
        task_id = payload.get("task_id") or cls.make_task_id(task_type)
        return cls(
            task_type=task_type,
            description=payload.get("description", ""),
            constraints=list(payload.get("constraints", [])),
            parameters=dict(payload.get("parameters", {})),
            task_id=task_id,
        )

    @staticmethod
    def make_task_id(task_type: str) -> str:
        stamp = datetime.now().strftime("%H%M%S")
        return f"{safe_slug(task_type, 32)}_{stamp}"

    def to_dict(self) -> dict[str, Any]:
        return {
            "task": self.task_type,
            "task_id": self.task_id,
            "description": self.description,
            "constraints": self.constraints,
            "parameters": self.parameters,
        }
