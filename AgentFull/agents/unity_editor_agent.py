from __future__ import annotations

from typing import Any

from core.sub_agent import SubAgent


class UnityEditorAgent(SubAgent):
    name = "unity_editor"
    description = "Generate readonly Unity Editor tools."
    available_skills = ["generate_unity_editor_tool"]

    def run(self, task: dict[str, Any], context: Any) -> dict[str, Any]:
        result = self.use_skill(
            "generate_unity_editor_tool",
            {
                "task_card": context.get("task_card", {}),
                "selected_candidate": context.get("selected_candidate", {}),
                "report_dir": context.get("report_dir"),
            },
            context,
        )
        context.set("generated_files", result.get("generated_files", []))
        context.set("generated_meta_files", result.get("generated_meta_files", []))
        return result
