using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class BuildItemView : MVCItemView
    {
        public override void setSelect(int i, Item item)
        {
            //// �����¼�,GetSiblingIndex����(�ڼ�������)
            //selectAndShow.select(item);
            BuildMenuPanel.Instance.Select.selectItemIndex = i;
            BuildMenuPanel.Instance.Select.itemData = (BuildItem)item;
        }
    }
}
