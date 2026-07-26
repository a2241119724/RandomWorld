// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventSystemSpawner.cs" company="Exit Games GmbH">
// </copyright>
// <summary>
// 对于增量场景加载的上下文，eventSystem 不能添加到每个场景中，而应该仅在必要时才实例化。
// https://answers.unity.com/questions/1403002/multiple-eventsystem-in-scene-this-is-not-supporte.html
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;

namespace Photon.Pun.UtilityScripts
{
    /// <summary>
    /// 事件系统生成器。将添加一个带有 EventSystem 组件和 StandaloneInputModule 组件的 EventSystem GameObject。
    /// 在增量场景加载的上下文中使用此脚本，否则 Unity 会报"场景中存在多个 EventSystem... 不支持此操作"的错误。
    /// </summary>
    public class EventSystemSpawner : MonoBehaviour
    {
        void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            Debug.LogError("PUN Demos are not compatible with the New Input System, unless you enable \"Both\" in: Edit > Project Settings > Player > Active Input Handling. Pausing App.");
            Debug.Break();
            return;
#endif

            EventSystem sceneEventSystem = FindObjectOfType<EventSystem>();
            if (sceneEventSystem == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");

                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }
        }
    }
}