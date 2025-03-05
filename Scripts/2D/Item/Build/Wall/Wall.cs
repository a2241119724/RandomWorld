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
        public Wall()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset(this.GetType().Name);
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
