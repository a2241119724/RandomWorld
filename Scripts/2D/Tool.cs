namespace LAB2D
{
    using Photon.Pun;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Xml.Serialization;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    /// <summary>
    /// 工具类.
    /// </summary>
    public class Tool
    {
        private static BinaryFormatter bf = new BinaryFormatter();
        private static string des = Application.persistentDataPath + "/Data/";
        private static string dataPath = new DirectoryInfo(Application.dataPath).FullName + "\\Resources\\";

        /// <summary>
        /// 画扇形.
        /// </summary>
        /// <param name="angle">正前方(Vector3.up)扇形角度.</param>
        /// <param name="radius">扇形半径.</param>
        /// <param name="color">扇形颜色.</param>
        /// <param name="parent">绑定的父元素.</param>
        public static void DrawSectorSolid(float angle, float radius, Color32 color, Transform parent)
        {
            int pointAmount = 100; // 将angle平均分为
            List<Vector3> vertices = new List<Vector3>();
            vertices.Add(Vector3.zero);
            for (int i = 0; i <= pointAmount; i++)
            {
                // Vector3.up通过四元数旋转(x轴,y轴,z轴)得到的向量
                Vector3 pos = Quaternion.Euler(0f, 0f, (angle / 2) - (angle / pointAmount * i)) * Vector3.up * radius;
                vertices.Add(pos);
            }

            int[] triangles = new int[3 * pointAmount];

            // 根据三角形的个数,来计算绘制三角形的顶点顺序(索引)
            // 顺时针或者逆时针
            for (int i = 0; i < pointAmount; i++)
            {
                triangles[3 * i] = 0; // 三角形一点均为中心点
                triangles[(3 * i) + 1] = i + 1;
                triangles[(3 * i) + 2] = i + 2;
            }

            GameObject g = new GameObject("Range");
            g.transform.position = parent.position + new Vector3(0, 0, -0.1f); // 使不被遮挡(对于非GUI Shader)
            MeshFilter mf = g.AddComponent<MeshFilter>();
            MeshRenderer mr = g.AddComponent<MeshRenderer>();

            // 两个世界坐标的物体成为父子物体
            // 是否保持世界坐标
            // true:相对位置不变,面板数值改变
            // false:面板不变,相对位置改变
            g.transform.SetParent(parent, true);

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles;

            // mr.material.shader = Shader.Find("Unlit/Color");
            mf.mesh = mesh;
            mr.material.shader = Shader.Find("GUI/Text Shader");
            mr.material.color = color;

            // 对于GUI Shader,设置层级,避免被遮挡
            mr.sortingLayerName = "Enemy";
            mr.sortingOrder = 0;
        }

        /// <summary>
        /// 画网格.
        /// </summary>
        /// <param name="cellSize">每个网格的大小.</param>
        /// <param name="color">网格的颜色.</param>
        /// <param name="parent">绑定的父元素.</param>
        public static void DrawGrid(float cellSize, Color32 color, Transform parent)
        {
            int width = 10;
            int height = 10;

            // 初始化顶点数组
            Vector3[] vertices = new Vector3[(((width + 1) * 2) + ((height + 1) * 2)) * (height + width) / 2];

            // 初始化索引数组（用于定义线条）
            int[] indices = new int[2 * (((width + 1) * height) + (width * (height + 1)))];
            int vertexIndex = 0;
            int indexIndex = 0;

            // 生成水平网格线
            for (int y = 0; y <= height; y++)
            {
                for (int x = 0; x <= width; x++)
                {
                    vertices[vertexIndex++] = new Vector3(x * cellSize, y * cellSize, 0);

                    // 垂直线
                    if (y < height)
                    {
                        indices[indexIndex] = (y * (width + 1)) + x;
                        indices[indexIndex + 1] = ((y + 1) * (width + 1)) + x;
                        indexIndex += 2;
                    }

                    // 水平线
                    if ((x + 1) % (width + 1) != 0)
                    {
                        indices[indexIndex] = (y * (width + 1)) + x;
                        indices[indexIndex + 1] = (y * (width + 1)) + x + 1;
                        indexIndex += 2;
                    }
                }
            }

            MeshFilter meshFilter = parent.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = parent.GetComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            meshFilter.mesh = mesh;

            // meshRenderer.material.shader = Shader.Find("GUI/Text Shader");
            meshRenderer.material.color = color;

            // 对于GUI Shader,设置层级,避免被遮挡
            meshRenderer.sortingLayerName = "Enemy";
            meshRenderer.sortingOrder = 0;
        }

        /// <summary>
        /// 在孩子中找到或添加组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="parent">父组件</param>
        /// <param name="name">组件名字</param>
        /// <returns>名字对应身上的组件</returns>
        public static T GetOrAddComponentInChildren<T>(GameObject parent, string name)
            where T : Component
        {
            // 找到所有parent下面的Transform组件
            Transform[] ts = parent.GetComponentsInChildren<Transform>();
            foreach (var t in ts)
            {
                if (t.name == name)
                {
                    if (t.GetComponent<T>() == null)
                    {
                        t.gameObject.AddComponent<T>();
                    }

                    return t.gameObject.GetComponent<T>();
                }
            }

            return null;
        }

        /// <summary>
        /// 在孩子中找到组件.
        /// </summary>
        /// <typeparam name="T">组件类型.</typeparam>
        /// <param name="parent">父物体.</param>
        /// <param name="name">组件名字.</param>
        /// <returns>名字对应身上的组件.</returns>
        public static T GetComponentInChildren<T>(GameObject parent, string name)
            where T : Component
        {
            // 找到所有parent下面的Transform组件
            T[] ts = parent.GetComponentsInChildren<T>();
            foreach (var t in ts)
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            LogManager.Instance.Log(name + " Not Found!!!", LogManager.LogLevel.Error);
            return null;
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
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);
                retval = bf.Deserialize(ms);
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
            using (MemoryStream ms = new MemoryStream())
            {
                XmlSerializer xml = new XmlSerializer(typeof(T));
                xml.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);
                retval = xml.Deserialize(ms);
                ms.Close();
            }

            return (T)retval;
        }

        /// <summary>
        /// 获取文件下的Resources并封装成Dictionary.
        /// </summary>
        /// <param name="folderPath">Resources下的文件夹路径.</param>
        /// <typeparam name="T">Resources类型.</typeparam>
        /// <returns>所有预制体的键值对.</returns>
        public static Dictionary<string, T> LoadResources<T>(string folderPath)
            where T : UnityEngine.Object
        {
            Dictionary<string, T> map = new Dictionary<string, T>();
            T[] prefabs = Resources.LoadAll<T>(folderPath);
            foreach (T p in prefabs)
            {
                map[p.name] = p;
            }

            return map;
        }

        /// <summary>
        /// resource下的所有文件路径.
        /// </summary>
        /// <returns>所有路径键值对.</returns>
        public static Dictionary<string, string> LoadPaths()
        {
            Dictionary<string, string> map = new Dictionary<string, string>();

            // string[] subPaths = AssetDatabase.GetAllAssetPaths();
#if UNITY_EDITOR
            // 开发阶段加载path,并保存起来
            LoadPaths1(dataPath, map);
            SaveDataByBinary(Application.streamingAssetsPath + "/resourcePath.lab", map);
#else
            map = loadDataByBinary<Dictionary<string, string>>(Application.streamingAssetsPath + "/resourcePath.lab");
#endif
            return map;
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

            using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                return (T)bf.Deserialize(fs);
            }
        }

        /// <summary>
        /// 保存为二进制文件.
        /// </summary>
        /// <typeparam name="T">加载数据的结构.</typeparam>
        /// <param name="filePath">路径.</param>
        /// <param name="data">数据内容.</param>
        public static void SaveDataByBinary<T>(string filePath, T data)
            where T : class
        {
            using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                bf.Serialize(fs, data);
                fs.Flush();
                fs.Close();
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
                LogManager.Instance.Log(filePath + "不存在", LogManager.LogLevel.Error);
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
            // JsonUtility无法序列化Dictionary
            string json = JsonUtility.ToJson(data);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 读取CSV文件，文件需要时UTF-8.
        /// </summary>
        /// <param name="name">文件名称.</param>
        /// <returns>文件内容.</returns>
        public static string[] GetCSV(string name)
        {
            string data;
            if (File.Exists(des + name + ".csv"))
            {
                data = File.ReadAllText(des + name + ".csv");
            }
            else
            {
                // Encoding.UTF8.GetString();
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
        /// 通过反射实现，从父类得到相应非抽象的子类信息.
        /// </summary>
        /// <typeparam name="T">获取子类中的类型.</typeparam>
        /// <returns>类型列表.</returns>
        public static List<Type> GetChildByParent<T>()
        {
            Type baseType = typeof(T);
            Assembly assembly = Assembly.GetExecutingAssembly();

            var derivedTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(baseType) && !t.IsAbstract)
                .ToList();
            return derivedTypes;
        }

        /// <summary>
        /// 向量加(x,y).
        /// </summary>
        /// <param name="vector">向量.</param>
        /// <param name="x">横坐标.</param>
        /// <param name="y">纵坐标.</param>
        /// <returns>结果.</returns>
        public static Vector3Int Add(Vector3Int vector, int x, int y)
        {
            return new Vector3Int(vector.x + x, vector.y + y, vector.z);
        }

        /// <summary>
        /// 向量转置.
        /// </summary>
        /// <param name="vector">向量.</param>
        /// <returns>转置结果.</returns>
        public static Vector3Int T(Vector3Int vector)
        {
            return new Vector3Int(vector.y, vector.x, vector.z);
        }

        /// <summary>
        /// 点击射线检测UI对象.
        /// </summary>
        /// <returns>UI对象.</returns>
        public static List<RaycastResult> GetUIByMousePos()
        {
            PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
            pointerEventData.position = Input.mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            GameObject.FindGameObjectWithTag(ResourceConstant.UI_TAG).GetComponent<GraphicRaycaster>().Raycast(pointerEventData, results);
            return results;
        }

        /// <summary>
        /// 实例化预制体.
        /// </summary>
        /// <param name="prefab">数据.</param>
        /// <param name="position">实例化位置.</param>
        /// <param name="rotation">实例化角度.</param>
        /// <returns>对象.</returns>
        public static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (NetworkConnect.Instance.IsOnline)
            {
                return PhotonNetwork.Instantiate(ResourcesManager.Instance.GetPath(prefab.name + ".prefab"), position, rotation);
            }
            else
            {
                return (GameObject)GameObject.Instantiate(prefab, position, rotation);
            }
        }

        /// <summary>
        /// 由于多个Enum放到一个中,需要Split.
        /// </summary>
        /// <typeparam name="T">Split枚举的类型.</typeparam>
        /// <param name="start">起始枚举.</param>
        /// <param name="end">终止枚举.</param>
        /// <returns>枚举列表.</returns>
        public static List<T> SplitEnum<T>(T start, T end)
            where T : Enum
        {
            List<T> itemTypes = new List<T>();
            IEnumerator enumerator = Enum.GetValues(typeof(T)).GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (((T)enumerator.Current).Equals(start))
                {
                    break;
                }
            }

            do
            {
                T type = (T)enumerator.Current;
                itemTypes.Add(type);
                if (type.Equals(end))
                {
                    break;
                }
            }
            while (enumerator.MoveNext());
            return itemTypes;
        }

        /// <summary>
        /// 加载场景.
        /// </summary>
        /// <param name="sceneName">场景名称.</param>
        public static void LoadScene(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.buildIndex == -1)
            {
                LogManager.Instance.Log(sceneName + " Not Found!!!", LogManager.LogLevel.Error);
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// 递归获取路径.
        /// </summary>
        /// <param name="path">路径.</param>
        /// <param name="map">out.</param>
        private static void LoadPaths1(string path, Dictionary<string, string> map)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            FileInfo[] fileInfos = directoryInfo.GetFiles();
            foreach (FileInfo fileInfo in fileInfos)
            {
                string[] splits = fileInfo.Name.Split(".");
                if (!splits[splits.Length - 1].Equals("meta"))
                {
                    map[fileInfo.Name] = path.Split(dataPath)[1].Replace("\\", "/").Split('.')[0] + "/" + fileInfo.Name.Split('.')[0];
                }
            }

            DirectoryInfo[] subDirectoryInfos = directoryInfo.GetDirectories();
            foreach (DirectoryInfo subDirectoryInfo in subDirectoryInfos)
            {
                LoadPaths1(subDirectoryInfo.FullName, map);
            }
        }
    }
}
