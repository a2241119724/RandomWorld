from __future__ import annotations

from typing import Any

from core.sub_agent import SubAgent


class ProjectAnalyzerAgent(SubAgent):
    name = "project_analyzer"
    description = "Analyze Unity project structure and C# scripts."
    available_skills = ["scan_unity_project", "analyze_csharp_scripts"]

    def run(self, task: dict[str, Any], context: Any) -> dict[str, Any]:
        mode = task.get("mode", "full_analysis")
        result: dict[str, Any] = {"mode": mode}

        if mode in {"scan_project", "full_analysis"}:
            scan = self.use_skill("scan_unity_project", {}, context)
            context.set("project_scan", scan)
            result["project_scan"] = scan

        if mode in {"analyze_scripts", "full_analysis"}:
            analysis = self.use_skill("analyze_csharp_scripts", {}, context)
            context.set("script_analysis", analysis)
            result["script_analysis"] = analysis

        return result
