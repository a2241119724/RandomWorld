namespace LAB2D
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 游戏加载进度条 UI
    /// </summary>
    public class AsyncProgressUI : MonoBehaviour
    {
        private int total;
        private Text tip; // 提示信息
        private Text percent; // 百分比
        private Slider slider; // 进度条
        private bool isOne = false; // 进度条

        /// <summary>
        /// 完成委托
        /// </summary>
        public delegate void CompleteDelegate();

        /// <summary>
        /// 单例
        /// </summary>
        public static AsyncProgressUI Instance { get; private set; }

        /// <summary>
        /// 所有完成回调
        /// </summary>
        public CompleteDelegate Complete { get; set; }

        /// <summary>
        /// 当前进度值
        /// </summary>
        public long CurProcess { get; set; }

        public void Awake()
        {
            Instance = this;
            this.tip = this.transform.Find("Center/Tips").GetComponent<Text>();
            if (this.tip == null)
            {
                LogManager.Instance.Log("tips Not Found!!!", LogManager.LogLevel.Error);
                return;
            }

            this.percent = this.transform.Find("Center/Percent").GetComponent<Text>();
            if (this.percent == null)
            {
                LogManager.Instance.Log("percent Not Found!!!", LogManager.LogLevel.Error);
                return;
            }

            this.slider = this.transform.Find("Center/ProgressBar").GetComponent<Slider>();
            if (this.slider == null)
            {
                LogManager.Instance.Log("slider Not Found!!!", LogManager.LogLevel.Error);
                return;
            }
        }

        /// <summary>
        /// 添加进度值1
        /// </summary>
        public void AddOneProcess()
        {
            this.CurProcess += 1;
            this.Show();
            if (this.CurProcess >= this.total && !this.isOne)
            {
                this.isOne = true;
                this.StartCoroutine(this.Complete1());
            }
        }

        /// <summary>
        /// 设置加载信息
        /// </summary>
        /// <param name="tip">信息</param>
        public void SetTip(string tip)
        {
            this.tip.text = tip;
            this.Show();
        }

        /// <summary>
        /// 增加进度值
        /// </summary>
        /// <param name="value">进度值</param>
        public void AddTotal(int value)
        {
            if (value < 0)
            {
                LogManager.Instance.Log("不能为负值!!!", LogManager.LogLevel.Error);
            }

            this.total += value;
        }

        /// <summary>
        /// 展示进度条
        /// </summary>
        private void Show()
        {
            this.percent.text = "当前进度:" + (this.CurProcess * 1000 / this.total / 10.0f).ToString() + "%";
            this.slider.value = this.CurProcess * 1.0f / this.total;
        }

        private IEnumerator Complete1()
        {
            yield return new WaitForSeconds(0.5f);
            this.Complete();
        }
    }
}