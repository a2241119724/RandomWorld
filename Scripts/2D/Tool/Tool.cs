namespace LAB2D
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    /// <summary>
    /// 工具类.
    /// </summary>
    public class Tool
    {
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
            List<Vector3> vertices = new ();
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

            GameObject g = new ("Range");
            g.transform.position = parent.position + new Vector3(0, 0, -0.1f); // 使不被遮挡(对于非GUI Shader)
            MeshFilter mf = g.AddComponent<MeshFilter>();
            MeshRenderer mr = g.AddComponent<MeshRenderer>();

            // 两个世界坐标的物体成为父子物体
            // 是否保持世界坐标
            // true:相对位置不变,面板数值改变
            // false:面板不变,相对位置改变
            g.transform.SetParent(parent, true);

            Mesh mesh = new ();
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

            Mesh mesh = new ();
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
                if (t.name.Trim().Equals(name))
                {
                    return t;
                }
            }

            LogManager.Instance.Log(name + " Not Found!!!", LogManager.LogLevel.Error);
            return null;
        }

        /// <summary>
        /// 通过反射实现，从父类得到相应非抽象的子类信息.
        /// </summary>
        /// <typeparam name="T">获取子类中的类型, 不能使用接口</typeparam>
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
        /// 点击射线检测UI对象.
        /// </summary>
        /// <param name="tag">在哪个canvas下发射射线</param>
        /// <returns>UI对象</returns>
        public static List<RaycastResult> GetUIByMousePos(string tag = TagConstant.UI_TAG)
        {
            PointerEventData pointerEventData = new (EventSystem.current);
            pointerEventData.position = Input.mousePosition;
            List<RaycastResult> results = new ();
            GameObject.FindGameObjectWithTag(tag).GetComponent<GraphicRaycaster>().Raycast(pointerEventData, results);
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
                return PhotonNetwork.Instantiate(ResourceManager.Instance.GetPath(prefab.name + ".prefab"), position, rotation);
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
            List<T> itemTypes = new ();
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
        /// 向BaseType获取静态属性, 直到BaseType为null
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="propertyName">属性名称</param>
        /// <returns>属性信息</returns>
        public static PropertyInfo GetStaticPropertyByType(Type type, string propertyName)
        {
            while (type != null && type != typeof(object))
            {
                PropertyInfo prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
                if (prop != null)
                {
                    return prop;
                }

                type = type.BaseType;
            }

            return null;
        }

        /// <summary>
        /// 向BaseType获取方法, 直到BaseType为null
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="propertyName">属性名称</param>
        /// <returns>属性信息</returns>
        public static MethodInfo GetMethodByType(Type type, string propertyName)
        {
            while (type != null && type != typeof(object))
            {
                MethodInfo method = type.GetMethod(propertyName);
                if (method != null)
                {
                    return method;
                }

                type = type.BaseType;
            }

            return null;
        }
    }
}
