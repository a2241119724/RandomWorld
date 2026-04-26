from __future__ import annotations

from typing import Any

from core.sub_agent import SubAgent


class ReportAgent(SubAgent):
    name = "report"
    description = "写出 Markdown 报告和 JSON 上下文。"
    available_skills = ["write_report"]

    def run(self, task: dict[str, Any], context: Any) -> dict[str, Any]:
        return self.use_skill("write_report", {"report_dir": context.get("report_dir")}, context)
