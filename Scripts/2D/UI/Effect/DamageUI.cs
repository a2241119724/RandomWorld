namespace LAB2D.UI.Effect
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 伤害 UI
    /// </summary>
    public class DamageUI : MonoBehaviour
    {
        private const float DestroyTime = 0.75f; // 销毁时间
        private float worldX; // 初始世界 X 坐标
        private float offsetX; // 偏移量
        private Text content; // 内容
        private List<Config> param;

        /// <summary>
        /// 设置伤害数值
        /// </summary>
        /// <param name="value">数值</param>
        /// <param name="colorIndex">颜色</param>
        public void SetDamage(float value, int colorIndex = 0)
        {
            this.content.text = ((float)System.Math.Round(value, 1)).ToString();
            this.content.color = this.param[colorIndex].Color;
            this.content.fontSize = this.param[colorIndex].FontSize;
        }

        public void Awake()
        {
            this.content = this.transform.Find("Text").GetComponent<Text>();
            if (this.content == null)
            {
                AWorkerTask.LogProvider("content Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.offsetX = Random.Range(-0.2f, 0.2f);

            // 不能在Start中
            this.param = new List<Config>
            {
                new Config(PixelUITheme.DamageNormal, 36),
                new Config(PixelUITheme.DamageCritical, 48),
            };
        }

        public void Start()
        {
            // 记录初始世界 X 坐标，用于 Update 中固定 X 轴位置
            this.worldX = this.transform.position.x;
            Destroy(this.gameObject, DestroyTime);
        }

        public void Update()
        {
            // 不随父元素旋转而旋转
            // X 轴固定在初始世界坐标，Y 轴保持当前位置（随 Translate 上浮）
            this.transform.SetPositionAndRotation(new Vector3(this.worldX + this.offsetX, this.transform.position.y, 0), Quaternion.identity);
            this.transform.Translate(2.0f * Time.deltaTime * Vector3.up, Space.World); // 使文本在垂直方向上产生一个偏移
        }

        /// <summary>
        /// 伤害配置
        /// </summary>
        public class Config
        {
            /// <summary>
            /// 颜色
            /// </summary>
            public Color Color;

            /// <summary>
            /// 字体大小
            /// </summary>
            public int FontSize;

            public Config(Color color, int fontSize)
            {
                this.Color = color;
                this.FontSize = fontSize;
            }
        }
    }
}