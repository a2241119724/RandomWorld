namespace LAB2D
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 点击道具显示道具信息
    /// </summary>
    public abstract class MVCInfoView : MonoBehaviour
    {
        // private SelectAndShowEventSO selectAndShow;
        private Text info; // 道具信息

        public void Start()
        {
            // info = transform.Find("Background/Message").GetComponent<Text>();
            this.info = Tool.GetComponentInChildren<Text>(this.gameObject, "Message");
            this.info.text = string.Empty;
        }

        /// <summary>
        /// 展示信息
        /// </summary>
        /// <param name="item">道具</param>
        public void ShowInfo(AItem item)
        {
            if (item == null)
            {
                LogManager.Instance.Log("item is null!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.info.text = item.ToString();

            // 记录点击的是哪个道具
        }

        private void OnEnable()
        {
            // selectAndShow = Resources.Load<SelectAndShowEventSO>("SO/SelectAndShowEvent");
            // //订阅事件
            // selectAndShow.OnSelectAndRun += showInfo;
        }

        // private void OnDisable()
        // {
        //     // 删除订阅(必要的)
        //     selectAndShow.OnSelectAndRun -= showInfo;
        // }
    }
}
