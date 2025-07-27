namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 农田
    /// </summary>
    [Serializable]
    public class Farmland : RoomItem
    {
        private readonly Wall soil;

        public Farmland()
        {
            this.Width = 4;
            this.Height = 3;
            this.soil = new FarmlandWall();
        }

        /// <inheritdoc/>
        public override void AddBuildTask(Vector3Int centerMap)
        {
            int[] x_B = this.GetXBoundary(centerMap);
            int[] y_B = this.GetYBoundary(centerMap);
            for (int i = x_B[0]; i < x_B[1] + 1; i++)
            {
                for (int j = y_B[0]; j < y_B[1] + 1; j++)
                {
                    BuildMap.Instance.DirectBuild(new Vector3Int(i, j, 0), this.soil.Tile);
                }
            }

            BuildMap.Instance.AddTask();

            // 添加仓库Cell
            FarmlandManager.Instance.AddCells(Tool.Add(centerMap, -this.Height / 2, -this.Width / 2), this.Width, this.Height);
        }
    }
}
