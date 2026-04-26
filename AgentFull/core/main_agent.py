from __future__ import annotations

import time
from datetime import datetime
from pathlib import Path
from typing import Any

from agents.code_generator_agent import CodeGeneratorAgent
from agents.feature_planner_agent import FeaturePlannerAgent
from agents.project_analyzer_agent import ProjectAnalyzerAgent
from agents.report_agent import ReportAgent
from agents.unity_editor_agent import UnityEditorAgent
from agents.validation_agent import ValidationAgent
from skills.analyze_csharp_scripts_skill import AnalyzeCSharpScriptsSkill
from skills.asset_reference_check_skill import AssetReferenceCheckSkill
from skills.discover_feature_gap_skill import DiscoverFeatureGapSkill
from skills.generate_csharp_script_skill import GenerateCSharpScriptSkill
from skills.generate_feature_task_skill import GenerateFeatureTaskSkill
from skills.generate_unity_editor_tool_skill import GenerateUnityEditorToolSkill
from skills.scan_unity_project_skill import ScanUnityProjectSkill
from skills.update_feature_status_skill import UpdateFeatureStatusSkill
from skills.write_report_skill import WriteReportSkill

from .cache import FileCache
from .context_compressor import ContextCompressor
from .context_manager import ContextManager
from .file_utils import csharp_class_name, csharp_output_dir, load_yaml, resolve_path
from .logger import get_logger
from .memory import Memory
from .model_router import ModelRouter
from .report_writer import ReportWriter
from .skill_registry import SkillRegistry
from .task import AgentTask


