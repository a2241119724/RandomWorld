// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TextButtonTransition.cs" company="Exit Games GmbH">
// </copyright>
// <summary>
//  在按钮文本上使用此脚本，可以在不破坏按钮行为的情况下实现文本颜色过渡效果。
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Photon.Pun.UtilityScripts
{

    /// <summary>
    /// 在按钮文本上使用此脚本，可以在不破坏按钮行为的情况下实现文本颜色过渡效果。
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class TextButtonTransition : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {

        Text _text;

        /// <summary>
        /// 可选择的组件。
        /// </summary>
		public Selectable Selectable;

        /// <summary>
        /// 过渡状态的正常颜色。
        /// </summary>
		public Color NormalColor = Color.white;

        /// <summary>
        /// 过渡状态的悬停颜色。
        /// </summary>
		public Color HoverColor = Color.black;

        public void Awake()
        {
            _text = GetComponent<Text>();
        }

        public void OnEnable()
        {
            _text.color = NormalColor;
        }

        public void OnDisable()
        {
            _text.color = NormalColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Selectable == null || Selectable.IsInteractable())
            {
                _text.color = HoverColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Selectable == null || Selectable.IsInteractable())
            {
                _text.color = NormalColor;
            }
        }
    }
}