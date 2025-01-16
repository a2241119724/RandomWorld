using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    [Serializable]
    public abstract class BuildItem : Item
    {
        public abstract void addBuildTask(Vector3Int centerMap, int width = 10, int height = 7);
    }

    public abstract class BuildItemObject : ItemObject
    {
    }
}

