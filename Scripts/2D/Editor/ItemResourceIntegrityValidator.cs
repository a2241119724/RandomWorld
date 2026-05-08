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
    /// Item resource binding validator.
    /// </summary>
    public static class ItemResourceIntegrityValidator
    {
        private const string Prefix = "工具/数据/";
        private const string ScriptableRoot = "Assets/Resources/SO";
        private const string ItemTileRoot = "Assets/Resources/Tilemap/Item";
        private const string ItemImageRoot = "Assets/Resources/Images/Item";
        private const string ReportRoot = "Assets/Agent/Reports";
        private const string TaskDirectoryName = "efficiency_F001_item_resource_integrity_validator";
        private const string ReportFileName = "resource_integrity_report.md";

        private static readonly string[] TileExtensions = { ".asset" };
        private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".psd", ".tga" };

        [MenuItem(Prefix + "导出道具资源绑定报告")]
        private static void ExportReport()
        {
            DateTime now = DateTime.Now;
            string directory = CreateUniqueTaskDirectory(ReportRoot, TaskDirectoryName, now);
            string report = BuildReport(now, directory);
            string reportPath = NormalizePath(Path.Combine(directory, ReportFileName));
            File.WriteAllText(reportPath, report, new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log("Item资源绑定报告已生成: " + reportPath);
        }

        internal static string BuildReport(DateTime now, string taskDirectory = null)
        {
            string normalizedTaskDirectory = NormalizePath(taskDirectory ?? Path.Combine(ReportRoot, now.ToString("yyyy-MM-dd"), TaskDirectoryName));
            List<ItemRecord> itemRecords = CollectItemRecords();
            List<AssetRecord> tileRecords = CollectAssetRecords(ItemTileRoot, TileExtensions);
            List<AssetRecord> imageRecords = CollectAssetRecords(ItemImageRoot, ImageExtensions);
            List<string> missingMetaFiles = CollectMissingMetaFiles(ScriptableRoot, ItemTileRoot, ItemImageRoot);

            HashSet<string> tileNames = new (tileRecords.Select(record => record.Name), StringComparer.Ordinal);
            HashSet<string> imageNames = new (imageRecords.Select(record => record.Name), StringComparer.Ordinal);
            List<string> itemNames = itemRecords
                .Select(record => record.EnName)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            List<string> missingTiles = itemNames
                .Where(name => !tileNames.Contains(name))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
            List<string> missingImages = itemNames
                .Where(name => !imageNames.Contains(name))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            List<IGrouping<string, ItemRecord>> duplicateItems = itemRecords
                .GroupBy(record => record.EnName, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToList();
            List<IGrouping<string, AssetRecord>> duplicateTiles = tileRecords
                .GroupBy(record => record.Name, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToList();
            List<IGrouping<string, AssetRecord>> duplicateImages = imageRecords
                .GroupBy(record => record.Name, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToList();

            StringBuilder sb = new ();
            sb.AppendLine("# Item Resource Integrity Report");
            sb.AppendLine();
            sb.AppendLine("- 生成时间: " + now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("- 工具菜单: `工具/数据/导出道具资源绑定报告`");
            sb.AppendLine("- 扫描模式: 只读");
            sb.AppendLine("- 本次任务目录: `" + normalizedTaskDirectory + "`");
            sb.AppendLine("- 输出路径: `" + normalizedTaskDirectory + "/" + ReportFileName + "`");
            sb.AppendLine();
            sb.AppendLine("## 扫描范围");
            sb.AppendLine();
            sb.AppendLine("- SO: `" + ScriptableRoot + "`");
            sb.AppendLine("- Tile: `" + ItemTileRoot + "`");
            sb.AppendLine("- Image: `" + ItemImageRoot + "`");
            sb.AppendLine();
            sb.AppendLine("## 统计");
            sb.AppendLine();
            sb.AppendLine("| 项目 | 数量 |");
            sb.AppendLine("| --- | ---: |");
            sb.AppendLine("| SO EnName | " + itemNames.Count + " |");
            sb.AppendLine("| Item Tile 资源 | " + tileRecords.Count + " |");
            sb.AppendLine("| Item Image 资源 | " + imageRecords.Count + " |");
            sb.AppendLine("| 缺失 Tile 绑定 | " + missingTiles.Count + " |");
            sb.AppendLine("| 缺失 Image 绑定 | " + missingImages.Count + " |");
            sb.AppendLine("| 重复 EnName | " + duplicateItems.Count + " |");
            sb.AppendLine("| 重复 Tile 名称 | " + duplicateTiles.Count + " |");
            sb.AppendLine("| 重复 Image 名称 | " + duplicateImages.Count + " |");
            sb.AppendLine("| 缺失 .meta | " + missingMetaFiles.Count + " |");
            sb.AppendLine();

            AppendStringList(sb, "缺失 Tile 绑定", missingTiles, "未发现缺失 Tile 绑定。", wrapValuesInCode: true);
            AppendStringList(sb, "缺失 Image 绑定", missingImages, "未发现缺失 Image 绑定。", wrapValuesInCode: true);
            AppendItemDuplicateList(sb, "重复 EnName", duplicateItems);
            AppendAssetDuplicateList(sb, "重复 Tile 名称", duplicateTiles);
            AppendAssetDuplicateList(sb, "重复 Image 名称", duplicateImages);
            AppendStringList(sb, "缺失 .meta", missingMetaFiles, "未发现缺失 .meta。", wrapValuesInCode: true);

            sb.AppendLine("## Item 记录");
            sb.AppendLine();
            sb.AppendLine("| EnName | SO |");
            sb.AppendLine("| --- | --- |");
            foreach (ItemRecord record in itemRecords.OrderBy(record => record.EnName, StringComparer.Ordinal))
            {
                sb.AppendLine("| " + EscapeTable(record.EnName) + " | `" + record.SourcePath + "` |");
            }

            return sb.ToString();
        }

        private static List<ItemRecord> CollectItemRecords()
        {
            List<ItemRecord> records = new ();
            if (!Directory.Exists(ScriptableRoot))
            {
                return records;
            }

            foreach (string assetPath in Directory.GetFiles(ScriptableRoot, "*.asset", SearchOption.AllDirectories)
                         .Select(NormalizePath)
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (asset == null)
                {
                    continue;
                }

                SerializedObject serializedObject = new (asset);
                SerializedProperty iterator = serializedObject.GetIterator();
                while (iterator.NextVisible(true))
                {
                    if (iterator.name != "EnName" || iterator.propertyType != SerializedPropertyType.String)
                    {
                        continue;
                    }

                    string enName = iterator.stringValue.Trim();
                    if (!string.IsNullOrEmpty(enName))
                    {
                        records.Add(new ItemRecord(enName, assetPath));
                    }
                }
            }

            return records;
        }

        private static List<AssetRecord> CollectAssetRecords(string root, IReadOnlyCollection<string> extensions)
        {
            List<AssetRecord> records = new ();
            if (!Directory.Exists(root))
            {
                return records;
            }

            foreach (string assetPath in Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                         .Select(NormalizePath)
                         .Where(path => extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                records.Add(new AssetRecord(Path.GetFileNameWithoutExtension(assetPath), assetPath));
            }

            return records;
        }

        private static void AppendItemDuplicateList(StringBuilder sb, string title, IReadOnlyCollection<IGrouping<string, ItemRecord>> groups)
        {
            sb.AppendLine("## " + title);
            sb.AppendLine();
            if (groups.Count == 0)
            {
                sb.AppendLine("- 未发现" + title + "。");
                sb.AppendLine();
                return;
            }

            foreach (IGrouping<string, ItemRecord> group in groups)
            {
                sb.AppendLine("- `" + group.Key + "`");
                foreach (ItemRecord record in group)
                {
                    sb.AppendLine("  - `" + record.SourcePath + "`");
                }
            }

            sb.AppendLine();
        }

        private static void AppendAssetDuplicateList(StringBuilder sb, string title, IReadOnlyCollection<IGrouping<string, AssetRecord>> groups)
        {
            sb.AppendLine("## " + title);
            sb.AppendLine();
            if (groups.Count == 0)
            {
                sb.AppendLine("- 未发现" + title + "。");
                sb.AppendLine();
                return;
            }

            foreach (IGrouping<string, AssetRecord> group in groups)
            {
                sb.AppendLine("- `" + group.Key + "`");
                foreach (AssetRecord record in group)
                {
                    sb.AppendLine("  - `" + record.Path + "`");
                }
            }

            sb.AppendLine();
        }

        private readonly struct ItemRecord
        {
            public readonly string EnName;
            public readonly string SourcePath;

            public ItemRecord(string enName, string sourcePath)
            {
                this.EnName = enName;
                this.SourcePath = sourcePath;
            }
        }

        private readonly struct AssetRecord
        {
            public readonly string Name;
            public readonly string Path;

            public AssetRecord(string name, string path)
            {
                this.Name = name;
                this.Path = path;
            }
        }
    }
}
