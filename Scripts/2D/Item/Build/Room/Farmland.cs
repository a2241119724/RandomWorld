namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 农田
    /// </summary>
    [Serializable]
    public class Farmland : ARoom
    {
        private readonly AWall soil;

        public Farmland()
        {
            this.Width = 4;
            this.Height = 3;
            this.soil = new FarmlandWall();
        }

        /// <inheritdoc/>
        public override void AddBuildTask(Vector3Int centerMap, Extra extra)
        {
            int[] boundary = this.GetBoundary(centerMap, extra);
            if (!this.CheckBoundary(boundary))
            {
                return;
            }

            for (int i = boundary[0]; i < boundary[1] + 1; i++)
            {
                for (int j = boundary[2]; j < boundary[3] + 1; j++)
                {
                    BuildMap.Instance.AddBuild(new Vector3Int(i, j, 0), this.soil.TileName);
                }
            }

            BuildMap.Instance.AddTask();

            // 添加仓库Cell
            FarmlandManager.Instance.AddCells(new Vector3Int(boundary[0], boundary[2]), boundary[3] - boundary[2], boundary[1] - boundary[0]);
        }
    }
}
