namespace LAB2D
{
    using System.Collections;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 游戏加载进度条 UI
    /// </summary>
    public class AsyncProgressUI : MonoBehaviour
    {
        private int totalProcess;
        private volatile int curProcess; // 当前进度
        private bool isOne = false; // 仅执行一次

        private Text tip; // 提示信息
        private Text percent; // 百分比
        private Slider slider; // 进度条

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

            this.slider = this.transform.Find("Center/Bar").GetComponent<Slider>();
            if (this.slider == null)
            {
                LogManager.Instance.Log("Progress/Bar Not Found!!!", LogManager.LogLevel.Error);
                return;
            }
        }

        /// <summary>
        /// 添加进度值1
        /// </summary>
        public void AddOneProcess()
        {
            this.curProcess += 1;
            if (this.curProcess % 1000 == 0 || this.curProcess >= this.totalProcess)
            {
                this.Show();
            }

            if (this.curProcess >= this.totalProcess && !this.isOne)
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

            this.totalProcess += value;
        }

        /// <summary>
        /// 展示进度条
        /// </summary>
        private void Show()
        {
            this.slider.value = this.curProcess * 1.0f / this.totalProcess;
            this.percent.text = "当前进度:" + Mathf.RoundToInt(this.slider.value * 100) + "%";
        }

        private IEnumerator Complete1()
        {
            yield return new WaitForSeconds(0.5f);
            this.Complete();
        }
    }
}