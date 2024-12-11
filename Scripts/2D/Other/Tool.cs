using Photon.Pun;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

namespace LAB2D
{
    public class Tool
    {
        private static BinaryFormatter bf = new BinaryFormatter();

        /// <summary>
        /// 画扇形
        /// </summary>
        /// <param name="center">扇形圆心</param>
        /// <param name="angle">正前方(Vector3.up)扇形角度</param>
        /// <param name="radius">扇形半径</param>
        /// <param name="color">扇形颜色</param>
        /// <param name="parent">绑定的父元素</param>
        public static void DrawSectorSolid(float angle, float radius, Color32 color, Transform parent)
        {
            int pointAmount = 100; // 将angle平均分为
            List<Vector3> vertices = new List<Vector3>();
            vertices.Add(Vector3.zero);
            for (int i = 0; i <= pointAmount; i++)
            {
                // Vector3.up通过四元数旋转(x轴,y轴,z轴)得到的向量
                Vector3 pos = Quaternion.Euler(0f, 0f, angle / 2 - angle / pointAmount * i) * Vector3.up * radius;
                vertices.Add(pos);
            }
            int[] triangles = new int[3 * pointAmount];
            // 根据三角形的个数,来计算绘制三角形的顶点顺序(索引)   
            // 顺时针或者逆时针      
            for (int i = 0; i < pointAmount; i++)
            {
                triangles[3 * i] = 0; // 三角形一点均为中心点      
                triangles[3 * i + 1] = i + 1;
                triangles[3 * i + 2] = i + 2;
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

            mf.mesh = mesh;
            //mr.material.shader = Shader.Find("Unlit/Color");
            mr.material.shader = Shader.Find("GUI/Text Shader");
            mr.material.color = color;
            // 对于GUI Shader,设置层级,避免被遮挡
            mr.sortingLayerName = "Enemy";
            mr.sortingOrder = 0;
        }

        public static void DrawGrid(float cellSize, Color32 color, Transform parent)
        {
            int width = 10;
            int height = 10;
            // 初始化顶点数组
            Vector3[] vertices = new Vector3[((width + 1) * 2 + (height + 1) * 2) * (height + width) / 2];
            // 初始化索引数组（用于定义线条）
            int[] indices = new int[2 * ((width + 1) * height + width * (height + 1))];
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
            //
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            meshFilter.mesh = mesh;

            //meshRenderer.material.shader = Shader.Find("GUI/Text Shader");
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
        public static T GetOrAddComponentInChildren<T>(GameObject parent, string name) where T : Component
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
        /// 在孩子中找到组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="parent">父物体</param>
        /// <param name="name">组件名字</param>
        /// <returns>名字对应身上的组件</returns>
        public static T GetComponentInChildren<T>(GameObject parent, string name) where T : Component
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
            Debug.Log(name + " Not Found!!!");
            return null;
        }

        /// <summary>
        /// 二进制序列化和反序列化深拷贝
        /// </summary>
        /// <typeparam name="T">拷贝类型</typeparam>
        /// <param name="obj">拷贝源对象</param>
        /// <returns>拷贝得到对象</returns>
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
        /// xml序列化和反序列化深拷贝
        /// </summary>
        /// <typeparam name="T">拷贝类型</typeparam>
        /// <param name="obj">拷贝源对象</param>
        /// <returns>拷贝得到对象</returns>
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
        /// 获取文件下的预制体并封装成Dictionary
        /// </summary>
        /// <param name="folderPath">Resources下的文件夹路径</param>
        /// <returns></returns>
        public static Dictionary<string, T> loadResources<T>(string folderPath) where T : UnityEngine.Object
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
        /// resource下的所有文件路径
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> loadPaths()
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            string[] subPaths = AssetDatabase.GetAllAssetPaths();
            foreach (string subPath in subPaths)
            {
                if (subPath.StartsWith("Assets/Resources/") && File.Exists(subPath))
                {
                    map[Path.GetFileName(subPath)] = subPath.Split('.')[0].Remove(0,17);
                }
            }
            return map;
        }

        public static void master(Action action)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                action();
            }
        }

        public static void slave(Action action)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                action();
            }
        }

        /// <summary>
        /// 加载二进制文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static T loadDataByBinary<T>(string filePath) where T : class
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
        /// 保存为二进制文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="data"></param>
        public static void saveDataByBinary<T>(string filePath, T data) where T : class
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
        /// <returns>是否有数据</returns>
        public static T loadDataByJson<T>(string filePath) where T : class
        {
            if (!File.Exists(filePath))
            {
                Debug.Log(filePath + "不存在");
                return null;
            }
            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<T>(json);
        }

        /// <summary>
        /// 保存Json数据
        /// </summary>
        public static void saveDataByJson<T>(string filePath, T data) where T : class
        {
            // JsonUtility无法序列化Dictionary
            string json = JsonUtility.ToJson(data);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 读取CSV文件，文件需要时UTF-8
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string[] getCSV(string name)
        {
            string des = Application.persistentDataPath + "/Data/";
            string data;
            if (File.Exists(des + name + ".csv"))
            {
                data = File.ReadAllText(des + name + ".csv");
            }
            else
            {
                //Encoding.UTF8.GetString();
                data = Resources.Load<TextAsset>(name).text;
            }
            return data.TrimEnd('\r', '\n').Split("\r\n");
        }

        /// <summary>
        /// 通过反射实现，从父类得到相应非抽象的子类信息
        /// </summary>
        public static List<Type> getChildByParen<T>() {
            Type baseType = typeof(T);
            Assembly assembly = Assembly.GetExecutingAssembly();

            var derivedTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(baseType) && !t.IsAbstract)
                .ToList();
            return derivedTypes;
        }
    }
}
