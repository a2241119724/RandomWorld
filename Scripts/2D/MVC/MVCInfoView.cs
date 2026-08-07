namespace LAB2D.MVC
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Item;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 点击道具显示道具信息
    /// </summary>
    public abstract class MVCInfoView : MonoBehaviour
    {
        private Text info; // 道具信息

        public void Start()
        {
            // info = transform.Find("Background/Message").GetComponent<Text>();
            this.info = LAB2D.Tool.Tool.GetComponentInChildren<Text>(this.gameObject, "Message");
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
                AWorkerTask.LogProvider("item is null!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.info.text = item.ToString();
        }

    }
}
