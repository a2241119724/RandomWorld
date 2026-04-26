from __future__ import annotations

from typing import Any

from core.sub_agent import SubAgent


class CodeGeneratorAgent(SubAgent):
    name = "code_generator"
    description = "生成 Unity C# 新功能脚本到配置的项目目录。"
    available_skills = ["generate_csharp_script"]

    def run(self, task: dict[str, Any], context: Any) -> dict[str, Any]:
        result = self.use_skill(
            "generate_csharp_script",
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
