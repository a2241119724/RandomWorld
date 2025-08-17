namespace LAB2D
{
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;

    /// <summary>
    /// 资源工具类
    /// </summary>
    public class ResourceTool
    {
        private static readonly string ResourcePath = Application.streamingAssetsPath + "\\Resources\\";

        /// <summary>
        /// 获取文件下的Resources并封装成Dictionary.
        /// </summary>
        /// <param name="folderPath">Resources下的文件夹路径.</param>
        /// <typeparam name="T">Resources类型.</typeparam>
        /// <returns>所有预制体的键值对.</returns>
        public static Dictionary<string, T> LoadResources<T>(string folderPath)
            where T : UnityEngine.Object
        {
            Dictionary<string, T> map = new ();
            T[] prefabs = Resources.LoadAll<T>(folderPath);
            foreach (T p in prefabs)
            {
                map[p.name.Split("/")[^1]] = p;
            }

            return map;
        }

        /// <summary>
        /// 递归获取路径.
        /// </summary>
        /// <param name="path">路径.</param>
        /// <param name="map">out.</param>
        private static void DoLoadPaths(string path, Dictionary<string, string> map)
        {
            DirectoryInfo directoryInfo = new (path);
            FileInfo[] fileInfos = directoryInfo.GetFiles();
            foreach (FileInfo fileInfo in fileInfos)
            {
                string[] splits = fileInfo.Name.Split(".");
                if (!splits[^1].Equals("meta"))
                {
                    map[fileInfo.Name] = path.Split(ResourcePath)[1].Replace("\\", "/").Split('.')[0] + "/" + fileInfo.Name.Split('.')[0];
                }
            }

            DirectoryInfo[] subDirectoryInfos = directoryInfo.GetDirectories();
            foreach (DirectoryInfo subDirectoryInfo in subDirectoryInfos)
            {
                DoLoadPaths(subDirectoryInfo.FullName, map);
            }
        }
    }
}
