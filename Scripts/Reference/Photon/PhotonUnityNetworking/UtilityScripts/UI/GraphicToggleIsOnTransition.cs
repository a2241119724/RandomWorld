// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImageToggleIsOnTransition.cs" company="Exit Games GmbH">
// </copyright>
// <summary>
//  在 Toggle 图形上使用此脚本，可以在不破坏 Toggle 行为的情况下实现颜色过渡效果。
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Photon.Pun.UtilityScripts
{
    /// <summary>
    /// 在 Toggle 文本上使用此脚本，根据 isOn 状态实现文本颜色过渡。
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class GraphicToggleIsOnTransition : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Toggle toggle;

        private Graphic _graphic;

        public Color NormalOnColor = Color.white;
        public Color NormalOffColor = Color.black;
        public Color HoverOnColor = Color.black;
        public Color HoverOffColor = Color.black;

        private bool isHover;

        public void OnPointerEnter(PointerEventData eventData)
        {
            this.isHover = true;
            this._graphic.color = this.toggle.isOn ? this.HoverOnColor : this.HoverOffColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            this.isHover = false;
            this._graphic.color = this.toggle.isOn ? this.NormalOnColor : this.NormalOffColor;
        }

        public void OnEnable()
        {
            this._graphic = this.GetComponent<Graphic>();

            this.OnValueChanged(this.toggle.isOn);

            this.toggle.onValueChanged.AddListener(this.OnValueChanged);
        }

        public void OnDisable()
        {
            this.toggle.onValueChanged.RemoveListener(this.OnValueChanged);
        }

        public void OnValueChanged(bool isOn)
        {
            this._graphic.color = isOn ? (this.isHover ? this.HoverOnColor : this.HoverOnColor) : (this.isHover ? this.NormalOffColor : this.NormalOffColor);
        }
    }
}