from __future__ import annotations

from pathlib import Path
from typing import Any

from core.file_utils import write_text
from core.skill import Skill


class GenerateUnityEditorToolSkill(Skill):
    name = "generate_unity_editor_tool"
    description = "Generate a readonly Unity EditorWindow tool into generated_code."
    input_schema = {"task_card": "task card", "report_dir": "report directory"}
    output_schema = {"generated_files": "written Editor C# files"}

    def run(self, params: dict[str, Any], context: Any) -> dict[str, Any]:
        report_dir = Path(params.get("report_dir") or context.get("report_dir") or ".")
        generated_dir = report_dir / "generated_code"
        class_name = params.get("class_name") or "AgentProjectOverviewWindow"
        namespace = params.get("namespace") or "RandomWorld.AgentGenerated.Editor"
        content = self._editor_window_template().replace("__CLASS_NAME__", class_name).replace(
            "__NAMESPACE__", namespace
        )
        target = write_text(generated_dir / f"{class_name}.cs", content, overwrite=False)
        return {
            "generated_files": [str(target)],
            "class_name": class_name,
            "namespace": namespace,
            "policy": "readonly_editor_tool_report_folder_first",
            "manual_next_step": "After review, copy this file into Assets/Editor or another Editor folder.",
        }

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
