from __future__ import annotations

from pathlib import Path
from typing import Any

from core.file_utils import (
    csharp_output_dir,
    mono_script_meta_content,
    read_text,
    safe_slug,
    unique_unity_asset_path,
    unity_generation_policy,
    unity_meta_path,
    write_text,
)
from core.llm_utils import compact_json, extract_csharp_code, record_model_call
from core.skill import Skill


class GenerateCSharpScriptSkill(Skill):
    name = "generate_csharp_script"
    description = "Generate a safe Unity C# script into the configured Unity script folder."
    input_schema = {"task_card": "task card", "script_kind": "MonoBehaviour|ScriptableObject|PlainClass"}
    output_schema = {"generated_files": "written C# files"}

    def run(self, params: dict[str, Any], context: Any) -> dict[str, Any]:
        task_card = params.get("task_card") or {}
        selected = params.get("selected_candidate") or {}
        report_dir = Path(params.get("report_dir") or context.get("report_dir") or ".")
        base_dir = Path(context.get_service("base_dir") or ".")
        configs = context.get("configs", {}).get("unity", {})

        script_kind = params.get("script_kind") or self._infer_kind(selected)
        implementation_type = selected.get("implementation_type")
        namespace = params.get("namespace") or "RandomWorld.AgentGenerated"
        class_name = params.get("class_name") or self._class_name(selected, script_kind)
        generated_dir = csharp_output_dir(
            base_dir,
            configs,
            report_dir,
            script_kind=script_kind,
            implementation_type=implementation_type,
        )
        target = unique_unity_asset_path(generated_dir / f"{class_name}.cs")
        class_name = target.stem
        content = self._llm_build_script(
            class_name,
            namespace,
            script_kind,
            task_card,
            selected,
            context,
        ) or self._build_script(class_name, namespace, script_kind, task_card)

        target = write_text(target, content, overwrite=False)
        meta_path = None
        if unity_generation_policy(configs).get("default_output_mode", "project") != "report_only":
            meta_path = write_text(unity_meta_path(target), mono_script_meta_content(), overwrite=False)
        result = {
            "generated_files": [str(target)],
            "generated_meta_files": [str(meta_path)] if meta_path else [],
            "script_kind": script_kind,
            "namespace": namespace,
            "class_name": class_name,
            "policy": "configured_unity_path_no_overwrite",
        }
        return result

    def _llm_build_script(
        self,
        class_name: str,
        namespace: str,
        script_kind: str,
        task_card: dict[str, Any],
        selected: dict[str, Any],
        context: Any,
    ) -> str | None:
        router = context.get_service("model_router")
        if not router:
            return None

        base_dir = Path(context.get_service("base_dir") or ".")
        system_prompt = read_text(base_dir / "prompts" / "code_generator_prompt.md")
        messages = [
            {
                "role": "system",
                "content": (
                    system_prompt
                    + "\nReturn only one complete C# file in a fenced csharp code block."
                ),
            },
            {
                "role": "user",
                "content": (
                    "Generate a small, reviewable Unity C# file for this task. "
                    "Use the exact namespace and class name. Do not modify scenes, prefabs, "
                    "ScriptableObjects, StreamingAssets, Addressables, save data, networking, "
                    "or build settings. Do not include destructive file operations.\n\n"
                    f"class_name: {class_name}\n"
                    f"namespace: {namespace}\n"
                    f"script_kind: {script_kind}\n"
                    f"selected_candidate:\n{compact_json(selected, 5000)}\n\n"
                    f"task_card:\n{compact_json(task_card, 7000)}"
                ),
            },
        ]
        response = router.chat_for_task("generate_csharp_script", messages)
        code = extract_csharp_code(response.get("content", ""))
        used = bool(code and self._is_safe_generated_code(code, class_name, script_kind))
        record_model_call(
            context,
            "generate_csharp_script",
            response,
            used=used,
            note=f"class_name={class_name}",
        )
        return code if used else None

    def _is_safe_generated_code(self, code: str, class_name: str, script_kind: str) -> bool:
        if class_name not in code or "```" in code:
            return False
        forbidden = [
            "AssetDatabase.DeleteAsset",
            "AssetDatabase.MoveAsset",
            "AssetDatabase.CreateAsset",
            "AssetDatabase.SaveAssets",
            "PrefabUtility.",
            "EditorSceneManager.",
            "File.Delete",
            "Directory.Delete",
            "Process.Start",
        ]
        if any(pattern in code for pattern in forbidden):
            return False
        if script_kind == "MonoBehaviour":
            return "MonoBehaviour" in code and "using UnityEngine" in code
        if script_kind == "ScriptableObject":
            return "ScriptableObject" in code and "using UnityEngine" in code
        return "class " in code

    def _infer_kind(self, selected: dict[str, Any]) -> str:
        if selected.get("implementation_type") == "runtime_feature":
            return "MonoBehaviour"
        return "PlainClass"

    def _class_name(self, selected: dict[str, Any], script_kind: str) -> str:
        if selected.get("candidate_id"):
            words = safe_slug(selected["candidate_id"], 48).split("_")
            return "".join(word.capitalize() for word in words if word) or "GeneratedUnityScript"
        return f"Generated{script_kind}"

    def _build_script(
        self,
        class_name: str,
        namespace: str,
        script_kind: str,
        task_card: dict[str, Any],
    ) -> str:
        goal = task_card.get("task_goal", "Generated Unity helper")
        if script_kind == "MonoBehaviour":
            return f"""using UnityEngine;

namespace {namespace}
{{
    /// <summary>
    /// Generated helper for: {goal}
    /// Review before copying into the Unity project.
    /// </summary>
    public sealed class {class_name} : MonoBehaviour
    {{
        [SerializeField] private bool logOnStart;

        private void Start()
        {{
            if (logOnStart)
            {{
                Debug.Log($"{{nameof({class_name})}} started.");
            }}
        }}

        public bool IsReady()
        {{
            return isActiveAndEnabled;
        }}
    }}
}}
"""
        if script_kind == "ScriptableObject":
            return f"""using UnityEngine;

namespace {namespace}
{{
    /// <summary>
    /// Generated ScriptableObject for: {goal}
    /// Review before creating assets from this type.
    /// </summary>
    [CreateAssetMenu(fileName = "{class_name}", menuName = "AgentFull/{class_name}")]
    public sealed class {class_name} : ScriptableObject
    {{
        [SerializeField] private string notes = "Generated by AgentFull.";

        public string Notes => notes;
    }}
}}
"""
        return f"""using System;

namespace {namespace}
{{
    /// <summary>
    /// Generated readonly helper for: {goal}
    /// </summary>
    public sealed class {class_name}
    {{
        public DateTime CreatedAtUtc {{ get; }} = DateTime.UtcNow;

        public string Describe()
        {{
            return "{class_name} generated by AgentFull.";
        }}
    }}
}}
"""
