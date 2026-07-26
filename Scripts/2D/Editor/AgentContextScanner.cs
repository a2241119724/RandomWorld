namespace LAB2D.Editor
{
    using LAB2D;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using static LAB2D.Editor.EditorReportUtility;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Agent规划上下文的只读扫描器。
    /// </summary>
    public static class AgentContextScanner
    {
        private const string MenuPath = "工具/智能体/导出上下文扫描报告";
        private const string ReportRoot = "Assets/Agent/Reports";
        private const string TaskDirectoryName = "efficiency_F002_agent_context_scanner";
        private const string ReportFileName = "agent_context_scan.md";
        private const string AgentRoot = "Assets/Agent";
        private const string ScriptsRoot = "Assets/Scripts/2D";

        private static readonly string[] AgentFiles =
        {
            "Assets/Agent/README.md",
            "Assets/Agent/Docs/ImplementationRoadmap.md",
            "Assets/Agent/Docs/SkillCatalog.md",
            "Assets/Agent/Config/agent_registry.json",
            "Assets/Agent/Config/task_router.json",
            "Assets/Agent/Templates/agent_task_card.md",
        };

        private static readonly string[] ResourceRoots =
        {
            "Assets/Resources/SO",
            "Assets/Resources/Tilemap",
            "Assets/Resources/Images",
        };

        private static readonly string[] HighRiskReadonlyRoots =
        {
            "Assets/Scenes",
            "Assets/StreamingAssets",
            "Assets/AddressableAssetsData",
            "Assets/ResourcesLocal",
        };

        private static readonly string[] SignalTokens =
        {
            "TODO",
            "FIXME",
            "HACK",
            "NotImplementedException",
            "throw new System.NotImplementedException",
            "throw new NotImplementedException",
            "临时",
        };

        private static readonly Regex EmptyMethodRegex = new Regex(
            @"^\s*(public|private|protected|internal)?\s*(static\s+)?[\w<>\[\],\s]+\s+\w+\s*\([^)]*\)\s*\{\s*\}\s*$",
            RegexOptions.Compiled);

        [MenuItem(MenuPath)]
        private static void ExportReport()
        {
            DateTime now = DateTime.Now;
            string directory = CreateUniqueTaskDirectory(ReportRoot, TaskDirectoryName, now);
            string reportPath = NormalizePath(Path.Combine(directory, ReportFileName));
            File.WriteAllText(reportPath, BuildReport(now, directory), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log("Agent context scan report generated: " + reportPath);
        }

        internal static string BuildReport(DateTime now, string taskDirectory = null)
        {
            string normalizedTaskDirectory = NormalizePath(taskDirectory ?? Path.Combine(ReportRoot, now.ToString("yyyy-MM-dd"), TaskDirectoryName));
            List<FileRecord> agentFileRecords = CollectKnownFiles(AgentFiles);
            List<FileRecord> taskCards = CollectFiles(ReportRoot, "task_*.md");
            List<ModuleRecord> modules = CollectScriptModules();
            List<SignalRecord> signals = CollectSignals(ScriptsRoot);
            List<ResourceRootRecord> resourceRecords = CollectResourceRoots(ResourceRoots);
            List<ResourceRootRecord> highRiskRecords = CollectResourceRoots(HighRiskReadonlyRoots);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Agent Context Scan");
            sb.AppendLine();
            sb.AppendLine("- 生成时间: " + now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("- 工具菜单: `" + MenuPath + "`");
            sb.AppendLine("- 扫描模式: 只读");
            sb.AppendLine("- 本次任务目录: `" + normalizedTaskDirectory + "`");
            sb.AppendLine("- 输出路径: `" + normalizedTaskDirectory + "/" + ReportFileName + "`");
            sb.AppendLine();

            AppendAgentFiles(sb, agentFileRecords);
            AppendTaskCards(sb, taskCards);
            AppendScriptModules(sb, modules);
            AppendSignals(sb, signals);
            AppendResourceRoots(sb, "资源概况", resourceRecords);
            AppendResourceRoots(sb, "高风险区域只读概况", highRiskRecords);

            sb.AppendLine("## 后续建议");
            sb.AppendLine();
            sb.AppendLine("- 将本报告作为每次 Agent 自动发现前的上下文索引，优先查看 TODO 信号、资源缺口和历史任务卡。");
            sb.AppendLine("- 高风险区域仅用于发现检查机会，不在本工具内做任何修复或写入。");
            sb.AppendLine("- 如需修复资源、存档、Photon 或 AssetBundle 问题，单独生成任务卡后再执行。");

            return sb.ToString();
        }

        private static List<FileRecord> CollectKnownFiles(IEnumerable<string> paths)
        {
            List<FileRecord> records = new List<FileRecord>();
            foreach (string path in paths)
            {
                FileInfo fileInfo = new FileInfo(path);
                records.Add(new FileRecord(NormalizePath(path), fileInfo.Exists, fileInfo.Exists ? fileInfo.Length : 0));
            }

            return records;
        }

        private static List<FileRecord> CollectFiles(string root, string searchPattern)
        {
            if (!Directory.Exists(root))
            {
                return new List<FileRecord>();
            }

            return Directory.GetFiles(root, searchPattern, SearchOption.AllDirectories)
                .Select(path =>
                {
                    FileInfo fileInfo = new FileInfo(path);
                    return new FileRecord(NormalizePath(path), fileInfo.Exists, fileInfo.Exists ? fileInfo.Length : 0);
                })
                .OrderBy(record => record.Path, StringComparer.Ordinal)
                .ToList();
        }

        private static List<ModuleRecord> CollectScriptModules()
        {
            List<ModuleRecord> records = new List<ModuleRecord>();
            if (!Directory.Exists(ScriptsRoot))
            {
                return records;
            }

            foreach (string directory in Directory.GetDirectories(ScriptsRoot, "*", SearchOption.TopDirectoryOnly)
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                List<string> files = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToList();
                List<string> signalFiles = files
                    .Where(path => !IsSelf(path))
                    .ToList();
                int signalCount = signalFiles.Sum(CountSignalLines);
                int emptyMethodCount = signalFiles.Sum(CountEmptyMethodLines);
                records.Add(new ModuleRecord(NormalizePath(directory), files.Count, signalCount, emptyMethodCount));
            }

            List<string> rootFiles = Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
            if (rootFiles.Count > 0)
            {
                List<string> signalFiles = rootFiles
                    .Where(path => !IsSelf(path))
                    .ToList();
                records.Insert(0, new ModuleRecord(
                    ScriptsRoot,
                    rootFiles.Count,
                    signalFiles.Sum(CountSignalLines),
                    signalFiles.Sum(CountEmptyMethodLines)));
            }

            return records;
        }

        private static List<SignalRecord> CollectSignals(string root)
        {
            List<SignalRecord> records = new List<SignalRecord>();
            if (!Directory.Exists(root))
            {
                return records;
            }

            foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                if (IsSelf(file))
                {
                    continue;
                }

                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (ContainsSignal(line) || EmptyMethodRegex.IsMatch(line))
                    {
                        records.Add(new SignalRecord(NormalizePath(file), i + 1, line.Trim()));
                    }
                }
            }

            return records;
        }

        private static List<ResourceRootRecord> CollectResourceRoots(IEnumerable<string> roots)
        {
            List<ResourceRootRecord> records = new List<ResourceRootRecord>();
            foreach (string root in roots)
            {
                if (!Directory.Exists(root))
                {
                    records.Add(new ResourceRootRecord(root, false, 0, 0, 0));
                    continue;
                }

                int fileCount = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                    .Count(path => !Path.GetExtension(path).Equals(".meta", StringComparison.OrdinalIgnoreCase));
                int directoryCount = Directory.GetDirectories(root, "*", SearchOption.AllDirectories).Length;
                int missingMetaCount = CountMissingMeta(root);
                records.Add(new ResourceRootRecord(root, true, fileCount, directoryCount, missingMetaCount));
            }

            return records;
        }

        private static int CountSignalLines(string file)
        {
            return File.ReadLines(file).Count(ContainsSignal);
        }

        private static int CountEmptyMethodLines(string file)
        {
            return File.ReadLines(file).Count(line => EmptyMethodRegex.IsMatch(line));
        }

        private static bool ContainsSignal(string line)
        {
            return SignalTokens.Any(token => line.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsSelf(string path)
        {
            return Path.GetFileName(path).Equals(nameof(AgentContextScanner) + ".cs", StringComparison.Ordinal);
        }

        private static void AppendAgentFiles(StringBuilder sb, IReadOnlyList<FileRecord> files)
        {
            sb.AppendLine("## Agent 基础文件");
            sb.AppendLine();
            sb.AppendLine("| 路径 | 状态 | 大小 |");
            sb.AppendLine("| --- | --- | ---: |");
            foreach (FileRecord file in files)
            {
                sb.AppendLine("| `" + file.Path + "` | " + (file.Exists ? "存在" : "缺失") + " | " + file.Length + " |");
            }

            sb.AppendLine();
        }

        private static void AppendTaskCards(StringBuilder sb, IReadOnlyList<FileRecord> taskCards)
        {
            sb.AppendLine("## 历史任务卡");
            sb.AppendLine();
            if (taskCards.Count == 0)
            {
                sb.AppendLine("- 未发现历史任务卡。");
                sb.AppendLine();
                return;
            }

            foreach (FileRecord taskCard in taskCards)
            {
                sb.AppendLine("- `" + taskCard.Path + "`");
            }

            sb.AppendLine();
        }

        private static void AppendScriptModules(StringBuilder sb, IReadOnlyList<ModuleRecord> modules)
        {
            sb.AppendLine("## Scripts/2D 模块概况");
            sb.AppendLine();
            sb.AppendLine("| 模块 | C# 文件 | TODO/临时信号 | 空方法信号 |");
            sb.AppendLine("| --- | ---: | ---: | ---: |");
            foreach (ModuleRecord module in modules)
            {
                sb.AppendLine("| `" + module.Path + "` | " + module.ScriptCount + " | " + module.SignalCount + " | " + module.EmptyMethodCount + " |");
            }

            sb.AppendLine();
        }

        private static void AppendSignals(StringBuilder sb, IReadOnlyList<SignalRecord> signals)
        {
            sb.AppendLine("## 后续开发信号");
            sb.AppendLine();
            if (signals.Count == 0)
            {
                sb.AppendLine("- 未发现 TODO/FIXME/临时实现/空方法信号。");
                sb.AppendLine();
                return;
            }

            foreach (SignalRecord signal in signals.Take(80))
            {
                sb.AppendLine("- `" + signal.Path + ":" + signal.Line + "` " + EscapeTable(signal.Text));
            }

            if (signals.Count > 80)
            {
                sb.AppendLine("- 其余 " + (signals.Count - 80) + " 条信号已省略。");
            }

            sb.AppendLine();
        }

        private static void AppendResourceRoots(StringBuilder sb, string title, IReadOnlyList<ResourceRootRecord> roots)
        {
            sb.AppendLine("## " + title);
            sb.AppendLine();
            sb.AppendLine("| 路径 | 状态 | 文件 | 子目录 | 缺失 .meta |");
            sb.AppendLine("| --- | --- | ---: | ---: | ---: |");
            foreach (ResourceRootRecord root in roots)
            {
                sb.AppendLine("| `" + root.Path + "` | " + (root.Exists ? "存在" : "缺失") + " | " + root.FileCount + " | " + root.DirectoryCount + " | " + root.MissingMetaCount + " |");
            }

            sb.AppendLine();
        }

        private readonly struct FileRecord
        {
            public readonly string Path;
            public readonly bool Exists;
            public readonly long Length;

            public FileRecord(string path, bool exists, long length)
            {
                this.Path = path;
                this.Exists = exists;
                this.Length = length;
            }
        }

        private readonly struct ModuleRecord
        {
            public readonly string Path;
            public readonly int ScriptCount;
            public readonly int SignalCount;
            public readonly int EmptyMethodCount;

            public ModuleRecord(string path, int scriptCount, int signalCount, int emptyMethodCount)
            {
                this.Path = path;
                this.ScriptCount = scriptCount;
                this.SignalCount = signalCount;
                this.EmptyMethodCount = emptyMethodCount;
            }
        }

        private readonly struct SignalRecord
        {
            public readonly string Path;
            public readonly int Line;
            public readonly string Text;

            public SignalRecord(string path, int line, string text)
            {
                this.Path = path;
                this.Line = line;
                this.Text = text;
            }
        }

        private readonly struct ResourceRootRecord
        {
            public readonly string Path;
            public readonly bool Exists;
            public readonly int FileCount;
            public readonly int DirectoryCount;
            public readonly int MissingMetaCount;

            public ResourceRootRecord(string path, bool exists, int fileCount, int directoryCount, int missingMetaCount)
            {
                this.Path = path;
                this.Exists = exists;
                this.FileCount = fileCount;
                this.DirectoryCount = directoryCount;
                this.MissingMetaCount = missingMetaCount;
            }
        }
    }
}
