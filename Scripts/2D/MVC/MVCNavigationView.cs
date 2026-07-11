namespace LAB2D.MVC
{
    using LAB2D;
    using LAB2D.Item;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    /// <summary>
    /// 导航按钮UI
    /// </summary>
    public abstract class MVCNavigationView : MonoBehaviour
    {
        /// <summary>
        /// 点击事件
        /// </summary>
        public UnityAction<int> OnClick;

        /// <summary>
        /// 当前的背包所选择的栏
        /// </summary>
        public AItem.ItemTypeEnum CurItemType { get; set; }

        /// <summary>
        /// 切换物品栏
        /// </summary>
        /// <param name="item">道具</param>
        public void AddClickOnButton(AItem.ItemTypeEnum item)
        {
            Tool.GetComponentInChildren<Button>(this.gameObject, item.ToString()).onClick.AddListener(() =>
            {
                this.CurItemType = item;
                this.OnClick?.Invoke(ItemDataManager.Instance.GetIndexByType(item));
            });
        }

        /// <summary>
        /// 绑定按钮
        /// </summary>
        /// <param name="start">道具类型起始</param>
        /// <param name="end">道具类型结束</param>
        protected void BindButton(AItem.ItemTypeEnum start, AItem.ItemTypeEnum end)
        {
            Tool.SplitEnum<AItem.ItemTypeEnum>(start, end).ForEach(item => this.AddClickOnButton(item));
        }

        /// <summary>
        /// 初始化
        /// </summary>
        protected abstract void Init();
    }
}
