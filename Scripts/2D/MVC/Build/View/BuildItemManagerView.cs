using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class BuildItemManagerView : MVCItemManagerView<BuildItemView,BuildModel>
    {
        public static BuildItemManagerView Instance { private set; get; }

        public override void Awake()
        {
            base.Awake();
            Instance = this;
            itemBox = ResourcesManager.Instance.getPrefab("BuildItem");
        }

        protected override int getQuantity(Item item)
        {
            return ((BuildItem)item).quantity;
        }
    }
}

