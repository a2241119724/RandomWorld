from __future__ import annotations

from abc import ABC, abstractmethod
from typing import Any


class Skill(ABC):
    name: str = "base_skill"
    description: str = ""
    input_schema: dict[str, Any] = {}
    output_schema: dict[str, Any] = {}

    @abstractmethod
    def run(self, params: dict[str, Any], context: Any) -> dict[str, Any]:
        """Execute a skill and return a serializable result."""

    def describe(self) -> dict[str, Any]:
        return {
            "name": self.name,
            "description": self.description,
            "input_schema": self.input_schema,
            "output_schema": self.output_schema,
        }
