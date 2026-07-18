// ----------------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Exit Games GmbH">
//   Photon Extensions - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>
//   为 Hashtables 等提供一些有用的方法和扩展。
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------

#if UNITY_4_7 || UNITY_5 || UNITY_5_3_OR_NEWER
#define SUPPORTED_UNITY
#endif


namespace Photon.Realtime
{
    using System.Collections;
    using System.Collections.Generic;

#if SUPPORTED_UNITY
#endif
#if SUPPORTED_UNITY || NETFX_CORE
    using Hashtable = ExitGames.Client.Photon.Hashtable;
    using SupportClass = ExitGames.Client.Photon.SupportClass;
#endif


    /// <summary>
    /// 此静态类为几个现有类（例如 Vector3、float 等）定义了一些有用的扩展方法。
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// 将 addHash 中的所有键合并到 target 中。添加新键并更新 target 中已有键的值。
        /// </summary>
        /// <param name="target">要更新的 IDictionary。</param>
        /// <param name="addHash">包含要合并到 target 的数据的 IDictionary。</param>
        public static void Merge(this IDictionary target, IDictionary addHash)
        {
            if (addHash == null || target.Equals(addHash))
            {
                return;
            }

            foreach (object key in addHash.Keys)
            {
                target[key] = addHash[key];
            }
        }

        /// <summary>
        /// 将字符串类型的键合并到 target Hashtable 中。
        /// </summary>
        /// <remarks>
        /// 不会从 target 中移除键（因此非字符串键如果之前存在，可以保留在 target 中）。
        /// </remarks>
        /// <param name="target">传入的目标 IDictionary 以及 addHash 中的所有字符串类型键。</param>
        /// <param name="addHash">应部分合并到 target 中以更新它的 IDictionary。</param>
        public static void MergeStringKeys(this IDictionary target, IDictionary addHash)
        {
            if (addHash == null || target.Equals(addHash))
            {
                return;
            }

            foreach (object key in addHash.Keys)
            {
                // 仅合并字符串类型的键
                if (key is string)
                {
                    target[key] = addHash[key];
                }
            }
        }

        /// <summary>用于调试 IDictionary 内容的辅助方法，包括类型信息。使用此方法性能不高。</summary>
        /// <remarks>应仅在必要时用于调试。</remarks>
        /// <param name="origin">某个 Dictionary 或 Hashtable。</param>
        /// <returns>IDictionary 内容的字符串。</returns>
        public static string ToStringFull(this IDictionary origin)
        {
            return SupportClass.DictionaryToString(origin, false);
        }

        /// <summary>用于调试 List<T> 内容的辅助方法。使用此方法性能不高。</summary>
        /// <remarks>应仅在必要时用于调试。</remarks>
        /// <param name="data">任何实现了 .ToString() 的 List<T>。</param>
        /// <returns>包含每个值的 ToString() 的逗号分隔字符串。</returns>
        public static string ToStringFull<T>(this List<T> data)
        {
            if (data == null) return "null";

            string[] sb = new string[data.Count];
            for (int i = 0; i < data.Count; i++)
            {
                object o = data[i];
                sb[i] = (o != null) ? o.ToString() : "null";
            }

            return string.Join(", ", sb);
        }

        /// <summary>用于调试 object[] 内容的辅助方法。使用此方法性能不高。</summary>
        /// <remarks>应仅在必要时用于调试。</remarks>
        /// <param name="data">任何 object[]。</param>
        /// <returns>包含每个值的 ToString() 的逗号分隔字符串。</returns>
        public static string ToStringFull(this object[] data)
        {
            if (data == null) return "null";

            string[] sb = new string[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                object o = data[i];
                sb[i] = (o != null) ? o.ToString() : "null";
            }

            return string.Join(", ", sb);
        }


