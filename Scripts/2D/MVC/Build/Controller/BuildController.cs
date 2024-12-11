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
                addItem(ItemFactory.Instance.getByName("Room"));
                addItem(ItemFactory.Instance.getByName("WallT"));
                addItem(ItemFactory.Instance.getByName("WallD"));
                addItem(ItemFactory.Instance.getByName("WallR"));
                addItem(ItemFactory.Instance.getByName("WallL"));
                addItem(ItemFactory.Instance.getByName("WallRT"));
                addItem(ItemFactory.Instance.getByName("WallRD"));
                addItem(ItemFactory.Instance.getByName("WallLT"));
                addItem(ItemFactory.Instance.getByName("WallLD"));
            }
        }
    }
}
