using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class DamageUI : MonoBehaviour
    {
        /// <summary>
        /// 销毁时间
        /// </summary>
        private const float destroyTime = 0.75f;
        /// <summary>
        /// 伤害数值
        /// </summary>
        private Transform parent; // 父元素
        private float offsetX; // 偏移量
        private Text content; // 内容
        private List<_Param> param;

        private void Awake()
        {
            content = transform.Find("Text").GetComponent<Text>();
            if (content == null)
            {
                LogManager.Instance.log("content Not Found!!!", LogManager.LogLevel.Error);
                return;
            }
            offsetX = Random.Range(-0.2f, 0.2f);
            // 不能在Start中
            param = new List<_Param>();
            param.Add(new _Param(Color.white, 40));
            param.Add(new _Param(Color.red, 50));
        }

        void Start()
        {
            // 由于transform，不能放到Awake中
            parent = transform.parent;
            if (parent == null)
            {
                LogManager.Instance.log("parent Not Found!!!", LogManager.LogLevel.Error);
                return;
            }
            Destroy(gameObject, destroyTime);
        }

        void Update()
        {
            // 不随父元素旋转而旋转
            transform.rotation = Quaternion.identity;
            // 不随父元素旋转而移动(通过世界坐标偏移量实现)
            transform.position = new Vector3(parent.position.x + offsetX, transform.position.y, 0);
            transform.Translate(2.0f * Time.deltaTime * Vector3.up, Space.World); // 使文本在垂直方向山产生一个偏移
        }

        /// <summary>
        /// 设置伤害数值
        /// </summary>
        /// <param name="value">数值</param>
        public void setDamage(float value, int colorIndex = 0) {
            content.text = ((float)System.Math.Round(value, 1)).ToString();
            content.color = param[colorIndex].color;
            content.fontSize = param[colorIndex].fontSize;
        }

        // 内部类
        public class _Param
        {
            public Color color;
            public int fontSize;

            public _Param(Color color,int fontSize)
            {
                this.color = color;
                this.fontSize = fontSize;
            }
        }
    }
}