        /// <summary>
        /// 此方法将原对象的所有字符串类型键复制到一个新的 Hashtable 中。
        /// </summary>
        /// <remarks>
        /// 不会递归（!）到根哈希中可能是值的哈希中。
        /// 这不会修改原对象。
        /// </remarks>
        /// <param name="original">要从中获取字符串类型键的原始 IDictonary。</param>
        /// <returns>仅包含原对象的字符串类型键的新 Hashtable。</returns>
        public static Hashtable StripToStringKeys(this IDictionary original)
        {
            Hashtable target = new Hashtable();
            if (original != null)
            {
                foreach (object key in original.Keys)
                {
                    if (key is string)
                    {
                        target[key] = original[key];
                    }
                }
            }

            return target;
        }

        /// <summary>
        /// 此方法将原对象的所有字符串类型键复制到一个新的 Hashtable 中。
        /// </summary>
        /// <remarks>
        /// 不会递归（!）到根哈希中可能是值的哈希中。
        /// 这不会修改原对象。
        /// </remarks>
        /// <param name="original">要从中获取字符串类型键的原始 IDictonary。</param>
        /// <returns>仅包含原对象的字符串类型键的新 Hashtable。</returns>
        public static Hashtable StripToStringKeys(this Hashtable original)
        {
            Hashtable target = new Hashtable();
            if (original != null)
            {
                foreach (DictionaryEntry entry in original)
                {
                    if (entry.Key is string)
                    {
                        target[entry.Key] = original[entry.Key];
                    }
                }
            }

            return target;
        }


        /// <summary>由 StripKeysWithNullValues 使用。</summary>
        /// <remarks>
        /// 通过将 keysWithNullValue 设为静态变量并在使用前清除，分配仅在预热阶段发生，
        /// 因为列表需要增长。一旦达到需要移除的键的高水位线即可。
        /// </remarks>
        private static readonly List<object> keysWithNullValue = new List<object>();

        /// <summary>移除所有具有 null 值的键。</summary>
        /// <remarks>
        /// Photon 属性通过将其值设置为 null 来移除。会更改原始 IDictionary！
        /// 使用 lock(keysWithNullValue)，在预期用例中应该没有问题。
        /// </remarks>
        /// <param name="original">要剥离具有 null 值的键的 IDictionary。</param>
        public static void StripKeysWithNullValues(this IDictionary original)
        {
            lock (keysWithNullValue)
            {
                keysWithNullValue.Clear();

                foreach (DictionaryEntry entry in original)
                {
                    if (entry.Value == null)
                    {
                        keysWithNullValue.Add(entry.Key);
                    }
                }

                for (int i = 0; i < keysWithNullValue.Count; i++)
                {
                    var key = keysWithNullValue[i];
                    original.Remove(key);
                }
            }
        }

        /// <summary>移除所有具有 null 值的键。</summary>
        /// <remarks>
        /// Photon 属性通过将其值设置为 null 来移除。会更改原始 IDictionary！
        /// 使用 lock(keysWithNullValue)，在预期用例中应该没有问题。
        /// </remarks>
        /// <param name="original">要剥离具有 null 值的键的 IDictionary。</param>
        public static void StripKeysWithNullValues(this Hashtable original)
        {
            lock (keysWithNullValue)
            {
                keysWithNullValue.Clear();

                foreach (DictionaryEntry entry in original)
                {
                    if (entry.Value == null)
                    {
                        keysWithNullValue.Add(entry.Key);
                    }
                }

                for (int i = 0; i < keysWithNullValue.Count; i++)
                {
                    var key = keysWithNullValue[i];
                    original.Remove(key);
                }
            }
        }


        /// <summary>
        /// 检查特定的整数值是否在 int 数组中。
        /// </summary>
        /// <remarks>这对于查找特定的 actorNumber 是否在房间的玩家列表中可能很有用。</remarks>
        /// <param name="target">要检查的 int 数组。</param>
        /// <param name="nr">要在 target 中查找的数字。</param>
        /// <returns>如果在 target 中找到了 nr，则返回 True。</returns>
        public static bool Contains(this int[] target, int nr)
        {
            if (target == null)
            {
                return false;
            }

            for (int index = 0; index < target.Length; index++)
            {
                if (target[index] == nr)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

