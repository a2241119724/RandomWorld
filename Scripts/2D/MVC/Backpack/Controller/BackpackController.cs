using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class BackpackController : MVCController<BackpackItemManagerView,BackpackModel,BackpackNavigationView,BackpackItemView,BackpackInfoView>
    {
        public static BackpackController Instance { get; private set; }
        
        public override void Awake()
        {
            itemManagerView = Tool.GetComponentInChildren<BackpackItemManagerView>(gameObject, "Inventory");
            navigationView = Tool.GetComponentInChildren<BackpackNavigationView>(gameObject, "Navigation");
            infoView = Tool.GetComponentInChildren<BackpackInfoView>(gameObject, "Info");
            base.Awake();
            Instance = this;
            //
            if (model.isNull(ItemType.Weapon))
            {
                // addItem(ItemFactory.Instance.getBackpackItemByName("SingleGun"));
                List<Item> items = ItemFactory.Instance.genBackpackItems();
                foreach (Item item in items)
                {
                    addItem(item);
                }
            }
        }
    }
}