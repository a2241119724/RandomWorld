namespace LAB2D
{
    using System;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 建造
    /// </summary>
    [Serializable]
    public abstract class BuildItem : Item
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
        public bool IsBottomLeft = false;

        /// <summary>
        /// 瓦片名称
        /// </summary>
        public string TileName;

        public BuildItem()
        {
            this.TileName = this.GetType().Name;
        }

        /// <summary>
        /// 添加建造任务
        /// </summary>
        /// <param name="centerMap">位置</param>
        public virtual void AddBuildTask(Vector3Int centerMap)
        {
            BuildMap.Instance.AddBuild(centerMap, this.TileName);
        }
    }

    /// <summary>
    /// 建造对象
    /// </summary>
    public abstract class BuildItemObject : ItemObject
    {
    }
}
