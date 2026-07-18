using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun
{

    public static class NestedComponentUtilities
    {

        public static T EnsureRootComponentExists<T, NestedT>(this Transform transform)
            where T : Component
            where NestedT : Component
        {
            var root = GetParentComponent<NestedT>(transform);
            if (root)
            {
                var comp = root.GetComponent<T>();

                if (comp)
                    return comp;

                return root.gameObject.AddComponent<T>();
            }

            return null;
        }

        #region GetComponent Replacements

        // 回收利用的集合
        private static Queue<Transform> nodesQueue = new Queue<Transform>();
        public static Dictionary<System.Type, ICollection> searchLists = new Dictionary<System.Type, ICollection>();
        private static Stack<Transform> nodeStack = new Stack<Transform>();

        /// <summary>
        /// 在提供的transform或其任何父节点上查找T。与GetComponentInParent不同，GameObject不需要处于活动状态即可被找到。
        /// </summary>
        public static T GetParentComponent<T>(this Transform t)
            where T : Component
        {
            T found = t.GetComponent<T>();

            if (found)
                return found;

            var par = t.parent;
            while (par)
            {
                found = par.GetComponent<T>();
                if (found)
                    return found;
                par = par.parent;
            }
            return null;
        }


        /// <summary>
        /// 返回子transform与其根节点之间找到的所有T。List中的顺序是从子到父，根/父节点排在最后。
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static void GetNestedComponentsInParents<T>(this Transform t, List<T> list)
            where T : Component
        {
            list.Clear();

            while (t != null)
            {
                T obj = t.GetComponent<T>();
                if (obj)
                    list.Add(obj);

                t = t.parent;
            }
        }

        public static T GetNestedComponentInChildren<T, NestedT>(this Transform t, bool includeInactive)
            where T : class
            where NestedT : class
        {
            // 首先在根节点上查找最明显的检查。
            var found = t.GetComponent<T>();
            if (!ReferenceEquals(found, null))
                return found;

            // 未在根节点找到，开始逐层测试 - 根是第一层。添加到队列。
            nodesQueue.Clear();
            nodesQueue.Enqueue(t);

            while (nodesQueue.Count > 0)
            {
                var node = nodesQueue.Dequeue();

                for (int c = 0, ccnt = node.childCount; c < ccnt; ++c)
                {
                    var child = node.GetChild(c);

                    // 忽略非活动状态的分支
                    if (!includeInactive && !child.gameObject.activeSelf)
                        continue;

                    // 遇到嵌套节点 - 不搜索此节点
                    if (!ReferenceEquals(child.GetComponent<NestedT>(), null))
                        continue;

                    // 查看我们要找的内容是否在此节点上
                    found = child.GetComponent<T>();

                    // 如果找到了我们要找的内容，返回
                    if (!ReferenceEquals(found, null))
                        return found;

                    // 将节点添加到队列中以进行下一轮深度搜索，因为在此层未找到任何内容。
                    nodesQueue.Enqueue(child);
                }

            }
            return found;
        }

        /// <summary>
        /// 与GetComponentInParent相同，但始终在搜索中包含非活动对象。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="DontRecurseOnT"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static T GetNestedComponentInParent<T, NestedT>(this Transform t)
            where T : class
            where NestedT : class
        {
            T found = null;

            Transform node = t;
            do
            {

                found = node.GetComponent<T>();

                if (!ReferenceEquals(found, null))
                    return found;

                // 在带有PV的节点上停止搜索
                if (!ReferenceEquals(node.GetComponent<NestedT>(), null))
                    return null;

                node = node.parent;
            }
            while (!ReferenceEquals(node, null));

            return null;
        }

        /// <summary>
        /// 未经测试
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="StopSearchOnT"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static T GetNestedComponentInParents<T, NestedT>(this Transform t)
            where T : class
            where NestedT : class
        {
            // 首先尝试根节点
            var found = t.GetComponent<T>();

            if (!ReferenceEquals(found, null))
                return found;

            /// 获取从起始节点向上到网络对象的transform反向列表
            var par = t.parent;

            while (!ReferenceEquals(par, null))
            {
                found = par.GetComponent<T>();
                if (!ReferenceEquals(found, null))
                    return found;

                /// 在NetObj处停止向上遍历（这是我们检测嵌套的方式）
                if (!ReferenceEquals(par.GetComponent<NestedT>(), null))
                    return null;

                par = par.parent;
            };

            return null;
        }


        /// <summary>
        /// 在提供的transform及其上方的每个父节点上查找类型T的组件，包含起始节点，并在具有StopSearchOnT组件的节点处停止。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="StopSearchOnT"></typeparam>
        /// <param name="t"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static void GetNestedComponentsInParents<T, NestedT>(this Transform t, List<T> list)
            where T : class
            where NestedT : class
        {

            // 获取起始节点上的组件 - 这是给定的。
            t.GetComponents(list);

            // 如果起始节点有停止搜索组件，我们就完成了。
            if (!ReferenceEquals(t.GetComponent<NestedT>(), null))
                return;

            var tnode = t.parent;

            // 如果没有父节点，我们就完成了。
            if (ReferenceEquals(tnode, null))
                return;

            nodeStack.Clear();

            while (true)
            {
                // 将新的父节点添加到栈中
                nodeStack.Push(tnode);

                // 如果此节点有停止搜索组件，我们就完成了向上递归。
                if (!ReferenceEquals(tnode.GetComponent<NestedT>(), null))
                    break;

                // 获取下一个父节点并将其添加到栈中
                tnode = tnode.parent;

                // 如果父节点为null，停止向上递归
                if (ReferenceEquals(tnode, null))
                    break;
            }

            if (nodeStack.Count == 0)
                return;

            System.Type type = typeof(T);

            // 从我们的池中获取正确的搜索列表
            List<T> searchList;
            if (!searchLists.ContainsKey(type))
            {
                searchList = new List<T>();
                searchLists.Add(type, searchList);
            }
            else
            {
                searchList = searchLists[type] as List<T>;
            }

            // 反向迭代找到的节点。这会产生一个GetComponentInParent的效果，从父停止节点向下到提供的transform
            while (nodeStack.Count > 0)
            {
                var node = nodeStack.Pop();

                node.GetComponents(searchList);
                list.AddRange(searchList);
            }
        }


        /// <summary>
        /// 与GetComponentsInChildren相同，但不会递归到具有DontRecurseOnT类型组件的子节点中。这允许尊重PhotonViews/NetObjects的嵌套。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="list">传入null将使用复用的列表。请立即使用结果。</param>
        public static List<T> GetNestedComponentsInChildren<T, NestedT>(this Transform t, List<T> list, bool includeInactive = true)
            where T : class
            where NestedT : class
        {
            System.Type type = typeof(T);

            // 临时列表也会被回收。获取/创建此类型的可复用List。
            List<T> searchList;
            if (!searchLists.ContainsKey(type))
                searchLists.Add(type, searchList = new List<T>());
            else
                searchList = searchLists[type] as List<T>;

            nodesQueue.Clear();

            if (list == null)
                list = new List<T>();

            // 获取起始transform上的组件 - 无例外
            t.GetComponents(list);

            // 将第一层子节点添加到队列中以进行下一层处理。
            for (int i = 0, cnt = t.childCount; i < cnt; ++i)
            {
                var child = t.GetChild(i);

                // 忽略非活动节点（可选）
                if (!includeInactive && !child.gameObject.activeSelf)
                    continue;

                // 忽略嵌套的DontRecurseOnT
                if (!ReferenceEquals(child.GetComponent<NestedT>(), null))
                    continue;

                nodesQueue.Enqueue(child);
            }

            // 递归处理节点层
            while (nodesQueue.Count > 0)
            {
                var node = nodesQueue.Dequeue();

                // 添加在此gameobject节点上找到的组件
                node.GetComponents(searchList);
                list.AddRange(searchList);

                // 将子节点添加到队列中以进行下一层处理。
                for (int i = 0, cnt = node.childCount; i < cnt; ++i)
                {
                    var child = node.GetChild(i);

                    // 忽略非活动节点（可选）
                    if (!includeInactive && !child.gameObject.activeSelf)
                        continue;

                    // 忽略嵌套的NestedT
                    if (!ReferenceEquals(child.GetComponent<NestedT>(), null))
                        continue;

                    nodesQueue.Enqueue(child);
                }
            }

            return list;
        }

        /// <summary>
        /// 与GetComponentsInChildren相同，但不会递归到具有DontRecurseOnT类型组件的子节点中。这允许尊重PhotonViews/NetObjects的嵌套。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="list">传入null将使用复用的列表。请立即使用结果。</param>
        public static List<T> GetNestedComponentsInChildren<T>(this Transform t, List<T> list, bool includeInactive = true, params System.Type[] stopOn)
            where T : class
        {
            System.Type type = typeof(T);

            // 临时列表也会被回收。获取/创建此类型的可复用List。
            List<T> searchList;
            if (!searchLists.ContainsKey(type))
                searchLists.Add(type, searchList = new List<T>());
            else
                searchList = searchLists[type] as List<T>;

            nodesQueue.Clear();

            // 获取起始transform上的组件 - 无例外
            t.GetComponents(list);

            // 将第一层子节点添加到队列中以进行下一层处理。
            for (int i = 0, cnt = t.childCount; i < cnt; ++i)
            {
                var child = t.GetChild(i);

                // 忽略非活动节点（可选）
                if (!includeInactive && !child.gameObject.activeSelf)
                    continue;

                // 忽略嵌套的DontRecurseOnT
                bool stopRecurse = false;
                for (int s = 0, scnt = stopOn.Length; s < scnt; ++s)
                {
                    if (!ReferenceEquals(child.GetComponent(stopOn[s]), null))
                    {
                        stopRecurse = true;
                        break;
                    }
                }
                if (stopRecurse)
                    continue;

                nodesQueue.Enqueue(child);
            }

            // 递归处理节点层
            while (nodesQueue.Count > 0)
            {
                var node = nodesQueue.Dequeue();

                // 添加在此gameobject节点上找到的组件
                node.GetComponents(searchList);
                list.AddRange(searchList);

                // 将子节点添加到队列中以进行下一层处理。
                for (int i = 0, cnt = node.childCount; i < cnt; ++i)
                {
                    var child = node.GetChild(i);

                    // 忽略非活动节点（可选）
                    if (!includeInactive && !child.gameObject.activeSelf)
                        continue;

                    // 忽略嵌套的NestedT
                    bool stopRecurse = false;
                    for (int s = 0, scnt = stopOn.Length; s < scnt; ++s)
                    {
                        if (!ReferenceEquals(child.GetComponent(stopOn[s]), null))
                        {
                            stopRecurse = true;
                            break;
                        }
                    }

                    if (stopRecurse)
                        continue;

                    nodesQueue.Enqueue(child);
                }
            }

            return list;
        }

        /// <summary>
        /// 与GetComponentsInChildren相同，但不会递归到具有NestedT类型组件的子节点中。这允许尊重PhotonViews/NetObjects的嵌套。
        /// </summary>
        /// <typeparam name="T">将找到的组件转换为此类型。通常是Component，但任何其他可以从SearchT赋值的类/接口也可以使用。</typeparam>
        /// <typeparam name="SearchT">查找此类或接口类型的组件。</typeparam>
        /// <typeparam name="DontRecurseOnT"></typeparam>
        /// <param name="t"></param>
        /// <param name="includeInactive"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static void GetNestedComponentsInChildren<T, SearchT, NestedT>(this Transform t, bool includeInactive, List<T> list)
            where T : class
            where SearchT : class
        {
            list.Clear();

            // 如果这是非活动的，将不会找到任何内容。如果我们被限制为仅活动状态，则现在放弃。
            if (!includeInactive && !t.gameObject.activeSelf)
                return;

            System.Type searchType = typeof(SearchT);

            // 临时列表也会被回收。获取/创建此类型的可复用List。
            List<SearchT> searchList;
            if (!searchLists.ContainsKey(searchType))
                searchLists.Add(searchType, searchList = new List<SearchT>());
            else
                searchList = searchLists[searchType] as List<SearchT>;

            // 逐层递归子节点。使用Queue可以在不花费太多功夫的情况下完成此操作。
            nodesQueue.Clear();
            nodesQueue.Enqueue(t);

            while (nodesQueue.Count > 0)
            {
                var node = nodesQueue.Dequeue();

                // 添加在此gameobject节点上找到的组件
                searchList.Clear();
                node.GetComponents(searchList);
                foreach (var comp in searchList)
                {
                    var casted = comp as T;
                    if (!ReferenceEquals(casted, null))
                        list.Add(casted);
                }

                // 将子节点添加到队列中以进行下一层处理。
                for (int i = 0, cnt = node.childCount; i < cnt; ++i)
                {
                    var child = node.GetChild(i);

                    // 忽略非活动节点（可选）
                    if (!includeInactive && !child.gameObject.activeSelf)
                        continue;

                    // 忽略嵌套的DontRecurseOnT
                    if (!ReferenceEquals(child.GetComponent<NestedT>(), null))
                        continue;

                    nodesQueue.Enqueue(child);
                }
            }

        }

        #endregion
    }

}