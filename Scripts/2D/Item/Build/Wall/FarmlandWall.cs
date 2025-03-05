using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class FarmlandWall : Wall
    {
        public FarmlandWall()
        {
        }

        public override void addBuildTask(Vector3Int centerMap)
        {
            BuildMap.Instance.directBuild(centerMap, tile).addTask();
        }
    }
}
