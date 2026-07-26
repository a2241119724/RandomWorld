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
            this.fps = LAB2D.Tool.Tool.GetComponentInChildren<Text>(this.gameObject, "FPS");
        }

        public void Start()
        {
            this.StartCoroutine(this.FPS());
        }

        public void Update()
        {
            // FPS计算
            // 累计经过的真实时间（不受timeScale影响）
            this.accum += Time.unscaledDeltaTime;

            // 一秒总共的次数
            ++this.frames;
        }

        private IEnumerator FPS()
        {
            while (true)
            {
                if (this.frames > 0)
                {
                    float avgFps = this.frames / this.accum;
                    this.fps.text = "FPS:" + avgFps.ToString("F1");
                }
                this.accum = 0.0f;
                this.frames = 0;
                yield return new WaitForSecondsRealtime(1.0f);
            }
        }
    }
}
