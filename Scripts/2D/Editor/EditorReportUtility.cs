namespace LAB2D.Editor
{
    using LAB2D;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    internal static class EditorReportUtility
    {
        public static string CreateUniqueTaskDirectory(string reportRoot, string taskDirectoryName, DateTime now)
        {
            string dateDirectory = NormalizePath(Path.Combine(reportRoot, now.ToString("yyyy-MM-dd")));
            if (!Directory.Exists(dateDirectory))
            {
                Directory.CreateDirectory(dateDirectory);
            }

            string desiredDirectory = NormalizePath(Path.Combine(dateDirectory, taskDirectoryName));
            if (!Directory.Exists(desiredDirectory))
            {
                Directory.CreateDirectory(desiredDirectory);
                return desiredDirectory;
            }

            string timestampDirectory = NormalizePath(Path.Combine(dateDirectory, taskDirectoryName + "_" + now.ToString("HHmmss")));
            if (!Directory.Exists(timestampDirectory))
            {
                Directory.CreateDirectory(timestampDirectory);
                return timestampDirectory;
            }

            int index = 2;
            while (true)
            {
                string indexedDirectory = NormalizePath(timestampDirectory + "_" + index);
                if (!Directory.Exists(indexedDirectory))
                {
                    Directory.CreateDirectory(indexedDirectory);
                    return indexedDirectory;
                }

                index++;
            }
        }

        public static List<string> CollectMissingMetaFiles(params string[] roots)
        {
            List<string> missing = new List<string>();
            foreach (string root in roots)
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                string rootMetaPath = root + ".meta";
                if (!File.Exists(rootMetaPath))
                {
                    missing.Add(NormalizePath(rootMetaPath));
                }

                foreach (string directory in Directory.GetDirectories(root, "*", SearchOption.AllDirectories))
                {
                    string metaPath = directory + ".meta";
                    if (!File.Exists(metaPath))
                    {
                        missing.Add(NormalizePath(metaPath));
                    }
                }

                foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    if (Path.GetExtension(file).Equals(".meta", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string metaPath = file + ".meta";
                    if (!File.Exists(metaPath))
                    {
                        missing.Add(NormalizePath(metaPath));
                    }
                }
            }

            return missing.OrderBy(path => path, StringComparer.Ordinal).ToList();
        }

        public static int CountMissingMeta(string root)
        {
            return CollectMissingMetaFiles(root).Count;
        }

        public static void AppendStringList(
            StringBuilder sb,
            string title,
            IReadOnlyCollection<string> values,
            string emptyText,
            bool wrapValuesInCode = false)
        {
            sb.AppendLine("## " + title);
            sb.AppendLine();
            if (values.Count == 0)
            {
                sb.AppendLine("- " + emptyText);
            }
            else
            {
                foreach (string value in values)
                {
                    sb.AppendLine("- " + (wrapValuesInCode ? "`" + value + "`" : value));
                }
            }

            sb.AppendLine();
        }

        public static string EscapeTable(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }

        public static string NormalizePath(string path)
        {
            return path.Replace("\\", "/");
        }
    }
}
