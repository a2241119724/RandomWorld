namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 建造
    /// </summary>
    [Serializable]
    public abstract class ABuildItem : AItem
    {
        /// <summary>
        /// 宽度
        /// </summary>
        public int Width = 1;

        /// <summary>
        /// 高度
        /// </summary>
        public int Height = 1;

        /// <summary>
        /// 是否左下，对于大于1*1的建造物，鼠标所在的位置
        /// </summary>
        public AWorkerTask.RectType RectType = AWorkerTask.RectType.Center;

        /// <summary>
        /// 是否可以自定义尺寸
        /// </summary>
        public bool IsCustomSize = false;

        /// <summary>
        /// 瓦片名称
        /// </summary>
        public string TileName;

        public ABuildItem()
        {
            this.TileName = this.GetType().Name;
        }

        /// <summary>
        /// 添加建造任务
        /// </summary>
        /// <param name="centerMap">位置</param>
        /// <param name="extra">额外信息</param>
        public virtual void AddBuildTask(Vector3Int centerMap, Extra extra)
        {
            BuildMap.Instance.AddBuild(centerMap, this.TileName);
        }

        public class Extra
        {
            /// <summary>
            /// TopLeft自定义大小需要
            /// </summary>
            public int Width = 1;

            /// <summary>
            /// TopLeft自定义大小需要
            /// </summary>
            public int Height = 1;

            public AWorkerTask.RectType RectType = AWorkerTask.RectType.Center;

            public Extra(int width, int height, AWorkerTask.RectType rectType)
            {
                this.Width = width;
                this.Height = height;
                this.RectType = rectType;
            }
        }
    }
}
