using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class Farmland : RoomItem
    {
        private Wall soil;

        public Farmland()
        {
            width = 4;
            height = 3;
            soil = new FarmlandWall();
        }

        public override void addBuildTask(Vector3Int centerMap)
        {
            int[] xB = getXBoundary(centerMap);
            int[] yB = getYBoundary(centerMap);
            for (int i = xB[0]; i < xB[1] + 1; i++)
            {
                for (int j = yB[0]; j < yB[1] + 1; j++)
                {
                    BuildMap.Instance.directBuild(new Vector3Int(i, j, 0), soil.tile);
                }
            }
            BuildMap.Instance.addTask();
            // Ìí¼Ó²Ö¿âCell
            FarmlandManager.Instance.addCells(Tool.add(centerMap, -height / 2, -width / 2), width, height);
        }
    }
}
