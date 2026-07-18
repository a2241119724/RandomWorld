// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TabViewManager.cs" company="Exit Games GmbH">
//   Part of: PunCockpit
// </copyright>
// <summary>
//  标签页的简易管理器，需要一个 ToggleGroup，然后为每个标签页提供唯一名称、关联的 Toggle 及其对应的 RectTransform 视图
// 此管理器处理标签页视图的激活与停用，并在选中标签页时提供 Unity 事件回调。
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Photon.Pun.UtilityScripts
{
    /// <summary>
    /// 标签页视图管理器。处理标签页视图的激活与停用，并在选中标签页时提供 Unity 事件回调。
    /// </summary>
    public class TabViewManager : MonoBehaviour
    {

        /// <summary>
        /// 标签页切换事件。
        /// </summary>
        [System.Serializable]
        public class TabChangeEvent : UnityEvent<string> { }

        [Serializable]
        public class Tab
        {
            public string ID = "";
            public Toggle Toggle;
            public RectTransform View;
        }

        /// <summary>
        /// 目标 ToggleGroup 组件。
        /// </summary>
        public ToggleGroup ToggleGroup;

        /// <summary>
        /// 此组的所有标签页
        /// </summary>
        public Tab[] Tabs;

        /// <summary>
        /// 标签页切换事件。
        /// </summary>
        public TabChangeEvent OnTabChanged;

        protected Tab CurrentTab;

        Dictionary<Toggle, Tab> Tab_lut;

        void Start()
        {

            Tab_lut = new Dictionary<Toggle, Tab>();

            foreach (Tab _tab in this.Tabs)
            {

                Tab_lut[_tab.Toggle] = _tab;

                _tab.View.gameObject.SetActive(_tab.Toggle.isOn);

                if (_tab.Toggle.isOn)
                {
                    CurrentTab = _tab;
                }
                _tab.Toggle.onValueChanged.AddListener((isSelected) =>
                {
                    if (!isSelected)
                    {
                        return;
                    }
                    OnTabSelected(_tab);
                });
            }


        }

        /// <summary>
        /// 选择指定的标签页。
        /// </summary>
        /// <param name="id">标签页 ID</param>
        public void SelectTab(string id)
        {
            foreach (Tab _t in Tabs)
            {
                if (_t.ID == id)
                {
                    _t.Toggle.isOn = true;
                    return;
                }
            }
        }


        /// <summary>
        /// 标签页选中流程的最终方法
        /// </summary>
        /// <param name="tab">标签页。</param>
        void OnTabSelected(Tab tab)
        {
            CurrentTab.View.gameObject.SetActive(false);

            CurrentTab = Tab_lut[ToggleGroup.ActiveToggles().FirstOrDefault()];

            CurrentTab.View.gameObject.SetActive(true);

            OnTabChanged.Invoke(CurrentTab.ID);

        }
    }
}