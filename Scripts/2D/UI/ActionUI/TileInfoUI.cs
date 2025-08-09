namespace LAB2D
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 鼠标地图Tile信息
    /// </summary>
    public class TileInfoUI : MonoBehaviourInit
    {
        private Text content;

        /// <summary>
        /// 单例
        /// </summary>
        public static TileInfoUI Instance { get; private set; }

        public void Awake()
        {
            Instance = this;
            this.content = this.transform.Find("Content").GetComponent<Text>();
        }

        /// <summary>
        /// 设置TileUI的位置
        /// </summary>
        /// <param name="worldPos">位置</param>
        public void SetPostion(Vector3 worldPos)
        {
            worldPos.z = 0;
            this.transform.position = worldPos;
        }

        /// <summary>
        /// 设置TileUI的显示内容
        /// </summary>
        /// <param name="content">内容</param>
        public void SetContent(string content)
        {
            this.content.text = content;
        }

        /// <inheritdoc/>
        public override void Init()
        {
            base.Init();
            this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
        }
    }
}
