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
        /// ������
        /// </summary>
        /// <param name="center">����Բ��</param>
        /// <param name="angle">��ǰ��(Vector3.up)���νǶ�</param>
        /// <param name="radius">���ΰ뾶</param>
        /// <param name="color">������ɫ</param>
        /// <param name="parent">�󶨵ĸ�Ԫ��</param>
        public static void DrawSectorSolid(float angle, float radius, Color32 color, Transform parent)
        {
            int pointAmount = 100; // ��angleƽ����Ϊ
            List<Vector3> vertices = new List<Vector3>();
            vertices.Add(Vector3.zero);
            for (int i = 0; i <= pointAmount; i++)
            {
                // Vector3.upͨ����Ԫ����ת(x��,y��,z��)�õ�������
                Vector3 pos = Quaternion.Euler(0f, 0f, angle / 2 - angle / pointAmount * i) * Vector3.up * radius;
                vertices.Add(pos);
            }
            int[] triangles = new int[3 * pointAmount];
            // ���������εĸ���,��������������εĶ���˳��(����)   
            // ˳ʱ�������ʱ��      
            for (int i = 0; i < pointAmount; i++)
            {
                triangles[3 * i] = 0; // ������һ���Ϊ���ĵ�      
                triangles[3 * i + 1] = i + 1;
                triangles[3 * i + 2] = i + 2;
            }

            GameObject g = new GameObject("Range");
            g.transform.position = parent.position + new Vector3(0, 0, -0.1f); // ʹ�����ڵ�(���ڷ�GUI Shader)
            MeshFilter mf = g.AddComponent<MeshFilter>();
            MeshRenderer mr = g.AddComponent<MeshRenderer>();
            // ������������������Ϊ��������
            // �Ƿ񱣳���������
            // true:���λ�ò���,�����ֵ�ı�
            // false:��岻��,���λ�øı�
            g.transform.SetParent(parent, true);

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles;

            mf.mesh = mesh;
            //mr.material.shader = Shader.Find("Unlit/Color");
            mr.material.shader = Shader.Find("GUI/Text Shader");
            mr.material.color = color;
            // ����GUI Shader,���ò㼶,���ⱻ�ڵ�
            mr.sortingLayerName = "Enemy";
            mr.sortingOrder = 0;
        }

        public static void DrawGrid(float cellSize, Color32 color, Transform parent)
        {
            int width = 10;
            int height = 10;
            // ��ʼ����������
            Vector3[] vertices = new Vector3[((width + 1) * 2 + (height + 1) * 2) * (height + width) / 2];
            // ��ʼ���������飨���ڶ���������
            int[] indices = new int[2 * ((width + 1) * height + width * (height + 1))];
            int vertexIndex = 0;
            int indexIndex = 0;
            // ����ˮƽ������
            for (int y = 0; y <= height; y++)
            {
                for (int x = 0; x <= width; x++)
                {
                    vertices[vertexIndex++] = new Vector3(x * cellSize, y * cellSize, 0);
                    // ��ֱ��
                    if (y < height)
                    {
                        indices[indexIndex] = (y * (width + 1)) + x;
                        indices[indexIndex + 1] = ((y + 1) * (width + 1)) + x;
                        indexIndex += 2;
                    }
                    // ˮƽ��
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
            // ����GUI Shader,���ò㼶,���ⱻ�ڵ�
            meshRenderer.sortingLayerName = "Enemy";
            meshRenderer.sortingOrder = 0;
        }

        /// <summary>
        /// �ں������ҵ���������
        /// </summary>
        /// <typeparam name="T">�������</typeparam>
        /// <param name="parent">�����</param>
        /// <param name="name">�������</param>
        /// <returns>���ֶ�Ӧ���ϵ����</returns>
        public static T GetOrAddComponentInChildren<T>(GameObject parent, string name) where T : Component
        {
            // �ҵ�����parent�����Transform���
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
        /// �ں������ҵ����
        /// </summary>
        /// <typeparam name="T">�������</typeparam>
        /// <param name="parent">������</param>
        /// <param name="name">�������</param>
        /// <returns>���ֶ�Ӧ���ϵ����</returns>
        public static T GetComponentInChildren<T>(GameObject parent, string name) where T : Component
        {
            // �ҵ�����parent�����Transform���
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
        /// ���������л��ͷ����л����
        /// </summary>
        /// <typeparam name="T">��������</typeparam>
        /// <param name="obj">����Դ����</param>
        /// <returns>�����õ�����</returns>
        public static T DeepCopyByBinary<T>(T obj)
        {
            object retval;
            // �Զ��ͷ���Դ
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
        /// xml���л��ͷ����л����
        /// </summary>
        /// <typeparam name="T">��������</typeparam>
        /// <param name="obj">����Դ����</param>
        /// <returns>�����õ�����</returns>
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
        /// ��ȡ�ļ��µ�Ԥ���岢��װ��Dictionary
        /// </summary>
        /// <param name="folderPath">Resources�µ��ļ���·��</param>
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
        /// resource�µ������ļ�·��
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
        /// ���ض������ļ�
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
        /// ����Ϊ�������ļ�
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
        /// ����Json����
        /// </summary>
        /// <returns>�Ƿ�������</returns>
        public static T loadDataByJson<T>(string filePath) where T : class
        {
            if (!File.Exists(filePath))
            {
                Debug.Log(filePath + "������");
                return null;
            }
            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<T>(json);
        }

        /// <summary>
        /// ����Json����
        /// </summary>
        public static void saveDataByJson<T>(string filePath, T data) where T : class
        {
            // JsonUtility�޷����л�Dictionary
            string json = JsonUtility.ToJson(data);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// ��ȡCSV�ļ����ļ���ҪʱUTF-8
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
        /// ͨ������ʵ�֣��Ӹ���õ���Ӧ�ǳ����������Ϣ
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
