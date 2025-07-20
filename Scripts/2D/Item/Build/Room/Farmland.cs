namespace LAB2D
{
    using System;
    using UnityEngine;

    [Serializable]
    public class Farmland : RoomItem
    {
        private Wall soil;

        public Farmland()
        {
            this.Width = 4;
            this.Height = 3;
            this.soil = new FarmlandWall();
        }

        public override void AddBuildTask(Vector3Int centerMap)
        {
            int[] xB = this.getXBoundary(centerMap);
            int[] yB = this.getYBoundary(centerMap);
            for (int i = xB[0]; i < xB[1] + 1; i++)
            {
                for (int j = yB[0]; j < yB[1] + 1; j++)
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
