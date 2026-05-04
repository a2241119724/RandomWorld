from __future__ import annotations

from abc import ABC, abstractmethod
from typing import Any


class SubAgent(ABC):
    name: str = "sub_agent"
    description: str = ""
    available_skills: list[str] = []

    def __init__(self, skill_registry: Any, logger: Any | None = None) -> None:
        self.skill_registry = skill_registry
        self.logger = logger

    @abstractmethod
    def run(self, task: dict[str, Any], context: Any) -> dict[str, Any]:
        """Run the sub-agent against the task and shared context."""

    def use_skill(self, skill_name: str, params: dict[str, Any], context: Any) -> dict[str, Any]:
        if skill_name not in self.available_skills:
            raise ValueError(f"{self.name} cannot use skill '{skill_name}'.")
        return self.skill_registry.run(skill_name, params, context)
