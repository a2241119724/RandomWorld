namespace LAB2D.Item.Build
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Constant;
    using System;
    using UnityEngine;

    /// <summary>
    /// 建造物品。
    /// 可直接实例化用于简单建造物品，也可被子类化用于有特殊行为的物品(如房间、床)。
    /// </summary>
    [Serializable]
    public class ABuildItem : AItem
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

        /// <summary>
        /// 无参构造器（供子类和反射使用）。
        /// TileName 默认为类名。
        /// </summary>
        public ABuildItem()
        {
            this.TileName = this.GetType().Name;
        }

        /// <summary>
        /// 带瓦片名的构造器（供直接实例化使用）。
        /// </summary>
        /// <param name="tileName">瓦片名称</param>
        public ABuildItem(string tileName)
        {
            this.TileName = tileName;
        }

        /// <summary>
        /// 墙的方向
        /// </summary>
        public enum WallDirectionEnum
        {
            /// <summary>上</summary>
            TOP,

            /// <summary>下</summary>
            DOWN,

            /// <summary>左</summary>
            LEFT,

            /// <summary>右</summary>
            RIGHT,

            /// <summary>右上</summary>
            RIGHT_TOP,

            /// <summary>右下</summary>
            RIGHT_DOWN,

            /// <summary>左上</summary>
            LEFT_TOP,

            /// <summary>左下</summary>
            LEFT_DOWN,
        }

        /// <summary>
        /// 添加建造任务
        /// </summary>
        /// <param name="centerMap">位置</param>
        /// <param name="extra">额外信息</param>
        /// <param name="priority">任务优先级，默认系统默认</param>
        public virtual void AddBuildTask(Vector3Int centerMap, Extra extra, int priority = WorkerTaskPriority.SystemDefault)
        {
            Core.ServiceLocator.Get<Map.BuildMap>().AddBuild(centerMap, this.TileName, priority);
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
