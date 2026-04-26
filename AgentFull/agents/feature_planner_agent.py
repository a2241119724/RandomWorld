from __future__ import annotations

from typing import Any

from core.sub_agent import SubAgent


class FeaturePlannerAgent(SubAgent):
    name = "feature_planner"
    description = "发现功能候选项并生成实现任务卡。"
    available_skills = ["discover_feature_gap", "generate_feature_task"]

    def run(self, task: dict[str, Any], context: Any) -> dict[str, Any]:
        mode = task.get("mode", "discover_and_plan")
        result: dict[str, Any] = {"mode": mode}

        if mode in {"discover", "discover_and_plan"}:
            candidates_result = self.use_skill(
                "discover_feature_gap",
                {
                    "project_scan": context.get("project_scan", {}),
                    "script_analysis": context.get("script_analysis", {}),
                },
                context,
            )
            context.set("feature_candidates", candidates_result.get("candidates", []))
            result.update(candidates_result)

        if mode in {"task_card", "discover_and_plan"}:
            task_card_result = self.use_skill(
                "generate_feature_task",
                {
                    "candidates": context.get("feature_candidates", []),
                    "report_dir": context.get("report_dir"),
                },
                context,
            )
            context.set("selected_candidate", task_card_result.get("selected_candidate"))
            context.set("task_card", task_card_result.get("task_card"))
            result.update(task_card_result)

        return result
