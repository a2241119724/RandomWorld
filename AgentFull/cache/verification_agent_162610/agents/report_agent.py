from __future__ import annotations

from typing import Any

from core.sub_agent import SubAgent


class ReportAgent(SubAgent):
    name = "report"
    description = "Write Markdown and JSON reports."
    available_skills = ["write_report"]

    def run(self, task: dict[str, Any], context: Any) -> dict[str, Any]:
        return self.use_skill("write_report", {"report_dir": context.get("report_dir")}, context)
