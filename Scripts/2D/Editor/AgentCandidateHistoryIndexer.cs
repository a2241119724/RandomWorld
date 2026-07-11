namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using static LAB2D.EditorReportUtility;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 跨报告构建Agent候选状态的只读索引。
    /// </summary>
    public static class AgentCandidateHistoryIndexer
    {
        private const string MenuPath = "工具/智能体/导出候选历史状态索引";
        private const string ReportRoot = "Assets/Agent/Reports";
        private const string TaskDirectoryName = "efficiency_F010_candidate_history_index";
        private const string ReportFileName = "candidate_history_index.md";

        private static readonly string[] StatusPrecedence =
        {
            "[DONE]",
            "[PARTIAL]",
            "[BLOCKED]",
            "[SKIPPED]",
            "[TODO]",
        };

        private static readonly string[] StatusTokens =
        {
            "[TODO]",
            "[DONE]",
            "[SKIPPED]",
            "[BLOCKED]",
            "[PARTIAL]",
        };

        private static readonly Regex CandidateIdRegex = new Regex(@"\bF\d{3}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex TaskIdRegex = new Regex(@"任务\s*ID\s*[:：]\s*(?<value>.+)$", RegexOptions.Compiled);
        private static readonly Regex CandidateIdLineRegex = new Regex(@"候选ID\s*[:：]\s*(?<value>F\d{3})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CurrentStatusRegex = new Regex(@"当前状态\s*[:：]\s*(?<value>.+)$", RegexOptions.Compiled);
        private static readonly Regex FinalStatusRegex = new Regex(@"最终状态\s*[:：]\s*(?<value>.+)$", RegexOptions.Compiled);

        [MenuItem(MenuPath)]
        private static void ExportReport()
        {
            DateTime now = DateTime.Now;
            string directory = CreateUniqueTaskDirectory(ReportRoot, TaskDirectoryName, now);
            string reportPath = NormalizePath(Path.Combine(directory, ReportFileName));
            File.WriteAllText(reportPath, BuildReport(now, directory), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log("Agent candidate history index generated: " + reportPath);
        }

        internal static string BuildReport(DateTime now, string taskDirectory = null)
        {
            string normalizedTaskDirectory = NormalizePath(taskDirectory ?? Path.Combine(ReportRoot, now.ToString("yyyy-MM-dd"), TaskDirectoryName));
            HistorySnapshot snapshot = CollectHistorySnapshot();
            List<CandidateSummary> summaries = BuildCandidateSummaries(snapshot.Candidates);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Agent Candidate History Index");
            sb.AppendLine();
            sb.AppendLine("- 生成时间: " + now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("- 工具菜单: `" + MenuPath + "`");
            sb.AppendLine("- 扫描模式: 只读扫描历史 Markdown，仅写入本报告");
            sb.AppendLine("- 本次任务目录: `" + normalizedTaskDirectory + "`");
            sb.AppendLine("- 输出路径: `" + normalizedTaskDirectory + "/" + ReportFileName + "`");
            sb.AppendLine();

            AppendScanScope(sb, snapshot);
            AppendStatusSummary(sb, summaries);
            AppendCandidateIndex(sb, summaries);
            AppendSourceDocuments(sb, "Feature Discovery 文件", snapshot.FeatureDiscoveryFiles);
            AppendSourceDocuments(sb, "任务卡文件", snapshot.TaskCardFiles);
            AppendSourceDocuments(sb, "验证记录文件", snapshot.ValidationFiles);
            AppendGuidance(sb);

            return sb.ToString();
        }

        private static HistorySnapshot CollectHistorySnapshot()
        {
            List<FileRecord> featureDiscoveryFiles = CollectFiles("feature_discovery.md");
            List<FileRecord> taskCardFiles = CollectFiles("task_*.md");
            List<FileRecord> validationFiles = CollectFiles("validation_*.md");

            List<CandidateRecord> candidates = new List<CandidateRecord>();
            foreach (FileRecord file in featureDiscoveryFiles)
            {
                candidates.AddRange(ParseFeatureDiscovery(file.Path));
            }

            foreach (FileRecord file in taskCardFiles)
            {
                CandidateRecord? candidate = ParseTaskCard(file.Path);
                if (candidate.HasValue)
                {
                    candidates.Add(candidate.Value);
                }
            }

            foreach (FileRecord file in validationFiles)
            {
                CandidateRecord? candidate = ParseValidation(file.Path);
                if (candidate.HasValue)
                {
                    candidates.Add(candidate.Value);
                }
            }

            return new HistorySnapshot(featureDiscoveryFiles, taskCardFiles, validationFiles, candidates);
        }

        private static List<FileRecord> CollectFiles(string searchPattern)
        {
            if (!Directory.Exists(ReportRoot))
            {
                return new List<FileRecord>();
            }

            return Directory.GetFiles(ReportRoot, searchPattern, SearchOption.AllDirectories)
                .Select(path =>
                {
                    FileInfo fileInfo = new FileInfo(path);
                    return new FileRecord(NormalizePath(path), fileInfo.Length);
                })
                .OrderBy(record => record.Path, StringComparer.Ordinal)
                .ToList();
        }

        private static List<CandidateRecord> ParseFeatureDiscovery(string path)
        {
            List<CandidateRecord> records = new List<CandidateRecord>();
            foreach (string line in File.ReadLines(path))
            {
                string trimmed = line.Trim();
                if (!trimmed.StartsWith("|", StringComparison.Ordinal) || trimmed.Contains("---"))
                {
                    continue;
                }

                string[] cells = trimmed.Trim('|')
                    .Split('|')
                    .Select(cell => cell.Trim())
                    .ToArray();

                int statusIndex = Array.FindIndex(cells, IsStatusCell);
                if (statusIndex >= 0)
                {
                    string candidateId = FindCandidateId(statusIndex + 1 < cells.Length ? cells[statusIndex + 1] : trimmed);
                    if (!string.IsNullOrEmpty(candidateId))
                    {
                        string featureName = statusIndex + 2 < cells.Length ? cells[statusIndex + 2] : string.Empty;
                        records.Add(new CandidateRecord(candidateId, featureName, NormalizeStatus(cells[statusIndex]), path, "feature_discovery", trimmed));
                    }

                    continue;
                }

                int idIndex = FindCandidateIdCellIndex(cells);
                if (idIndex >= 0)
                {
                    string candidateId = FindCandidateId(cells[idIndex]);
                    string featureName = idIndex + 1 < cells.Length ? cells[idIndex + 1] : string.Empty;
                    string status = InferLegacyFeatureStatus(cells);
                    records.Add(new CandidateRecord(candidateId, featureName, status, path, "feature_discovery", trimmed));
                }
            }

            return records;
        }

        private static CandidateRecord? ParseTaskCard(string path)
        {
            string candidateId = FindCandidateId(Path.GetFileName(path));
            string status = "[TODO]";
            string featureName = string.Empty;

            foreach (string line in File.ReadLines(path))
            {
                Match taskIdMatch = TaskIdRegex.Match(line);
                if (taskIdMatch.Success)
                {
                    candidateId = FindCandidateId(taskIdMatch.Groups["value"].Value) ?? candidateId;
                }

                Match currentStatusMatch = CurrentStatusRegex.Match(line);
                if (currentStatusMatch.Success)
                {
                    status = NormalizeStatus(currentStatusMatch.Groups["value"].Value);
                }

                if (string.IsNullOrEmpty(featureName))
                {
                    string lineCandidateId = FindCandidateId(line);
                    if (!string.IsNullOrEmpty(lineCandidateId) && (line.Contains("：") || line.Contains(":")))
                    {
                        featureName = ExtractNameAfterCandidateId(line, lineCandidateId);
                    }
                }
            }

            if (string.IsNullOrEmpty(candidateId))
            {
                return null;
            }

            return new CandidateRecord(candidateId, featureName, status, path, "task_card", "任务卡状态: " + status);
        }

        private static CandidateRecord? ParseValidation(string path)
        {
            string candidateId = FindCandidateId(Path.GetFileName(path));
            string status = "[TODO]";
            string featureName = string.Empty;

            foreach (string line in File.ReadLines(path))
            {
                Match candidateMatch = CandidateIdLineRegex.Match(line);
                if (candidateMatch.Success)
                {
                    candidateId = candidateMatch.Groups["value"].Value.ToUpperInvariant();
                }

                Match finalStatusMatch = FinalStatusRegex.Match(line);
                if (finalStatusMatch.Success)
                {
                    status = NormalizeStatus(finalStatusMatch.Groups["value"].Value);
                }

                if (string.IsNullOrEmpty(featureName) && line.StartsWith("#", StringComparison.Ordinal))
                {
                    featureName = line.TrimStart('#').Trim();
                }
            }

            if (string.IsNullOrEmpty(candidateId))
            {
                return null;
            }

            return new CandidateRecord(candidateId, featureName, status, path, "validation", "验证记录最终状态: " + status);
        }

        private static List<CandidateSummary> BuildCandidateSummaries(IReadOnlyList<CandidateRecord> records)
        {
            return records
                .GroupBy(record => record.CandidateId, StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    List<CandidateRecord> groupRecords = group.OrderBy(record => record.SourcePath, StringComparer.Ordinal).ToList();
                    string status = ResolveStatus(groupRecords);
                    string featureName = groupRecords.Select(record => record.FeatureName).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
                    return new CandidateSummary(group.Key.ToUpperInvariant(), featureName, status, groupRecords);
                })
                .OrderBy(summary => summary.CandidateId, StringComparer.Ordinal)
                .ToList();
        }

        private static string ResolveStatus(IReadOnlyList<CandidateRecord> records)
        {
            foreach (string status in StatusPrecedence)
            {
                if (records.Any(record => record.Status == status))
                {
                    return status;
                }
            }

            return "[TODO]";
        }

        private static void AppendScanScope(StringBuilder sb, HistorySnapshot snapshot)
        {
            sb.AppendLine("## 扫描范围");
            sb.AppendLine();
            sb.AppendLine("| 类型 | 数量 |");
            sb.AppendLine("| --- | ---: |");
            sb.AppendLine("| `feature_discovery.md` | " + snapshot.FeatureDiscoveryFiles.Count + " |");
            sb.AppendLine("| `task_*.md` | " + snapshot.TaskCardFiles.Count + " |");
            sb.AppendLine("| `validation_*.md` | " + snapshot.ValidationFiles.Count + " |");
            sb.AppendLine("| 候选状态记录 | " + snapshot.Candidates.Count + " |");
            sb.AppendLine();
        }

        private static void AppendStatusSummary(StringBuilder sb, IReadOnlyList<CandidateSummary> summaries)
        {
            sb.AppendLine("## 状态汇总");
            sb.AppendLine();
            sb.AppendLine("| 状态 | 候选数 |");
            sb.AppendLine("| --- | ---: |");
            foreach (string status in StatusTokens)
            {
                sb.AppendLine("| " + status + " | " + summaries.Count(summary => summary.Status == status) + " |");
            }

            sb.AppendLine();
        }

        private static void AppendCandidateIndex(StringBuilder sb, IReadOnlyList<CandidateSummary> summaries)
        {
            sb.AppendLine("## 候选索引");
            sb.AppendLine();
            if (summaries.Count == 0)
            {
                sb.AppendLine("- 未发现候选状态记录。");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("| 候选ID | 归并状态 | 功能名称 | 来源文件 | 去重依据 |");
            sb.AppendLine("| --- | --- | --- | --- | --- |");
            foreach (CandidateSummary summary in summaries)
            {
                string sourceFiles = string.Join("<br>", summary.Records.Select(record => "`" + record.SourcePath + "`").Distinct().Take(4));
                string evidence = string.Join("<br>", summary.Records.Select(record => EscapeTable(record.Evidence)).Distinct().Take(4));
                sb.AppendLine("| " + summary.CandidateId + " | " + summary.Status + " | " + EscapeTable(summary.FeatureName) + " | " + sourceFiles + " | " + evidence + " |");
            }

            sb.AppendLine();
        }

        private static void AppendSourceDocuments(StringBuilder sb, string title, IReadOnlyList<FileRecord> files)
        {
            sb.AppendLine("## " + title);
            sb.AppendLine();
            if (files.Count == 0)
            {
                sb.AppendLine("- 未发现。");
                sb.AppendLine();
                return;
            }

            foreach (FileRecord file in files)
            {
                sb.AppendLine("- `" + file.Path + "` (" + file.Length + " bytes)");
            }

            sb.AppendLine();
        }

        private static void AppendGuidance(StringBuilder sb)
        {
            sb.AppendLine("## 使用建议");
            sb.AppendLine();
            sb.AppendLine("- 后续自动发现候选前，先查看本索引中的 `[DONE]` 候选，避免重复实现。");
            sb.AppendLine("- `[SKIPPED]` 和 `[BLOCKED]` 候选可以继续保留，但再次选择前应先确认风险是否已经降低。");
            sb.AppendLine("- 本工具不修改业务资源；如需修复场景、预制体、SO、存档、联机同步或打包产物，请单独生成任务卡。");
        }

        private static bool IsStatusCell(string cell)
        {
            return StatusTokens.Any(token => cell.Equals(token, StringComparison.OrdinalIgnoreCase));
        }

        private static int FindCandidateIdCellIndex(IReadOnlyList<string> cells)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (!string.IsNullOrEmpty(FindCandidateId(cells[i])))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string FindCandidateId(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            Match match = CandidateIdRegex.Match(value);
            return match.Success ? match.Value.ToUpperInvariant() : null;
        }

        private static string InferLegacyFeatureStatus(IEnumerable<string> cells)
        {
            foreach (string cell in cells)
            {
                string status = NormalizeStatus(cell);
                if (status != "[TODO]")
                {
                    return status;
                }
            }

            return "[TODO]";
        }

        private static string NormalizeStatus(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "[TODO]";
            }

            foreach (string token in StatusTokens)
            {
                if (value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return token.ToUpperInvariant();
                }
            }

            string lower = value.ToLowerInvariant();
            if (lower.Contains("done") || value.Contains("已完成") || value.Contains("已落地"))
            {
                return "[DONE]";
            }

            if (lower.Contains("partial") || value.Contains("部分"))
            {
                return "[PARTIAL]";
            }

            if (lower.Contains("blocked") || lower.Contains("rolledback") || value.Contains("受阻"))
            {
                return "[BLOCKED]";
            }

            if (lower.Contains("skipped") || value.Contains("跳过"))
            {
                return "[SKIPPED]";
            }

            return "[TODO]";
        }

        private static string ExtractNameAfterCandidateId(string line, string candidateId)
        {
            int candidateIndex = line.IndexOf(candidateId, StringComparison.OrdinalIgnoreCase);
            if (candidateIndex < 0)
            {
                return string.Empty;
            }

            string tail = line.Substring(candidateIndex + candidateId.Length).Trim(' ', ':', '：', '-', '。', '.', '`');
            return tail;
        }

        private readonly struct HistorySnapshot
        {
            public readonly List<FileRecord> FeatureDiscoveryFiles;
            public readonly List<FileRecord> TaskCardFiles;
            public readonly List<FileRecord> ValidationFiles;
            public readonly List<CandidateRecord> Candidates;

            public HistorySnapshot(
                List<FileRecord> featureDiscoveryFiles,
                List<FileRecord> taskCardFiles,
                List<FileRecord> validationFiles,
                List<CandidateRecord> candidates)
            {
                this.FeatureDiscoveryFiles = featureDiscoveryFiles;
                this.TaskCardFiles = taskCardFiles;
                this.ValidationFiles = validationFiles;
                this.Candidates = candidates;
            }
        }

        private readonly struct FileRecord
        {
            public readonly string Path;
            public readonly long Length;

            public FileRecord(string path, long length)
            {
                this.Path = path;
                this.Length = length;
            }
        }

        private readonly struct CandidateRecord
        {
            public readonly string CandidateId;
            public readonly string FeatureName;
            public readonly string Status;
            public readonly string SourcePath;
            public readonly string SourceType;
            public readonly string Evidence;

            public CandidateRecord(string candidateId, string featureName, string status, string sourcePath, string sourceType, string evidence)
            {
                this.CandidateId = candidateId;
                this.FeatureName = featureName;
                this.Status = status;
                this.SourcePath = NormalizePath(sourcePath);
                this.SourceType = sourceType;
                this.Evidence = evidence;
            }
        }

        private readonly struct CandidateSummary
        {
            public readonly string CandidateId;
            public readonly string FeatureName;
            public readonly string Status;
            public readonly List<CandidateRecord> Records;

            public CandidateSummary(string candidateId, string featureName, string status, List<CandidateRecord> records)
            {
                this.CandidateId = candidateId;
                this.FeatureName = featureName;
                this.Status = status;
                this.Records = records;
            }
        }
    }
}
