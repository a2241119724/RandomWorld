namespace LAB2D
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 调试 UI
    /// </summary>
    public class DebugUI : MonoBehaviour
    {
        private Text text;

        /// <summary>
        /// 单例
        /// </summary>
        public static DebugUI Instance { get; private set; }

        /// <summary>
        /// 更新信息
        /// </summary>
        /// <param name="text">信息</param>
        public void UpdateInfo(string text)
        {
            this.text.text = text;
        }

        public void Awake()
        {
            this.text = Tool.GetComponentInChildren<Text>(this.gameObject, "Info");
            Instance = this;
        }
    }
}
