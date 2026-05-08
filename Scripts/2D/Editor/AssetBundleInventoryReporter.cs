namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using static LAB2D.EditorReportUtility;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Read-only inventory reporter for AssetBundle and StreamingAssets context.
    /// </summary>
    public static class AssetBundleInventoryReporter
    {
        private const string MenuPath = "工具/智能体/导出资源包清单报告";
        private const string ReportRoot = "Assets/Agent/Reports";
        private const string TaskDirectoryName = "efficiency_F011_AssetBundle_Inventory_Report";
        private const string ReportFileName = "assetbundle_inventory_report.md";
        private const string StreamingAssetsRoot = "Assets/StreamingAssets";
        private const string ResourcesLocalPrefabRoot = "Assets/ResourcesLocal/Prefabs";
        private const string AddressableRoot = "Assets/AddressableAssetsData";

        [MenuItem(MenuPath)]
        private static void ExportReport()
        {
            DateTime now = DateTime.Now;
            string directory = CreateUniqueTaskDirectory(ReportRoot, TaskDirectoryName, now);
            string reportPath = NormalizePath(Path.Combine(directory, ReportFileName));
            File.WriteAllText(reportPath, BuildReport(now, directory), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log("AssetBundle inventory report generated: " + reportPath);
        }

        internal static string BuildReport(DateTime now, string taskDirectory = null)
        {
            string normalizedTaskDirectory = NormalizePath(taskDirectory ?? Path.Combine(ReportRoot, now.ToString("yyyy-MM-dd"), TaskDirectoryName));
            List<FileRecord> streamingFiles = CollectFiles(StreamingAssetsRoot, includeMeta: false);
            List<FileRecord> bundleFiles = streamingFiles
                .Where(record => !record.Extension.Equals(".manifest", StringComparison.OrdinalIgnoreCase))
                .OrderBy(record => record.Path, StringComparer.Ordinal)
                .ToList();
            List<FileRecord> manifestFiles = streamingFiles
                .Where(record => record.Extension.Equals(".manifest", StringComparison.OrdinalIgnoreCase))
                .OrderBy(record => record.Path, StringComparer.Ordinal)
                .ToList();
            List<FileRecord> prefabSources = CollectFiles(ResourcesLocalPrefabRoot, includeMeta: false)
                .Where(record => record.Extension.Equals(".prefab", StringComparison.OrdinalIgnoreCase))
                .OrderBy(record => record.Path, StringComparer.Ordinal)
                .ToList();
            List<BundleLabelRecord> bundleLabels = CollectBundleLabels();
            List<FileRecord> addressableFiles = CollectFiles(AddressableRoot, includeMeta: false);
            List<string> missingMetaFiles = CollectMissingMetaFiles(StreamingAssetsRoot, ResourcesLocalPrefabRoot, AddressableRoot);
            List<string> bundleWithoutManifests = FindBundleWithoutManifests(bundleFiles);
            List<string> manifestWithoutBundles = FindManifestWithoutBundles(manifestFiles);
            List<string> namingHints = BuildNamingHints(bundleFiles);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# AssetBundle Inventory Report");
            sb.AppendLine();
            sb.AppendLine("- 生成时间: " + now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("- 工具菜单: `" + MenuPath + "`");
            sb.AppendLine("- 扫描模式: 只读");
            sb.AppendLine("- 本次任务目录: `" + normalizedTaskDirectory + "`");
            sb.AppendLine("- 输出路径: `" + normalizedTaskDirectory + "/" + ReportFileName + "`");
            sb.AppendLine();

            AppendScanScope(sb);
            AppendSummary(sb, streamingFiles, bundleFiles, manifestFiles, prefabSources, bundleLabels, addressableFiles, missingMetaFiles, bundleWithoutManifests, manifestWithoutBundles);
            AppendFileTable(sb, "StreamingAssets Bundle 候选", bundleFiles, "未发现 bundle 候选文件。");
            AppendFileTable(sb, "StreamingAssets Manifest", manifestFiles, "未发现 manifest 文件。");
            AppendStringList(sb, "Bundle 缺失 Manifest", bundleWithoutManifests, "未发现 bundle 缺失 manifest。");
            AppendStringList(sb, "Manifest 缺失 Bundle", manifestWithoutBundles, "未发现 manifest 缺失 bundle。");
            AppendStringList(sb, "命名提示", namingHints, "未发现需要提示的命名。");
            AppendBundleLabelTable(sb, bundleLabels);
            AppendFileTable(sb, "ResourcesLocal Prefab 源", prefabSources, "未发现 Prefab 源文件。");
            AppendFileTable(sb, "Addressables 配置文件", addressableFiles, "未发现 Addressables 配置文件。");
            AppendStringList(sb, "缺失 .meta", missingMetaFiles, "未发现缺失 .meta。");
            AppendGuidance(sb);

            return sb.ToString();
        }

        private static List<FileRecord> CollectFiles(string root, bool includeMeta)
        {
            if (!Directory.Exists(root))
            {
                return new List<FileRecord>();
            }

            return Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                .Where(path => includeMeta || !Path.GetExtension(path).Equals(".meta", StringComparison.OrdinalIgnoreCase))
                .Select(path =>
                {
                    FileInfo fileInfo = new FileInfo(path);
                    return new FileRecord(
                        NormalizePath(path),
                        Path.GetFileNameWithoutExtension(path),
                        Path.GetExtension(path),
                        fileInfo.Exists ? fileInfo.Length : 0);
                })
                .OrderBy(record => record.Path, StringComparer.Ordinal)
                .ToList();
        }

        private static List<BundleLabelRecord> CollectBundleLabels()
        {
            List<BundleLabelRecord> records = new List<BundleLabelRecord>();
            foreach (string bundleName in AssetDatabase.GetAllAssetBundleNames().OrderBy(name => name, StringComparer.Ordinal))
            {
                string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToArray();
                records.Add(new BundleLabelRecord(bundleName, assetPaths));
            }

            return records;
        }

        private static List<string> FindBundleWithoutManifests(IEnumerable<FileRecord> bundleFiles)
        {
            return bundleFiles
                .Where(record => !File.Exists(record.Path + ".manifest"))
                .Select(record => record.Path)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
        }

        private static List<string> FindManifestWithoutBundles(IEnumerable<FileRecord> manifestFiles)
        {
            return manifestFiles
                .Where(record =>
                {
                    string bundlePath = record.Path.Substring(0, record.Path.Length - ".manifest".Length);
                    return !File.Exists(bundlePath);
                })
                .Select(record => record.Path)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
        }

        private static List<string> BuildNamingHints(IEnumerable<FileRecord> bundleFiles)
        {
            List<string> hints = new List<string>();
            foreach (FileRecord record in bundleFiles)
            {
                string fileName = Path.GetFileName(record.Path);
                if (fileName.Equals("StreamingAssets", StringComparison.OrdinalIgnoreCase))
                {
                    hints.Add("`" + record.Path + "` 与根目录同名，排查打包产物时容易混淆。");
                }

                if (record.Extension.Length > 0)
                {
                    hints.Add("`" + record.Path + "` 带有扩展名 `" + record.Extension + "`，确认运行时加载路径是否包含扩展名。");
                }
            }

            return hints.OrderBy(value => value, StringComparer.Ordinal).ToList();
        }

        private static void AppendScanScope(StringBuilder sb)
        {
            sb.AppendLine("## 扫描范围");
            sb.AppendLine();
            sb.AppendLine("- StreamingAssets: `" + StreamingAssetsRoot + "`");
            sb.AppendLine("- Prefab 源: `" + ResourcesLocalPrefabRoot + "`");
            sb.AppendLine("- Addressables 配置: `" + AddressableRoot + "`");
            sb.AppendLine("- AssetBundle 标签: `AssetDatabase.GetAllAssetBundleNames()`");
            sb.AppendLine();
        }

        private static void AppendSummary(
            StringBuilder sb,
            IReadOnlyCollection<FileRecord> streamingFiles,
            IReadOnlyCollection<FileRecord> bundleFiles,
            IReadOnlyCollection<FileRecord> manifestFiles,
            IReadOnlyCollection<FileRecord> prefabSources,
            IReadOnlyCollection<BundleLabelRecord> bundleLabels,
            IReadOnlyCollection<FileRecord> addressableFiles,
            IReadOnlyCollection<string> missingMetaFiles,
            IReadOnlyCollection<string> bundleWithoutManifests,
            IReadOnlyCollection<string> manifestWithoutBundles)
        {
            sb.AppendLine("## 统计");
            sb.AppendLine();
            sb.AppendLine("| 项目 | 数量 |");
            sb.AppendLine("| --- | ---: |");
            sb.AppendLine("| StreamingAssets 文件 | " + streamingFiles.Count + " |");
            sb.AppendLine("| Bundle 候选文件 | " + bundleFiles.Count + " |");
            sb.AppendLine("| Manifest 文件 | " + manifestFiles.Count + " |");
            sb.AppendLine("| Prefab 源文件 | " + prefabSources.Count + " |");
            sb.AppendLine("| AssetBundle 标签 | " + bundleLabels.Count + " |");
            sb.AppendLine("| AssetBundle 标签资产 | " + bundleLabels.Sum(record => record.AssetPaths.Length) + " |");
            sb.AppendLine("| Addressables 配置文件 | " + addressableFiles.Count + " |");
            sb.AppendLine("| Bundle 缺失 Manifest | " + bundleWithoutManifests.Count + " |");
            sb.AppendLine("| Manifest 缺失 Bundle | " + manifestWithoutBundles.Count + " |");
            sb.AppendLine("| 缺失 .meta | " + missingMetaFiles.Count + " |");
            sb.AppendLine();
        }

        private static void AppendFileTable(StringBuilder sb, string title, IReadOnlyList<FileRecord> records, string emptyText)
        {
            sb.AppendLine("## " + title);
            sb.AppendLine();
            if (records.Count == 0)
            {
                sb.AppendLine("- " + emptyText);
                sb.AppendLine();
                return;
            }

            sb.AppendLine("| 路径 | 大小 |");
            sb.AppendLine("| --- | ---: |");
            foreach (FileRecord record in records)
            {
                sb.AppendLine("| `" + record.Path + "` | " + record.Length + " |");
            }

            sb.AppendLine();
        }

        private static void AppendBundleLabelTable(StringBuilder sb, IReadOnlyList<BundleLabelRecord> labels)
        {
            sb.AppendLine("## AssetBundle 标签");
            sb.AppendLine();
            if (labels.Count == 0)
            {
                sb.AppendLine("- 未发现 AssetBundle 标签。");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("| 标签 | 资产数 | 资产路径 |");
            sb.AppendLine("| --- | ---: | --- |");
            foreach (BundleLabelRecord label in labels)
            {
                string assetPaths = string.Join("<br>", label.AssetPaths.Select(path => "`" + path + "`"));
                sb.AppendLine("| `" + EscapeTable(label.Name) + "` | " + label.AssetPaths.Length + " | " + assetPaths + " |");
            }

            sb.AppendLine();
        }

        private static void AppendGuidance(StringBuilder sb)
        {
            sb.AppendLine("## 使用建议");
            sb.AppendLine();
            sb.AppendLine("- 本报告只用于打包前后排查，不会触发 AssetBundle 构建。");
            sb.AppendLine("- 若发现 bundle/manifest 不匹配，单独生成修复任务卡后再处理 `StreamingAssets`。");
            sb.AppendLine("- 若发现 Prefab 源和 AssetBundle 标签不一致，先核对运行时加载路径，再决定是否重打包。");
        }

        private readonly struct FileRecord
        {
            public readonly string Path;
            public readonly string Name;
            public readonly string Extension;
            public readonly long Length;

            public FileRecord(string path, string name, string extension, long length)
            {
                this.Path = path;
                this.Name = name;
                this.Extension = extension;
                this.Length = length;
            }
        }

        private readonly struct BundleLabelRecord
        {
            public readonly string Name;
            public readonly string[] AssetPaths;

            public BundleLabelRecord(string name, string[] assetPaths)
            {
                this.Name = name;
                this.AssetPaths = assetPaths;
            }
        }
    }
}
