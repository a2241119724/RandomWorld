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
        /// ��ǰ�ı�����ѡ�����
        /// </summary>
        public ItemType CurItemType { get; set; }
        public UnityAction<int> OnClick;

        /// <summary>
        /// �л���Ʒ��
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