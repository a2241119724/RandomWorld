namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 伤害 UI
    /// </summary>
    public class DamageUI : MonoBehaviour
    {
        private const float DestroyTime = 0.75f; // 销毁时间
        private Transform parent; // 父元素
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

        private void Awake()
        {
            this.content = this.transform.Find("Text").GetComponent<Text>();
            if (this.content == null)
            {
                LogManager.Instance.Log("content Not Found!!!", LogManager.LogLevel.Error);
                return;
            }

            this.offsetX = Random.Range(-0.2f, 0.2f);

            // 不能在Start中
            this.param = new List<Config>();
            this.param.Add(new Config(Color.white, 40));
            this.param.Add(new Config(Color.red, 50));
        }

        private void Start()
        {
            // 由于transform，不能放到Awake中
            this.parent = this.transform.parent;
            if (this.parent == null)
            {
                LogManager.Instance.Log("parent Not Found!!!", LogManager.LogLevel.Error);
                return;
            }

            Destroy(this.gameObject, DestroyTime);
        }

        private void Update()
        {
            // 不随父元素旋转而旋转
            this.transform.rotation = Quaternion.identity;

            // 不随父元素旋转而移动(通过世界坐标偏移量实现)
            this.transform.position = new Vector3(this.parent.position.x + this.offsetX, this.transform.position.y, 0);
            this.transform.Translate(2.0f * Time.deltaTime * Vector3.up, Space.World); // 使文本在垂直方向山产生一个偏移
        }

        /// <summary>
        /// Damage 配置
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