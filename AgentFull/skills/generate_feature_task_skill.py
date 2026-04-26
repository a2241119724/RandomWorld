from __future__ import annotations

from pathlib import Path
from typing import Any

from core.file_utils import csharp_class_name, csharp_output_dir, read_text
from core.llm_utils import compact_json, extract_json_value, record_model_call
from core.project_context import build_llm_project_context
from core.skill import Skill


class GenerateFeatureTaskSkill(Skill):
    name = "generate_feature_task"
    description = "Select a safe candidate and convert it into an implementation task card."
    input_schema = {"candidates": "candidate list", "report_dir": "current report directory"}
    output_schema = {"selected_candidate": "chosen candidate", "task_card": "implementation card"}

    def run(self, params: dict[str, Any], context: Any) -> dict[str, Any]:
        candidates = list(params.get("candidates") or [])
        configs = context.get("configs", {}).get("unity", {})
        policies = configs
        risk_policy = policies.get("risk_policy", {})
        skip_high = risk_policy.get("skip_high_risk_candidate", True)

        selected = self._select_candidate(candidates, skip_high=skip_high)
        if not selected:
            return {
                "selected_candidate": None,
                "task_card": {
                    "task_goal": "只生成只读分析报告。",
                    "reason": "没有可用的待处理低/中风险功能候选项。",
                    "verification_steps": ["查看 report.md 中的扫描和验证结果。"],
                },
            }

        llm_plan = self._llm_plan(candidates, selected, skip_high, context)
        llm_selected = self._candidate_from_plan(candidates, llm_plan, skip_high)
        if llm_selected:
            selected = llm_selected

        report_dir = Path(params.get("report_dir") or context.get("report_dir") or ".")
        base_dir = Path(context.get_service("base_dir") or ".")
        class_name = self._class_name_for_candidate(selected)
        generated_dir = csharp_output_dir(
            base_dir,
            configs,
            report_dir,
            implementation_type=selected.get("implementation_type"),
        )
        generated_file = generated_dir / f"{class_name}.cs"
        task_card = {
            "task_goal": selected.get("feature_name"),
            "candidate_id": selected.get("candidate_id"),
            "description": selected.get("description"),
            "implementation_type": selected.get("implementation_type"),
            "suggested_class_name": selected.get("suggested_class_name") or class_name,
            "implementation_scope": [
                "在配置的 Unity Scripts 目录中生成一个新的 C# 运行时功能文件。",
                "不要修改已有场景、Prefab、ScriptableObject、StreamingAssets 或 Addressables。",
                "保持功能自包含，方便审查后手动挂载或接入。",
            ],
            "modify_files": [],
            "new_files": [str(generated_file)],
            "risk_notes": [
                f"风险等级：{selected.get('risk_level')}",
                "生成代码只写入新文件，不覆盖已有 Unity 文件。",
                "运行时接入需要手动选择并经过 Unity 审查。",
            ],
            "verification_steps": [
                "审查配置路径中的生成 C# 代码。",
                "打开 Unity 并确认新脚本可以编译。",
                "审查通过后，在测试场景或 Prefab 中手动挂载或引用该组件。",
            ],
            "rollback_plan": [
                "删除配置路径中的生成 C# 文件。",
            ],
            "selected_at": context.get("started_at"),
        }
        task_card = self._merge_llm_task_card(task_card, llm_plan.get("task_card", {}))
        return {"selected_candidate": selected, "task_card": task_card}

    def _llm_plan(
        self,
        candidates: list[dict[str, Any]],
        default_selected: dict[str, Any],
        skip_high: bool,
        context: Any,
    ) -> dict[str, Any]:
        router = context.get_service("model_router")
        if not router:
            return {}

        base_dir = Path(context.get_service("base_dir") or ".")
        system_prompt = read_text(base_dir / "prompts" / "feature_planner_prompt.md")
        project_context = build_llm_project_context(
            context,
            "generate_feature_task",
            selected=default_selected,
        )
        eligible = [
            candidate
            for candidate in candidates
            if candidate.get("status") == "pending"
            and not (skip_high and candidate.get("risk_level") == "high")
        ]
        messages = [
            {
                "role": "system",
                "content": (
                    system_prompt
                    + "\n你需要准确选择一个实现候选项，并细化任务卡。只返回 JSON。"
                ),
            },
            {
                "role": "user",
                "content": (
                    "请从可选列表中选择最安全且有用的新功能候选项。优先选择 runtime_feature，"
                    "并且这个功能应当能通过一个新的 C# 文件实现，不需要编辑已有项目文件。"
                    "当 skip_high_risk_candidate 为 true 时不要选择高风险候选项。不要加入"
                    "场景、Prefab、ScriptableObject、StreamingAssets、Addressables、存档、"
                    "网络、构建或破坏性文件改动。\n\n"
                    "返回这个 JSON 结构：\n"
                    "{\n"
                    "  \"selected_candidate_id\": \"candidate id from the list\",\n"
                    "  \"selection_reason\": \"简短选择理由\",\n"
                    "  \"task_card\": {\n"
                    "    \"implementation_scope\": [],\n"
                    "    \"risk_notes\": [],\n"
                    "    \"verification_steps\": [],\n"
                    "    \"rollback_plan\": []\n"
                    "  }\n"
                    "}\n\n"
                    "完整上下文包（包含项目结构、关键 C# 片段、会话上下文、用户输入和最近模型调用）：\n"
                    f"{project_context}\n\n"
                    f"skip_high_risk_candidate: {skip_high}\n"
                    f"default_selected_candidate_id: {default_selected.get('candidate_id')}\n"
                    f"eligible_candidates:\n{compact_json(eligible, 10000)}"
                ),
            },
        ]
        response = router.chat_for_task("generate_feature_task", messages)
        parsed = extract_json_value(response.get("content", ""))
        plan = parsed if isinstance(parsed, dict) else {}
        used = bool(self._candidate_from_plan(candidates, plan, skip_high))
        record_model_call(
            context,
            "generate_feature_task",
            response,
            used=used,
            note=f"selected_candidate_id={plan.get('selected_candidate_id', '')}",
        )
        return plan if used else {}

    def _candidate_from_plan(
        self,
        candidates: list[dict[str, Any]],
        plan: dict[str, Any],
        skip_high: bool,
    ) -> dict[str, Any] | None:
        selected_id = plan.get("selected_candidate_id")
        if not selected_id:
            return None
        for candidate in candidates:
            if candidate.get("candidate_id") != selected_id:
                continue
            if candidate.get("status") != "pending":
                return None
            if skip_high and candidate.get("risk_level") == "high":
                return None
            return candidate
        return None

    def _merge_llm_task_card(
        self,
        task_card: dict[str, Any],
        llm_task_card: Any,
    ) -> dict[str, Any]:
        if not isinstance(llm_task_card, dict):
            return task_card

        merged = dict(task_card)
        list_fields = {
            "implementation_scope",
            "risk_notes",
            "verification_steps",
            "rollback_plan",
        }
        for field in list_fields:
            value = llm_task_card.get(field)
            if isinstance(value, list) and value:
                merged[field] = [str(item) for item in value[:8]]

        for field in {"task_goal", "description"}:
            value = llm_task_card.get(field)
            if isinstance(value, str) and value.strip():
                merged[field] = value.strip()

        merged["candidate_id"] = task_card.get("candidate_id")
        merged["implementation_type"] = task_card.get("implementation_type")
        merged["modify_files"] = []
        merged["new_files"] = task_card.get("new_files", [])
        merged["selected_at"] = task_card.get("selected_at")
        return merged

    def _select_candidate(self, candidates: list[dict[str, Any]], skip_high: bool) -> dict[str, Any] | None:
        risk_order = {"low": 0, "medium": 1, "high": 2}
        type_order = {"runtime_feature": 0, "editor_tool": 1, "report_tool": 2, "readonly_tool": 3}
        eligible = []
        for candidate in candidates:
            if candidate.get("status") != "pending":
                continue
            if skip_high and candidate.get("risk_level") == "high":
                continue
            eligible.append(candidate)
        eligible.sort(
            key=lambda item: (
                risk_order.get(item.get("risk_level"), 9),
                type_order.get(item.get("implementation_type"), 9),
                item.get("candidate_id", ""),
            )
        )
        return eligible[0] if eligible else None

    def _class_name_for_candidate(self, candidate: dict[str, Any]) -> str:
        candidate_id = candidate.get("candidate_id", "candidate")
        if candidate_id == "cand_001_project_overview_editor":
            return "AgentProjectOverviewWindow"
        return csharp_class_name(
            candidate.get("suggested_class_name") or candidate.get("feature_name"),
            "GeneratedUnityFeature",
        )
