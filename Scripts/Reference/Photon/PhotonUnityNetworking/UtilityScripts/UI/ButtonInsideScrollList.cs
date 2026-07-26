// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ButtonInsideScrollList.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Utilities,
// </copyright>
// <summary>
//  用于 UI 列表中的按钮，防止在按钮上按下时父级 scrollRect 滚动。
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Photon.Pun.UtilityScripts
{
    /// <summary>
    /// 滚动列表中的按钮将阻止 scrollRect 容器的滚动能力，使得在按钮上按下并拖动时不会影响滚动。
    /// 如果父层级中找不到 scrollRect 组件，则此脚本不执行任何操作。
    /// </summary>
    public class ButtonInsideScrollList : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {

        ScrollRect scrollRect;

        // 用于初始化
        void Start()
        {
            scrollRect = GetComponentInParent<ScrollRect>();
        }

        #region IPointerDownHandler implementation
        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (scrollRect != null)
            {
                scrollRect.StopMovement();
                scrollRect.enabled = false;
            }
        }
        #endregion

        #region IPointerUpHandler implementation

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            if (scrollRect != null && !scrollRect.enabled)
            {
                scrollRect.enabled = true;
            }
        }

        #endregion
    }
}