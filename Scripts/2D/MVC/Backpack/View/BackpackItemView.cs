using UnityEngine;
using UnityEngine.EventSystems;

namespace LAB2D
{
    public class BackpackItemView : MVCItemView
    {
        public override void setSelect(int i, Item item)
        {
            //// 发布事件,GetSiblingIndex索引(第几个孩子)
            //selectAndShow.select(item);
            BackpackMenuPanel.Instance.Select.selectItemIndex = i;
            BackpackMenuPanel.Instance.Select.item = (BackpackItem)item;
        }
    }
}