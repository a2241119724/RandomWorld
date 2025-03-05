using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LAB2D
{
    public abstract class MVCNavigationView : MonoBehaviour
    {
        /// <summary>
        /// 当前的背包所选择的栏
        /// </summary>
        public ItemType CurItemType { get; set; }
        public UnityAction<int> OnClick;

        protected void bindButton(ItemType start, ItemType end)
        {
            Tool.splitEnum<ItemType>(start,end).ForEach(item => addClickOnButton(item));
        }

        /// <summary>
        /// 切换物品栏
        /// </summary>
        public void addClickOnButton(ItemType item) {
            Tool.GetComponentInChildren<Button>(gameObject, item.ToString()).onClick.AddListener(() => {
                CurItemType = item;
                OnClick?.Invoke(ItemDataManager.Instance.getIndexByType(item));
            });
        }

        protected abstract void init();
    }
}
