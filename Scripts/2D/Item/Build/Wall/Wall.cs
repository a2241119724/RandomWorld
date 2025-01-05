using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class Wall : BuildItem
    {
        [NonSerialized]
        public TileBase tile;

        public Wall()
        {
        }
    }

    [Serializable]
    public class WallT : Wall
    {
        public WallT()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("WallT");
        }
    }

    [Serializable]
    public class WallD : Wall
    {
        public WallD()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("WallD");
        }
    }

    [Serializable]
    public class WallL : Wall
    {
        public WallL()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("WallL");
        }
    }

    [Serializable]
    public class WallR : Wall
    {
        public WallR()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("WallR");
        }
    }

    [Serializable]
    public class WallRT : Wall
    {
        public WallRT()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("WallRT");
        }
    }

    [Serializable]
    public class WallRD : Wall
    {
        public WallRD()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("WallRD");
        }
    }

    [Serializable]
    public class WallLT : Wall
    {
        public WallLT()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("WallLT");
        }
    }

    [Serializable]
    public class WallLD : Wall
    {
        public WallLD()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("WallLD");
        }
    }

    public class WallObject : BuildItemObject
    {
    }
}
