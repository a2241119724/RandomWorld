from __future__ import annotations

import re
from pathlib import Path
from typing import Any

from core.file_utils import (
    csharp_class_name,
    csharp_output_dir,
    mono_script_meta_content,
    read_text,
    unique_unity_asset_path,
    unity_generation_policy,
    unity_meta_path,
    write_text,
)
from core.llm_utils import compact_json, extract_csharp_code, record_model_call
from core.project_context import build_llm_project_context
from core.skill import Skill


class GenerateUnityEditorToolSkill(Skill):
    name = "generate_unity_editor_tool"
    description = "Generate a readonly Unity EditorWindow tool into the configured Unity Editor folder."
    input_schema = {"task_card": "task card", "report_dir": "report directory"}
    output_schema = {"generated_files": "written Editor C# files"}

    def run(self, params: dict[str, Any], context: Any) -> dict[str, Any]:
        report_dir = Path(params.get("report_dir") or context.get("report_dir") or ".")
        base_dir = Path(context.get_service("base_dir") or ".")
        configs = context.get("configs", {}).get("unity", {})
        selected = params.get("selected_candidate") or {}
        generated_dir = csharp_output_dir(
            base_dir,
            configs,
            report_dir,
            script_kind="EditorWindow",
            implementation_type=selected.get("implementation_type") or "editor_tool",
        )
        class_name = params.get("class_name") or self._class_name(selected)
        target = unique_unity_asset_path(generated_dir / f"{class_name}.cs")
        class_name = target.stem
        namespace = params.get("namespace") or "RandomWorld.AgentGenerated.Editor"
        content = self._llm_editor_tool(
            class_name,
            namespace,
            params.get("task_card") or {},
            selected,
            context,
        ) or self._editor_window_template().replace("__CLASS_NAME__", class_name).replace(
            "__NAMESPACE__", namespace
        )
        target = write_text(target, content, overwrite=False)
        meta_path = None
        if unity_generation_policy(configs).get("default_output_mode", "project") != "report_only":
            meta_path = write_text(unity_meta_path(target), mono_script_meta_content(), overwrite=False)
        return {
            "generated_files": [str(target)],
            "generated_meta_files": [str(meta_path)] if meta_path else [],
            "class_name": class_name,
            "namespace": namespace,
            "policy": "readonly_editor_tool_configured_editor_path",
            "manual_next_step": "Open Unity and confirm the generated Editor menu compiles.",
        }

    def _llm_editor_tool(
        self,
        class_name: str,
        namespace: str,
        task_card: dict[str, Any],
        selected: dict[str, Any],
        context: Any,
    ) -> str | None:
        router = context.get_service("model_router")
        if not router:
            return None

        base_dir = Path(context.get_service("base_dir") or ".")
        system_prompt = read_text(base_dir / "prompts" / "unity_editor_prompt.md")
        project_context = build_llm_project_context(
            context,
            "generate_unity_editor_tool",
            selected=selected,
            task_card=task_card,
        )
        messages = [
            {
                "role": "system",
                "content": (
                    system_prompt
                    + "\n只返回一个完整 C# 文件，并放在带 csharp 语言标记的 Markdown 代码块中。"
                    + "\n生成 C# 代码中的注释必须使用中文，包括 XML summary、普通注释和说明性注释。"
                ),
            },
            {
                "role": "user",
                "content": (
                    "请生成一个只读 Unity Editor 工具。必须使用给定的 namespace 和 class_name。"
                    "它可以扫描项目并导出报告，但不能修改场景、Prefab、ScriptableObject、"
                    "StreamingAssets、Addressables、构建设置或任何已有项目资源。"
                    "优先使用 Tools/AgentFull 菜单入口。\n\n"
                    "完整上下文包（包含项目结构、关键 C# 片段、会话上下文、用户输入和最近模型调用）：\n"
                    f"{project_context}\n\n"
                    f"class_name: {class_name}\n"
                    f"namespace: {namespace}\n"
                    f"selected_candidate:\n{compact_json(selected, 5000)}\n\n"
                    f"task_card:\n{compact_json(task_card, 7000)}"
                ),
            },
        ]
        response = router.chat_for_task("generate_unity_editor_tool", messages)
        code = extract_csharp_code(response.get("content", ""))
        used = bool(code and self._is_safe_editor_code(code, class_name, namespace))
        record_model_call(
            context,
            "generate_unity_editor_tool",
            response,
            used=used,
            note=f"class_name={class_name}",
        )
        return code if used else None

    def _class_name(self, selected: dict[str, Any]) -> str:
        candidate_id = selected.get("candidate_id")
        if candidate_id == "cand_001_project_overview_editor":
            return "AgentProjectOverviewWindow"
        return csharp_class_name(selected.get("feature_name"), "AgentGeneratedEditorTool")

    def _is_safe_editor_code(self, code: str, class_name: str, namespace: str) -> bool:
        if class_name not in code or namespace not in code or "```" in code:
            return False
        if "UnityEditor" not in code:
            return False
        if "EditorWindow" not in code and "[MenuItem" not in code:
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
        return not any(pattern in code for pattern in forbidden) and self._comments_are_chinese(code)

    def _comments_are_chinese(self, code: str) -> bool:
        line_comments = re.findall(r"^\s*//+.*$", code, flags=re.MULTILINE)
        block_comments = re.findall(r"/\*.*?\*/", code, flags=re.DOTALL)
        for comment in line_comments + block_comments:
            text = re.sub(r"</?\w+[^>]*>", "", comment)
            text = re.sub(r"[/\*]+", " ", text).strip()
            if not text:
                continue
            if re.search(r"[A-Za-z]{4,}", text) and not re.search(r"[\u4e00-\u9fff]", text):
                return False
        return True

    def _editor_window_template(self) -> str:
        return """#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace __NAMESPACE__
{
    public sealed class __CLASS_NAME__ : EditorWindow
    {
        private Vector2 scroll;
        private ProjectStats stats;

        [MenuItem("Tools/AgentFull/Readonly Project Overview")]
        public static void Open()
        {
            GetWindow<__CLASS_NAME__>("Agent Project Overview");
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                Refresh();
            }

            if (GUILayout.Button("Export Markdown", EditorStyles.toolbarButton, GUILayout.Width(130)))
            {
                ExportMarkdown();
            }
            EditorGUILayout.EndHorizontal();

            if (stats == null)
            {
                EditorGUILayout.HelpBox("No stats loaded.", MessageType.Info);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawStat("Scripts", stats.ScriptCount);
            DrawStat("Scenes", stats.SceneCount);
            DrawStat("Prefabs", stats.PrefabCount);
            DrawStat("Materials", stats.MaterialCount);
            DrawStat("Textures", stats.TextureCount);
            DrawStat("Scriptable Assets", stats.AssetFileCount);
            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox("Readonly scan. This window does not modify scenes, prefabs, or assets.", MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawStat(string label, int value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(160));
            EditorGUILayout.SelectableLabel(value.ToString(), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndHorizontal();
        }

        private void Refresh()
        {
            stats = ProjectStats.Collect();
            Repaint();
        }

        private void ExportMarkdown()
        {
            if (stats == null)
            {
                Refresh();
            }

            string folder = "Assets/AgentFull/reports/unity_editor_exports";
            Directory.CreateDirectory(folder);
            string fileName = "project_overview_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".md";
            string path = Path.Combine(folder, fileName);
            File.WriteAllText(path, stats.ToMarkdown(), Encoding.UTF8);
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(Path.GetFullPath(path));
        }

        private sealed class ProjectStats
        {
            public int ScriptCount;
            public int SceneCount;
            public int PrefabCount;
            public int MaterialCount;
            public int TextureCount;
            public int AssetFileCount;

            public static ProjectStats Collect()
            {
                return new ProjectStats
                {
                    ScriptCount = CountByExtension(".cs"),
                    SceneCount = CountByExtension(".unity"),
                    PrefabCount = CountByExtension(".prefab"),
                    MaterialCount = CountByExtension(".mat"),
                    TextureCount = CountByExtensions(new[] { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tif", ".tiff" }),
                    AssetFileCount = CountByExtension(".asset")
                };
            }

            public string ToMarkdown()
            {
                var builder = new StringBuilder();
                builder.AppendLine("# Unity Project Overview");
                builder.AppendLine();
                builder.AppendLine("| Type | Count |");
                builder.AppendLine("| --- | ---: |");
                builder.AppendLine("| Scripts | " + ScriptCount + " |");
                builder.AppendLine("| Scenes | " + SceneCount + " |");
                builder.AppendLine("| Prefabs | " + PrefabCount + " |");
                builder.AppendLine("| Materials | " + MaterialCount + " |");
                builder.AppendLine("| Textures | " + TextureCount + " |");
                builder.AppendLine("| Scriptable Assets | " + AssetFileCount + " |");
                builder.AppendLine();
                builder.AppendLine("Generated by AgentFull readonly EditorWindow.");
                return builder.ToString();
            }

            private static int CountByExtension(string extension)
            {
                if (!Directory.Exists(Application.dataPath))
                {
                    return 0;
                }

                string agentFolder = Path.DirectorySeparatorChar + "AgentFull" + Path.DirectorySeparatorChar;
                return Directory
                    .GetFiles(Application.dataPath, "*" + extension, SearchOption.AllDirectories)
                    .Count(path => path.IndexOf(agentFolder, StringComparison.OrdinalIgnoreCase) < 0);
            }

            private static int CountByExtensions(string[] extensions)
            {
                return extensions.Sum(CountByExtension);
            }
        }
    }
}
#endif
"""
