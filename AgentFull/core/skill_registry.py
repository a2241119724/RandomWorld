from __future__ import annotations

from typing import Any

from .skill import Skill


class SkillRegistry:
    def __init__(self, logger: Any | None = None) -> None:
        self._skills: dict[str, Skill] = {}
        self.logger = logger

    def register(self, skill: Skill) -> None:
        if not skill.name:
            raise ValueError("Skill name cannot be empty.")
        self._skills[skill.name] = skill
        if self.logger:
            self.logger.info("Registered skill: %s", skill.name)

    def get(self, name: str) -> Skill:
        try:
            return self._skills[name]
        except KeyError as exc:
            available = ", ".join(sorted(self._skills))
            raise KeyError(f"Skill '{name}' is not registered. Available: {available}") from exc

    def run(self, name: str, params: dict[str, Any], context: Any) -> dict[str, Any]:
        skill = self.get(name)
        result = skill.run(params, context)
        context.append_skill_result(name, result)
        return result

    def list_skills(self) -> list[dict[str, Any]]:
        return [skill.describe() for skill in self._skills.values()]
