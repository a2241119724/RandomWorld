using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class BuildNavigationView : MVCNavigationView
    {
        private void OnEnable()
        {
            CurItemType = ItemType.Room;
        }

        void Start()
        {
            bindButton(ItemType.Room, ItemType.BuildOther);
        }

        protected override void init()
        {
            BuildMenuPanel.Instance.Select.init();
        }
    }
}
