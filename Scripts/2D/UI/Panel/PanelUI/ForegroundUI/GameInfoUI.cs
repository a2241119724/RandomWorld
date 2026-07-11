namespace LAB2D.UI.Panel.PanelUI.ForegroundUI
{
    using LAB2D;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 游戏相关信息 UI
    /// </summary>
    public class GameInfoUI : MonoBehaviour
    {
        private Text fps;

        private float accum; // FPS累计
        private int frames;

        /// <summary>
        /// 单例
        /// </summary>
        public static GameInfoUI Instance { get; private set; }

        public void Awake()
        {
            Instance = this;
            this.fps = Tool.GetComponentInChildren<Text>(this.gameObject, "FPS");
        }

        public void Start()
        {
            this.StartCoroutine(this.FPS());
        }

        public void Update()
        {
            // FPS计算
            // 添加本次可能会执行的帧数
            this.accum += Time.timeScale / Time.deltaTime;

            // 一秒总共的次数
            ++this.frames;
        }

        private IEnumerator FPS()
        {
            while (true)
            {
                // 每秒平均帧数
                this.accum /= this.frames;

                // if (!double.IsNaN(accum))
                // {
                this.fps.text = "FPS:" + this.accum.ToString("F1");

                // }
                this.accum = 0.0f;
                this.frames = 0;
                yield return new WaitForSeconds(1.0f);
            }
        }
    }
}
