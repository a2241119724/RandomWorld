namespace LAB.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using LAB.Attributes;
    using UnityEditor;

    public class BuilderGenerator : AssetPostprocessor
    {
        // 每个文件每次只执行一次
        private static readonly Dictionary<string, string> IsOne = new ();
        private static readonly Dictionary<Type, string> Types = new Dictionary<Type, string>()
        {
            { typeof(int), "int" },
            { typeof(string), "string" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(char), "char" },
            { typeof(decimal), "decimal" },
            { typeof(long), "long" },
            { typeof(short), "short" },
            { typeof(uint), "uint" },
            { typeof(ulong), "ulong" },
            { typeof(ushort), "ushort" },
            { typeof(sbyte), "sbyte" },
        };

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var asset in importedAssets)
            {
                if (!asset.EndsWith(".cs") || IsOne.ContainsKey(asset))
                {
                    continue;
                }

                var className = Path.GetFileNameWithoutExtension(asset);
                var types = Assembly.Load("Assembly-CSharp").GetTypes();
                var type = types.FirstOrDefault(t => t.Name == className && t.GetCustomAttribute<BuilderAttribute>() != null);
                if (type != null)
                {
                    IsOne.Add(asset, className);
                    GenerateBuilder(type);
                }
            }
        }

        private static void GenerateBuilder(System.Type type)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            string assetPath = AssetDatabase.FindAssets($"t:Script {type.Name}")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .FirstOrDefault(path => Path.GetFileNameWithoutExtension(path) == type.Name);
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            var originalContent = File.ReadAllText(assetPath);

            // 先删除已存在的Builder类
            string prefix = "\t\t";
            string builderStartString = "\n        public class Builder";
            var builderStart = originalContent.IndexOf(builderStartString);
            var builderEnd = originalContent.LastIndexOf("}", originalContent.LastIndexOf("}") - 1) - 1;
            if (builderStart != -1)
            {
                originalContent = originalContent.Remove(builderStart, builderEnd - builderStart);
                builderEnd = originalContent.LastIndexOf("}", originalContent.LastIndexOf("}") - 1) - 1;
            }

            // 生成新的Builder代码
            var builderCode = new StringBuilder();
            builderCode.AppendLine(builderStartString);
            builderCode.AppendLine(prefix + "{");
            builderCode.AppendLine(prefix + $"\tprivate readonly {type.Name} instance = new ();");

            foreach (var prop in properties)
            {
                builderCode.AppendLine(string.Empty);
                builderCode.AppendLine(prefix + $"\tpublic Builder With{prop.Name}({Types[prop.PropertyType]} value)");
                builderCode.AppendLine(prefix + "\t{");
                builderCode.AppendLine(prefix + $"\t\tthis.instance.{prop.Name} = value;");
                builderCode.AppendLine(prefix + "\t\treturn this;");
                builderCode.AppendLine(prefix + "\t}");
            }

            builderCode.AppendLine(string.Empty);
            builderCode.AppendLine(prefix + $"\tpublic {type.Name} Build()");
            builderCode.AppendLine(prefix + "\t{");
            builderCode.AppendLine(prefix + "\t\treturn this.instance;");
            builderCode.AppendLine(prefix + "\t}");
            builderCode.AppendLine(prefix + "}");

            // 插入新生成的Builder类
            var newContent = originalContent.Insert(builderEnd, builderCode.ToString());
            File.WriteAllText(assetPath, newContent);
            AssetDatabase.Refresh();
        }
    }
}
