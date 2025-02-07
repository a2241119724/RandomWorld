using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    /// <summary>
    /// 背包仓库界面
    /// </summary>
    public class BackpackItemManagerView : MVCItemManagerView<BackpackItemView, BackpackModel>
    {
        public static BackpackItemManagerView Instance { private set; get; }

        public override void Awake()
        {
            base.Awake();
            Instance = this;
            itemBox = ResourcesManager.Instance.getPrefab("BackpackItem");
        }

        protected override int getQuantity(Item item)
        {
            return ((BackpackItem)item).quantity;
        }
    }
}