class MainAgent:
    def __init__(
        self,
        base_dir: Path,
        model_name: str | None = None,
        mock: bool = False,
        project_root: str | None = None,
        output: str | None = None,
    ) -> None:
        self.base_dir = Path(base_dir).resolve()
        self.model_name = model_name
        self.mock = mock
        self.project_root_override = project_root
        self.output_override = output

        self.agent_config: dict[str, Any] = {}
        self.model_config: dict[str, Any] = {}
        self.unity_config: dict[str, Any] = {}
        self.agents: dict[str, Any] = {}

        self.context = ContextManager()

        self.load_config()
        logging_config = self.agent_config.get("logging", {})
        log_dir = self.base_dir / "cache" if logging_config.get("write_log_file", True) else None
        self.log_path = (log_dir / "agentfull.log") if log_dir else None
        self.logger = get_logger(
            "AgentFull",
            log_dir,
            level=logging_config.get("level", "INFO"),
        )
        self.skill_registry = SkillRegistry(self.logger)
        self.context_compressor = ContextCompressor(
            self.agent_config.get("context", {}).get("max_chars", 24000)
        )
        self.memory = Memory(self.base_dir / "memory")
        self.cache = FileCache(
            self.base_dir / "cache",
            enabled=self.agent_config.get("cache", {}).get("enabled", True),
        )
        reports_dir = self._resolve_reports_dir()
        self.report_writer = ReportWriter(reports_dir)
        self.model_router = ModelRouter(
            self.base_dir / "config" / "model_config.yaml",
            model_name=model_name,
            mock=mock,
            logger=self.logger,
        )

        self._attach_services()
        self.register_skills()
        self.register_agents()

    def load_config(self) -> None:
        self.agent_config = load_yaml(self.base_dir / "config" / "agent_config.yaml")
        self.model_config = load_yaml(self.base_dir / "config" / "model_config.yaml")
        self.unity_config = load_yaml(self.base_dir / "config" / "unity_project_config.yaml")

        if self.project_root_override:
            root = Path(self.project_root_override).expanduser().resolve()
            unity_project = self.unity_config.setdefault("unity_project", {})
            unity_project["root_path"] = str(root)
            unity_project["assets_path"] = str(root / "Assets")
            unity_project["scripts_path"] = str(root / "Assets" / "Scripts")
            unity_project["editor_path"] = str(root / "Assets" / "Editor")
            unity_project["scenes_path"] = str(root / "Assets" / "Scenes")
            unity_project["prefabs_path"] = str(root / "Assets" / "Prefabs")
            unity_project["resources_path"] = str(root / "Assets" / "Resources")
            unity_project["streaming_assets_path"] = str(root / "Assets" / "StreamingAssets")
            unity_project["addressables_path"] = str(root / "Assets" / "AddressableAssetsData")

        if self.output_override:
            self.unity_config.setdefault("unity_project", {})["reports_path"] = self.output_override

    def register_agents(self) -> None:
        self.agents = {
            "project_analyzer": ProjectAnalyzerAgent(self.skill_registry, self.logger),
            "feature_planner": FeaturePlannerAgent(self.skill_registry, self.logger),
            "code_generator": CodeGeneratorAgent(self.skill_registry, self.logger),
            "unity_editor": UnityEditorAgent(self.skill_registry, self.logger),
            "validation": ValidationAgent(self.skill_registry, self.logger),
            "report": ReportAgent(self.skill_registry, self.logger),
        }

    def register_skills(self) -> None:
        for skill in [
            ScanUnityProjectSkill(),
            AnalyzeCSharpScriptsSkill(),
            DiscoverFeatureGapSkill(),
            GenerateFeatureTaskSkill(),
            GenerateCSharpScriptSkill(),
            GenerateUnityEditorToolSkill(),
            AssetReferenceCheckSkill(),
            WriteReportSkill(),
            UpdateFeatureStatusSkill(),
        ]:
            self.skill_registry.register(skill)

    def run_task(self, task: dict[str, Any]) -> dict[str, Any]:
        task_obj = AgentTask.from_dict(task)
        started = time.perf_counter()
        self.context.reset(
            {
                "task": task_obj.to_dict(),
                "started_at": datetime.now().isoformat(timespec="seconds"),
            }
        )
        self._attach_services()
        self.context.set("configs", self._context_configs())

        initial_report_dir = self.report_writer.create_run_dir(task_obj.task_id)
        self.context.set("report_dir", str(initial_report_dir))
        self.context.append_event(f"Task started: {task_obj.task_type}")
        model_profile = self.model_name or self.model_config.get("models", {}).get("default")
        selected_model_config = self.model_config.get("models", {}).get(model_profile, {})
        self.logger.info(
            "Task started | task_id=%s task_type=%s model_profile=%s provider=%s model=%s mock=%s report_dir=%s",
            task_obj.task_id,
            task_obj.task_type,
            model_profile,
            selected_model_config.get("provider", ""),
            selected_model_config.get("model", ""),
            self.mock,
            initial_report_dir,
        )

        try:
            if task_obj.task_type == "scan_project":
                self.dispatch_to_agent("project_analyzer", {"mode": "scan_project"})
                self.dispatch_to_agent("validation", {"mode": "asset_reference_check"})
            elif task_obj.task_type == "analyze_scripts":
                self.dispatch_to_agent("project_analyzer", {"mode": "full_analysis"})
            elif task_obj.task_type == "generate_feature":
                self._run_auto_pipeline(mark_completed=False)
            elif task_obj.task_type == "auto_discover_and_implement":
                self._run_auto_pipeline(mark_completed=True)
            else:
                raise ValueError(f"Unsupported task type: {task_obj.task_type}")
        except Exception as exc:
            self.context.append_error("Task execution failed.", str(exc))
            self.logger.exception("Task execution failed")

        self.context.set("finished_at", datetime.now().isoformat(timespec="seconds"))
        self.memory.save_short_term(self.context.to_serializable())
        report_info = self.save_report()
        self.context.set("final_report", report_info)
        self.memory.save_short_term(self.context.to_serializable())
        elapsed_ms = int((time.perf_counter() - started) * 1000)
        self.logger.info(
            "Task finished | task_id=%s duration_ms=%s report_path=%s error_count=%s",
            task_obj.task_id,
            elapsed_ms,
            report_info.get("report_path"),
            len(self.context.get("errors", [])),
        )

        return {
            "task_id": task_obj.task_id,
            "report_path": report_info.get("report_path"),
            "log_path": str(self.log_path) if self.log_path else None,
            "summary": self.summarize_result(),
        }

    def dispatch_to_agent(self, agent_name: str, task: dict[str, Any]) -> dict[str, Any]:
        if agent_name not in self.agents:
            raise KeyError(f"Agent '{agent_name}' is not registered.")
        self.context.append_event(f"Dispatching to agent: {agent_name}")
        started = time.perf_counter()
        self.logger.info(
            "Agent dispatch started | agent=%s mode=%s",
            agent_name,
            task.get("mode", ""),
        )
        try:
            result = self.agents[agent_name].run(task, self.context)
        except Exception:
            self.logger.exception("Agent dispatch failed | agent=%s", agent_name)
            raise
        elapsed_ms = int((time.perf_counter() - started) * 1000)
        self.logger.info(
            "Agent dispatch finished | agent=%s duration_ms=%s result_keys=%s",
            agent_name,
            elapsed_ms,
            sorted(result.keys()),
        )
        self.context.append_agent_result(agent_name, result)
        return result

    def summarize_result(self) -> dict[str, Any]:
        scan = self.context.get("project_scan", {})
        scripts = self.context.get("script_analysis", {})
        selected = self.context.get("selected_candidate") or {}
        return {
            "report_dir": self.context.get("report_dir"),
            "project_counts": scan.get("counts", {}),
            "total_scripts": scripts.get("total_scripts", 0),
            "candidate_count": len(self.context.get("feature_candidates", [])),
            "selected_candidate": selected.get("candidate_id"),
            "generated_files": self.context.get("generated_files", []),
            "model_call_count": len(self.context.get("model_calls", [])),
            "model_calls": [
                {
                    "purpose": item.get("purpose"),
                    "provider": item.get("provider"),
                    "model": item.get("model"),
                    "mock": item.get("mock"),
                    "used": item.get("used"),
                    "fallback_reason": item.get("fallback_reason", ""),
                }
                for item in self.context.get("model_calls", [])
            ],
            "errors": self.context.get("errors", []),
        }

    def save_report(self) -> dict[str, Any]:
        return self.dispatch_to_agent("report", {"mode": "write_report"})

    def _run_auto_pipeline(self, mark_completed: bool) -> None:
        self.dispatch_to_agent("project_analyzer", {"mode": "full_analysis"})
        planner_result = self.dispatch_to_agent("feature_planner", {"mode": "discover_and_plan"})
        selected = planner_result.get("selected_candidate")

        if selected:
            final_report_dir = self.report_writer.create_candidate_dir(selected)
            self.context.set("report_dir", str(final_report_dir))
            self._refresh_task_card_paths(selected, final_report_dir)
            self.context.append_event(f"Candidate selected: {selected.get('candidate_id')}")

            implementation_type = selected.get("implementation_type")
            if implementation_type in {"readonly_tool", "editor_tool", "report_tool"}:
                self.dispatch_to_agent("unity_editor", {"mode": "generate_editor_tool"})
            else:
                self.dispatch_to_agent("code_generator", {"mode": "generate_csharp_script"})

            self.dispatch_to_agent("validation", {"mode": "full_validation"})
            if mark_completed:
                self.skill_registry.run(
                    "update_feature_status",
                    {
                        "candidate_id": selected.get("candidate_id"),
                        "status": "completed",
                        "note": "Generated a new feature script to the configured Unity path without overwriting existing files.",
                    },
                    self.context,
                )
        else:
            self.context.append_event("No implementable low/medium risk candidate found.")
            self.dispatch_to_agent("validation", {"mode": "asset_reference_check"})

    def _attach_services(self) -> None:
        self.context.set_service("memory", getattr(self, "memory", None))
        self.context.set_service("cache", getattr(self, "cache", None))
        self.context.set_service("report_writer", getattr(self, "report_writer", None))
        self.context.set_service("model_router", getattr(self, "model_router", None))
        self.context.set_service("context_compressor", getattr(self, "context_compressor", None))
        self.context.set_service("base_dir", self.base_dir)

    def _refresh_task_card_paths(self, selected: dict[str, Any], report_dir: Path) -> None:
        task_card = self.context.get("task_card") or {}
        if not task_card:
            return
        class_name = "AgentProjectOverviewWindow"
        if selected.get("candidate_id") != "cand_001_project_overview_editor":
            class_name = csharp_class_name(
                selected.get("suggested_class_name") or selected.get("feature_name"),
                "GeneratedUnityFeature",
            )
        generated_dir = csharp_output_dir(
            self.base_dir,
            self.unity_config,
            report_dir,
            implementation_type=selected.get("implementation_type"),
        )
        task_card["new_files"] = [str(generated_dir / f"{class_name}.cs")]
        self.context.set("task_card", task_card)

    def _context_configs(self) -> dict[str, Any]:
        return {
            "agent": self.agent_config,
            "unity": self.unity_config,
            "model_profile": self.model_name or self.model_config.get("models", {}).get("default"),
            "mock": self.mock,
            "log_path": str(self.log_path) if self.log_path else None,
        }

    def _resolve_reports_dir(self) -> Path:
        reports_value = self.unity_config.get("unity_project", {}).get("reports_path", "./reports")
        if self.output_override:
            reports_value = self.output_override
        resolved = resolve_path(self.base_dir, reports_value)
        return resolved or (self.base_dir / "reports")
