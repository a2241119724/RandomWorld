namespace LAB2D.Tool
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Xml.Serialization;
    using UnityEngine;

    /// <summary>
    /// 数据处理工具
    /// </summary>
    public class DataTool
    {
        private static readonly BinaryFormatter Bf = new ();

        /// <summary>
        /// 保存为二进制文件.
        /// </summary>
        /// <typeparam name="T">加载数据的结构.</typeparam>
        /// <param name="filePath">路径.</param>
        /// <param name="data">数据内容.</param>
        public static void SaveDataByBinary<T>(string filePath, T data)
            where T : class
        {
            CreateDirectoryIfNeed(filePath);
            using FileStream fs = new (filePath, FileMode.OpenOrCreate, FileAccess.Write);
            Bf.Serialize(fs, data);
            fs.Flush();
            fs.Close();
        }

        /// <summary>
        /// 加载二进制文件.
        /// </summary>
        /// <typeparam name="T">加载数据的结构.</typeparam>
        /// <param name="filePath">文件路径.</param>
        /// <returns>文件内容.</returns>
        /// <returns>对应的数据类.</returns>
        public static T LoadDataByBinary<T>(string filePath)
            where T : class
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                using FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read);
                return (T)Bf.Deserialize(fs);
            }
            catch
            {
                // 反序列化失败（如类型结构变更），删除旧文件并返回 null
                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                }

                return null;
            }
        }

        /// <summary>
        /// 加载Json数据
        /// </summary>
        /// <typeparam name="T">加载数据的结构.</typeparam>
        /// <param name="filePath">路径.</param>
        /// <returns>是否有数据</returns>
        public static T LoadDataByJson<T>(string filePath)
            where T : class
        {
            if (!File.Exists(filePath))
            {
                AWorkerTask.LogProvider(filePath + "不存在", LogManager.LogLevelEnum.Error);
                return null;
            }

            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<T>(json);
        }

        /// <summary>
        /// 保存Json数据.
        /// </summary>
        /// <typeparam name="T">加载数据的结构.</typeparam>
        /// <param name="filePath">路径.</param>
        /// <param name="data">数据内容.</param>
        public static void SaveDataByJson<T>(string filePath, T data)
            where T : class
        {
            CreateDirectoryIfNeed(filePath);
            // JsonUtility无法序列化Dictionary
            string json = JsonUtility.ToJson(data);
            File.WriteAllText(filePath, json);
        }

        private static void CreateDirectoryIfNeed(string filePath)
        {
            string directoryPath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directoryPath))
            {
                return;
            }

            Directory.CreateDirectory(directoryPath);
        }

        /// <summary>
        /// 读取CSV文件，文件需要时UTF-8.
        /// </summary>
        /// <param name="name">文件名称.</param>
        /// <returns>文件内容.</returns>
        public static string[] LoadCSV(string name)
        {
            string data = $"{Application.persistentDataPath}/{name}.csv";
            if (File.Exists(data))
            {
                data = File.ReadAllText(data);
            }
            else
            {
                // 使用 UTF8 编码读取字符串
                TextAsset textAsset = Resources.Load<TextAsset>(name);
                if (textAsset == null)
                {
                    return null;
                }

                data = textAsset.text;
            }

            return data.TrimEnd('\r', '\n').Split("\r\n");
        }

        /// <summary>
        /// 将数据序列化为字节数组
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="data">数据</param>
        /// <returns>字节数组</returns>
        public static byte[] ToByteArray<T>(T data)
        {
            using MemoryStream stream = new ();
            Bf.Serialize(stream, data);
            return stream.ToArray();
        }

        /// <summary>
        /// 将数据序列化为字节数组
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="data">数据</param>
        /// <returns>字节数组</returns>
        public static T FromByteArray<T>(byte[] data)
        {
            using MemoryStream stream = new (data);
            return (T)Bf.Deserialize(stream);
        }

        /// <summary>
        /// 二进制序列化和反序列化深拷贝.
        /// </summary>
        /// <typeparam name="T">拷贝类型.</typeparam>
        /// <param name="obj">拷贝源对象.</param>
        /// <returns>拷贝得到对象.</returns>
        public static T DeepCopyByBinary<T>(T obj)
        {
            object retval;

            // 自动释放资源
            using (MemoryStream ms = new ())
            {
                Bf.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);
                retval = Bf.Deserialize(ms);
                ms.Close();
            }

            return (T)retval;
        }

        /// <summary>
        /// xml序列化和反序列化深拷贝.
        /// </summary>
        /// <typeparam name="T">拷贝类型.</typeparam>
        /// <param name="obj">拷贝源对象.</param>
        /// <returns>拷贝得到对象.</returns>
        public static T DeepCopyByXml<T>(T obj)
        {
            object retval;
            using (MemoryStream ms = new ())
            {
                XmlSerializer xml = new (typeof(T));
                xml.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);
                retval = xml.Deserialize(ms);
                ms.Close();
            }

            return (T)retval;
        }
    }
}
