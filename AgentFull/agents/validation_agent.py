from __future__ import annotations

from pathlib import Path
from typing import Any

from core.file_utils import read_text
from core.sub_agent import SubAgent


class ValidationAgent(SubAgent):
    name = "validation"
    description = "Run readonly static validation and risk checks."
    available_skills = ["asset_reference_check"]

    def run(self, task: dict[str, Any], context: Any) -> dict[str, Any]:
        mode = task.get("mode", "full_validation")
        result: dict[str, Any] = {"mode": mode}

        if mode in {"asset_reference_check", "full_validation"}:
            asset_report = self.use_skill("asset_reference_check", {}, context)
            result["asset_reference_check"] = asset_report
            context.set("asset_reference_check", asset_report)

        if mode == "full_validation":
            generated_checks = self._validate_generated_files(context.get("generated_files", []))
            result["generated_file_validation"] = generated_checks
            context.set("validation", generated_checks)

        return result

    def _validate_generated_files(self, generated_files: list[str]) -> dict[str, Any]:
        checks: list[dict[str, Any]] = []
        for file_name in generated_files:
            path = Path(file_name)
            content = read_text(path)
            file_checks = {
                "path": str(path),
                "exists": path.exists(),
                "has_using_unity_editor": "using UnityEditor;" in content,
                "has_namespace": "namespace " in content,
                "has_class": " class " in content,
                "risky_asset_modification_calls": [],
            }
            risky_calls = [
                "AssetDatabase.DeleteAsset",
                "AssetDatabase.MoveAsset",
                "PrefabUtility.SaveAsPrefabAsset",
                "EditorSceneManager.SaveScene",
                "File.Delete(",
            ]
            for call in risky_calls:
                if call in content:
                    file_checks["risky_asset_modification_calls"].append(call)
            file_checks["passed"] = bool(
                file_checks["exists"]
                and file_checks["has_namespace"]
                and file_checks["has_class"]
                and not file_checks["risky_asset_modification_calls"]
            )
            checks.append(file_checks)
        return {
            "passed": all(item.get("passed") for item in checks) if checks else True,
            "checks": checks,
            "policy": "Static validation only; Unity compilation is not executed by default.",
        }
