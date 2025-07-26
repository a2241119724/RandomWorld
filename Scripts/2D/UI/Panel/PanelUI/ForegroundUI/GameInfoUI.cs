namespace LAB2D
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Rendering.Universal;
    using UnityEngine.UI;

    /// <summary>
    /// 游戏相关信息 UI
    /// </summary>
    public class GameInfoUI : MonoBehaviour
    {
        private Text fps;
        private Text position;
        private Text time;
        private Light2D globalLight; // 白天黑天显示

        private float accum; // fps
        private int frames;

        private double curGameTime; // time
        private double lastGameTime;

        /// <summary>
        /// 单例
        /// </summary>
        public static GameInfoUI Instance { get; private set; }

        /// <summary>
        /// 设置当前游戏画面的位置
        /// </summary>
        /// <param name="worldPos">位置</param>
        public void SetPosition(Vector3 worldPos)
        {
            if (worldPos == null)
            {
                LogManager.Instance.Log("v is null!!!", LogManager.LogLevel.Error);
                return;
            }

            Vector3Int posMap = TileMap.Instance.WorldPosToMapPos(worldPos);
            this.position.text = "(" + posMap.x + "," + posMap.y + ")";
        }

        private void Awake()
        {
            Instance = this;
            this.fps = Tool.GetComponentInChildren<Text>(this.gameObject, "FPS");
            this.position = Tool.GetComponentInChildren<Text>(this.gameObject, "PlayerPosition");
            this.time = Tool.GetComponentInChildren<Text>(this.gameObject, "Time");
            this.globalLight = GameObject.FindGameObjectWithTag(ResourceConstant.GLOBAL_LIGHT_TAG).GetComponent<Light2D>();
        }

        private void Start()
        {
            this.StartCoroutine(this.FPS());
        }

        private void Update()
        {
            // fps
            // 添加本次可能会执行的帧数
            this.accum += Time.timeScale / Time.deltaTime;

            // 一秒总共的次数
            ++this.frames;

            // time
            this.curGameTime += Time.deltaTime;
            this.globalLight.intensity = Mathf.Clamp(Mathf.Abs(Mathf.Cos((float)this.curGameTime / GlobalData.DayTime)), 0.2f, 1.0f);
            if (this.curGameTime - this.lastGameTime >= 1.0)
            {
                this.lastGameTime = this.curGameTime;
                int hour = (int)this.curGameTime / 3600;
                int minute = ((int)this.curGameTime - (hour * 3600)) / 60;
                int second = (int)this.curGameTime - (hour * 3600) - (minute * 60);
                this.time.text = string.Format("<color=blue>游戏时间: </color>{0:D2}:{1:D2}:{2:D2}", hour, minute, second);
            }
        }

        private IEnumerator FPS()
        {
            while (true)
            {
                // 每秒平均帧数
                this.accum = this.accum / this.frames;

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
