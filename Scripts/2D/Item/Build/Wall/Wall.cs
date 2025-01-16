using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public abstract class Wall : BuildItem
    {
        [NonSerialized]
        public TileBase tile;

        public Wall()
        {
        }

        public override void addBuildTask(Vector3Int centerMap, int width = 10, int height = 7)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public abstract class WallT : Wall
    {
    }

    [Serializable]
    public abstract class WallD : Wall
    {
    }

    [Serializable]
    public abstract class WallL : Wall
    {
    }

    [Serializable]
    public abstract class WallR : Wall
    {
    }

    [Serializable]
    public abstract class WallRT : Wall
    {
    }

    [Serializable]
    public abstract class WallRD : Wall
    {
    }

    [Serializable]
    public abstract class WallLT : Wall
    {
    }

    [Serializable]
    public abstract class WallLD : Wall
    {
    }

    public class WallObject : BuildItemObject
    {
    }
}
