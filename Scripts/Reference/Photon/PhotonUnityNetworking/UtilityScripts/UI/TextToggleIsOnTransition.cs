// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TextToggleIsOnTransition.cs" company="Exit Games GmbH">
// </copyright>
// <summary>
//  在 Toggle 按钮文本上使用此脚本，可以在不破坏按钮行为的情况下实现文本颜色过渡效果。
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Photon.Pun.UtilityScripts
{

    /// <summary>
    /// 在 Toggle 按钮文本上使用此脚本，根据 isOn 状态实现文本颜色过渡。
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class TextToggleIsOnTransition : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {

        /// <summary>
        /// Toggle 组件。
        /// </summary>
		public Toggle toggle;

        Text _text;

        /// <summary>
        /// 正常开启过渡状态的颜色。
        /// </summary>
		public Color NormalOnColor = Color.white;

        /// <summary>
        /// 正常关闭过渡状态的颜色。
        /// </summary>
		public Color NormalOffColor = Color.black;

        /// <summary>
        /// 悬停开启过渡状态的颜色。
        /// </summary>
		public Color HoverOnColor = Color.black;

        /// <summary>
        /// 悬停关闭过渡状态的颜色。
        /// </summary>
		public Color HoverOffColor = Color.black;

        bool isHover;

        public void OnEnable()
        {
            _text = GetComponent<Text>();

            OnValueChanged(toggle.isOn);

            toggle.onValueChanged.AddListener(OnValueChanged);

        }

        public void OnDisable()
        {
            toggle.onValueChanged.RemoveListener(OnValueChanged);
        }

        public void OnValueChanged(bool isOn)
        {
            _text.color = isOn ? (isHover ? HoverOnColor : HoverOnColor) : (isHover ? NormalOffColor : NormalOffColor);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHover = true;
            _text.color = toggle.isOn ? HoverOnColor : HoverOffColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHover = false;
            _text.color = toggle.isOn ? NormalOnColor : NormalOffColor;
        }

    }
}