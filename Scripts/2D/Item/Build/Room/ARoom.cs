namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 房间数据
    /// </summary>
    [Serializable]
    public abstract class ARoom : ABuildItem
    {
        /// <summary>
        /// 房间的所有墙
        /// </summary>
        public Dictionary<AWall.WallDirectionEnum, AWall> Walls;

        /// <inheritdoc/>
        public override void AddBuildTask(Vector3Int centerMap)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 获得XY轴边界
        /// </summary>
        /// <param name="centerMap">中心位置</param>
        /// <returns>坐标</returns>
        public int[] GetBoundary(Vector3Int centerMap)
        {
            return new int[]
            {
                centerMap.x - (this.Height / 2), centerMap.x + this.Height - 1 - (this.Height / 2),
                centerMap.y - (this.Width / 2), centerMap.y + this.Width - 1 - (this.Width / 2),
            };
        }
    }
}
