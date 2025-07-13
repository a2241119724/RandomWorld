using System;
using UnityEngine;

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
