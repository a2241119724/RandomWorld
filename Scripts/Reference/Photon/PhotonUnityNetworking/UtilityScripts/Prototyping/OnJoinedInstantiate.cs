// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OnJoinedInstantiate.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Utilities,
// </copyright>
// <summary>
//  此组件将在加入房间时实例化一个网络GameObject
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

using Photon.Realtime;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Photon.Pun.UtilityScripts
{

    /// <summary>
    /// 此组件将在加入房间时实例化一个网络GameObject
    /// </summary>
    public class OnJoinedInstantiate : MonoBehaviour
        , IMatchmakingCallbacks
    {
        public enum SpawnSequence { Connection, Random, RoundRobin }

        #region Inspector Items

        // 旧字段，仅为向后兼容保留。值将在OnValidate中复制到SpawnPoints
        [HideInInspector] private Transform SpawnPosition;

        [HideInInspector] public SpawnSequence Sequence = SpawnSequence.Connection;

        [HideInInspector] public List<Transform> SpawnPoints = new List<Transform>(1) { null };

        [Tooltip("Add a random variance to a spawn point position. GetRandomOffset() can be overridden with your own method for producing offsets.")]
        [HideInInspector] public bool UseRandomOffset = true;

        [Tooltip("Radius of the RandomOffset.")]
        [FormerlySerializedAs("PositionOffset")]
        [HideInInspector] public float RandomOffset = 2.0f;

        [Tooltip("Disables the Y axis of RandomOffset. The Y value of the spawn point will be used.")]
        [HideInInspector] public bool ClampY = true;

        [HideInInspector] public List<GameObject> PrefabsToInstantiate = new List<GameObject>(1) { null }; // 在Inspector中设置

        [FormerlySerializedAs("autoSpawnObjects")]
        [HideInInspector] public bool AutoSpawnObjects = true;

        #endregion

        // 已生成对象的记录，用于Despawn All
        public Stack<GameObject> SpawnedObjects = new Stack<GameObject>();
        protected int spawnedAsActorId;



#if UNITY_EDITOR

        protected void OnValidate()
        {
            /// 检查预制体以确保它是实际的资源，而不是场景对象或其他实例。
            if (PrefabsToInstantiate != null)
                for (int i = 0; i < PrefabsToInstantiate.Count; ++i)
                {
                    var prefab = PrefabsToInstantiate[i];
                    if (prefab)
                        PrefabsToInstantiate[i] = ValidatePrefab(prefab);
                }

            /// 将旧SpawnPosition字段中的任何值移动到新的SpawnPoints
            if (SpawnPosition)
            {
                if (SpawnPoints == null)
                    SpawnPoints = new List<Transform>();

                SpawnPoints.Add(SpawnPosition);
                SpawnPosition = null;
            }
        }

        /// <summary>
        /// 验证，如果有效则将此预制体添加到列表的第一个null元素，或创建一个新元素。如果对象已添加则返回true。
        /// </summary>
        /// <param name="prefab"></param>
        public bool AddPrefabToList(GameObject prefab)
        {
            var validated = ValidatePrefab(prefab);
            if (validated)
            {
                // 如果此预制体已在列表中，则不添加
                if (PrefabsToInstantiate.Contains(validated))
                    return false;

                // 首先尝试使用任何null数组槽来保持整洁
                if (PrefabsToInstantiate.Contains(null))
                    PrefabsToInstantiate[PrefabsToInstantiate.IndexOf(null)] = validated;
                // 否则，直接添加此预制体。
                else
                    PrefabsToInstantiate.Add(validated);
                return true;
            }

            return false;

        }

        /// <summary>
        /// 判断提供的GameObject是预制体实例还是实际的源资源，
        /// 并返回开发人员意图使用的实际资源的最佳猜测。
        /// </summary>
        /// <returns></returns>
        protected static GameObject ValidatePrefab(GameObject unvalidated)
        {
            if (unvalidated == null)
                return null;

            if (!unvalidated.GetComponent<PhotonView>())
                return null;

#if UNITY_2018_3_OR_NEWER

            GameObject validated = null;

            if (unvalidated != null)
            {

                if (PrefabUtility.IsPartOfPrefabAsset(unvalidated))
                    return unvalidated;

                var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(unvalidated);
#pragma warning disable CS0618 // PrefabInstanceStatus.Disconnected已过时
                var isValidPrefab = prefabStatus == PrefabInstanceStatus.Connected || prefabStatus == PrefabInstanceStatus.Disconnected;
#pragma warning restore CS0618

                if (isValidPrefab)
                    validated = PrefabUtility.GetCorrespondingObjectFromSource(unvalidated) as GameObject;
                else
                    return null;

                if (!PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(validated).Contains("/Resources"))
                    Debug.LogWarning("Player Prefab needs to be a Prefab in a Resource folder.");
            }
#else
            GameObject validated = unvalidated;

            if (unvalidated != null && PrefabUtility.GetPrefabType(unvalidated) != PrefabType.Prefab)
                validated = PrefabUtility.GetPrefabParent(unvalidated) as GameObject;
#endif
            return validated;
        }

#endif


        public virtual void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }

        public virtual void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }


        public virtual void OnJoinedRoom()
        {
            // 只有当我们是新ActorId时才自动生成。重新加入应通过服务器实例化来重现对象。
            if (AutoSpawnObjects && !PhotonNetwork.LocalPlayer.HasRejoined)
            {
                SpawnObjects();
            }
        }

        public virtual void SpawnObjects()
        {
            if (this.PrefabsToInstantiate != null)
            {
                foreach (GameObject o in this.PrefabsToInstantiate)
                {
                    if (o == null)
                        continue;
#if UNITY_EDITOR
                    Debug.Log("Auto-Instantiating: " + o.name);
#endif
                    Vector3 spawnPos; Quaternion spawnRot;
                    GetSpawnPoint(out spawnPos, out spawnRot);


                    var newobj = PhotonNetwork.Instantiate(o.name, spawnPos, spawnRot, 0);
                    SpawnedObjects.Push(newobj);
                }
            }
        }

        /// <summary>
        /// 销毁此组件为此客户端生成的所有对象。
        /// </summary>
        /// <param name="localOnly">使用Object.Destroy而不是PhotonNetwork.Destroy。</param>
        public virtual void DespawnObjects(bool localOnly)
        {

            while (SpawnedObjects.Count > 0)
            {
                var go = SpawnedObjects.Pop();
                if (go)
                {
                    if (localOnly)
                        Object.Destroy(go);
                    else
                        PhotonNetwork.Destroy(go);

                }
            }
        }

        public virtual void OnFriendListUpdate(List<FriendInfo> friendList) { }
        public virtual void OnCreatedRoom() { }
        public virtual void OnCreateRoomFailed(short returnCode, string message) { }
        public virtual void OnJoinRoomFailed(short returnCode, string message) { }
        public virtual void OnJoinRandomFailed(short returnCode, string message) { }
        public virtual void OnLeftRoom() { }

        protected int lastUsedSpawnPointIndex = -1;

        /// <summary>
        /// 使用SpawnSequence从列表中获取下一个SpawnPoint，并将RandomOffset（如果使用）应用于变换矩阵。
        /// 重写此方法以使用任何自定义代码来生成生成位置。此方法由AutoSpawn使用。
        /// </summary>
        public virtual void GetSpawnPoint(out Vector3 spawnPos, out Quaternion spawnRot)
        {

            // 使用指定的Sequence方法获取一个点
            Transform point = GetSpawnPoint();

            if (point != null)
            {
                spawnPos = point.position;
                spawnRot = point.rotation;
            }
            else
            {
                spawnPos = new Vector3(0, 0, 0);
                spawnRot = new Quaternion(0, 0, 0, 1);
            }

            if (UseRandomOffset)
            {
                Random.InitState((int)(Time.time * 10000));
                spawnPos += GetRandomOffset();
            }
        }


        /// <summary>
        /// 从列表中使用SpawnSequence设置选择下一个SpawnPoint的变换。
        /// 不应用RandomOffset，仅返回SpawnPoint的变换。
        /// 重写此方法以更改SpawnPoint变换的选择方式。返回你想要作为生成点使用的变换。
        /// </summary>
        /// <returns></returns>
        protected virtual Transform GetSpawnPoint()
        {
            // 使用指定的Sequence方法获取一个点
            if (SpawnPoints == null || SpawnPoints.Count == 0)
            {
                return null;
            }
            else
            {
                switch (Sequence)
                {
                    case SpawnSequence.Connection:
                        {
                            int id = PhotonNetwork.LocalPlayer.ActorNumber;
                            return SpawnPoints[(id == -1) ? 0 : id % SpawnPoints.Count];
                        }

                    case SpawnSequence.RoundRobin:
                        {
                            lastUsedSpawnPointIndex++;
                            if (lastUsedSpawnPointIndex >= SpawnPoints.Count)
                                lastUsedSpawnPointIndex = 0;

                            /// 如果我们处理的是没有生成点或生成点为null的情况，则使用Vector.Zero和Quaternion.Identity。
                            return SpawnPoints == null || SpawnPoints.Count == 0 ? null : SpawnPoints[lastUsedSpawnPointIndex];
                        }

                    case SpawnSequence.Random:
                        {
                            return SpawnPoints[Random.Range(0, SpawnPoints.Count)];
                        }

                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// 当启用UseRandomOffset时，调用此方法生成Vector3偏移量。默认实现将Y值限制为零。你可以使用自己的实现重写此方法。
        /// </summary>
        protected virtual Vector3 GetRandomOffset()
        {
            Vector3 random = Random.insideUnitSphere;
            if (ClampY)
                random.y = 0;
            return RandomOffset * random.normalized;
        }

    }

#if UNITY_EDITOR

    [CustomEditor(typeof(OnJoinedInstantiate), true)]
    [CanEditMultipleObjects]
    public class OnJoinedInstantiateEditor : Editor
    {

        SerializedProperty SpawnPoints, PrefabsToInstantiate, UseRandomOffset, ClampY, RandomOffset, Sequence, autoSpawnObjects;
        GUIStyle fieldBox;

        private void OnEnable()
        {
            SpawnPoints = serializedObject.FindProperty("SpawnPoints");
            PrefabsToInstantiate = serializedObject.FindProperty("PrefabsToInstantiate");
            UseRandomOffset = serializedObject.FindProperty("UseRandomOffset");
            ClampY = serializedObject.FindProperty("ClampY");
            RandomOffset = serializedObject.FindProperty("RandomOffset");
            Sequence = serializedObject.FindProperty("Sequence");

            autoSpawnObjects = serializedObject.FindProperty("AutoSpawnObjects");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            const int PAD = 6;

            if (fieldBox == null)
                fieldBox = new GUIStyle("HelpBox") { padding = new RectOffset(PAD, PAD, PAD, PAD) };

            EditorGUI.BeginChangeCheck();

            EditableReferenceList(PrefabsToInstantiate, new GUIContent(PrefabsToInstantiate.displayName, PrefabsToInstantiate.tooltip), fieldBox);

            EditableReferenceList(SpawnPoints, new GUIContent(SpawnPoints.displayName, SpawnPoints.tooltip), fieldBox);

            /// 生成模式
            EditorGUILayout.BeginVertical(fieldBox);
            EditorGUILayout.PropertyField(Sequence);
            EditorGUILayout.PropertyField(UseRandomOffset);
            if (UseRandomOffset.boolValue)
            {
                EditorGUILayout.PropertyField(RandomOffset);
                EditorGUILayout.PropertyField(ClampY);
            }
            EditorGUILayout.EndVertical();

            /// 自动/手动生成
            EditorGUILayout.BeginVertical(fieldBox);
            EditorGUILayout.PropertyField(autoSpawnObjects);
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// 从SerializedProperty列表或数组创建基本的渲染对象列表，带有Add/Destroy按钮。
        /// </summary>
        /// <param name="list"></param>
        /// <param name="gc"></param>
        public void EditableReferenceList(SerializedProperty list, GUIContent gc, GUIStyle style = null)
        {
            EditorGUILayout.LabelField(gc);

            if (style == null)
                style = new GUIStyle("HelpBox") { padding = new RectOffset(6, 6, 6, 6) };

            EditorGUILayout.BeginVertical(style);

            int count = list.arraySize;

            if (count == 0)
            {
                if (GUI.Button(EditorGUILayout.GetControlRect(GUILayout.MaxWidth(20)), "+", (GUIStyle)"minibutton"))
                {
                    int newindex = list.arraySize;
                    list.InsertArrayElementAtIndex(0);
                    list.GetArrayElementAtIndex(0).objectReferenceValue = null;
                }
            }
            else
            {
                // 列表元素和删除按钮
                for (int i = 0; i < count; ++i)
                {
                    EditorGUILayout.BeginHorizontal();
                    bool add = (GUI.Button(EditorGUILayout.GetControlRect(GUILayout.MaxWidth(20)), "+", (GUIStyle)"minibutton"));
                    EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), GUIContent.none);
                    bool remove = (GUI.Button(EditorGUILayout.GetControlRect(GUILayout.MaxWidth(20)), "x", (GUIStyle)"minibutton"));

                    EditorGUILayout.EndHorizontal();

                    if (add)
                    {
                        Add(list, i);
                        break;
                    }

                    if (remove)
                    {
                        list.DeleteArrayElementAtIndex(i);
                        //EditorGUILayout.EndHorizontal();
                        break;
                    }
                }

                EditorGUILayout.GetControlRect(false, 4);

                if (GUI.Button(EditorGUILayout.GetControlRect(), "Add", (GUIStyle)"minibutton"))
                    Add(list, count);

            }


            EditorGUILayout.EndVertical();
        }

        private void Add(SerializedProperty list, int i)
        {
            {
                int newindex = list.arraySize;
                list.InsertArrayElementAtIndex(i);
                list.GetArrayElementAtIndex(i).objectReferenceValue = null;
            }
        }
    }


#endif
}