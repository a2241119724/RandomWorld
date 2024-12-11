using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class BuildItemView : MVCItemView
    {
        public override void setSelect(int i, Item item)
        {
            //// 发布事件,GetSiblingIndex索引(第几个孩子)
            //selectAndShow.select(item);
            BuildMenuPanel.Instance.Select.selectItemIndex = i;
            BuildMenuPanel.Instance.Select.itemData = (BuildItem)item;
        }
    }
}
