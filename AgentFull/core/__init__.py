from __future__ import annotations

from typing import Any

__all__ = ["MainAgent", "SubAgent", "Skill", "SkillRegistry", "AgentTask"]


def __getattr__(name: str) -> Any:
    if name == "MainAgent":
        from .main_agent import MainAgent

        return MainAgent
    if name == "SubAgent":
        from .sub_agent import SubAgent

        return SubAgent
    if name == "Skill":
        from .skill import Skill

        return Skill
    if name == "SkillRegistry":
        from .skill_registry import SkillRegistry

        return SkillRegistry
    if name == "AgentTask":
        from .task import AgentTask

        return AgentTask
    raise AttributeError(f"module 'core' has no attribute {name!r}")
