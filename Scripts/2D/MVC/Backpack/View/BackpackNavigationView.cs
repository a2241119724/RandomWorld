using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LAB2D
{
    public class BackpackNavigationView : MVCNavigationView
    {
        public static BackpackNavigationView Instance { set; get; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            CurItemType = ItemType.Weapon;
        }

        void Start()
        {
            addClickOnButton(ItemType.Weapon);
            addClickOnButton(ItemType.Equipment);
            addClickOnButton(ItemType.Consumable);
            addClickOnButton(ItemType.Material);
            addClickOnButton(ItemType.Task);
            addClickOnButton(ItemType.Other);
        }

        protected override void init()
        {
            BackpackMenuPanel.Instance.Select.init();
        }
    }
}
