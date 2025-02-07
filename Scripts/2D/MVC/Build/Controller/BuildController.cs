using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class BuildController : MVCController<BuildItemManagerView, BuildModel, BuildNavigationView, BuildItemView, BuildInfoView>
    {
        public static BuildController Instance;

        public override void Awake()
        {
            itemManagerView = Tool.GetComponentInChildren<BuildItemManagerView>(gameObject, "Inventory");
            navigationView = Tool.GetComponentInChildren<BuildNavigationView>(gameObject, "Navigation");
            infoView = Tool.GetComponentInChildren<BuildInfoView>(gameObject, "Info");
            base.Awake();
            Instance = this;
            // 如果背包为空添加一个武器到背包
            navigationView.CurItemType = ItemType.Room;
            if (model.isNull(navigationView.CurItemType))
            {
                List<Item> items = ItemFactory.Instance.getBuildItems();
                foreach(Item item in items)
                {
                    addItem(item);
                }
            }
        }
    }
}
