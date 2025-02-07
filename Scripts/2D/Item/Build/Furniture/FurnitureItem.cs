using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public abstract class FurnitureItem : BuildItem
    {
        [NonSerialized]
        public TileBase tile;

        public FurnitureItem()
        {
            width = 1;
            height = 1;
        }
    }

        
    public abstract class FurnitureItemObject : BuildItemObject { 
    
    }
}
