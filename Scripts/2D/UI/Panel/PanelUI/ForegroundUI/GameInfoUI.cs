namespace LAB2D
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 游戏相关信息 UI
    /// </summary>
    public class GameInfoUI : MonoBehaviour
    {
        private Text fps;
        private Text position;

        private float accum; // fps
        private int frames;

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

        public void Awake()
        {
            Instance = this;
            this.fps = Tool.GetComponentInChildren<Text>(this.gameObject, "FPS");
            this.position = Tool.GetComponentInChildren<Text>(this.gameObject, "PlayerPosition");
        }

        public void Start()
        {
            this.StartCoroutine(this.FPS());
        }

        public void Update()
        {
            // fps
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
