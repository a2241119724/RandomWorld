using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class Apple : Food
    {
        public Apple()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("Apple");
        }
    }

    public class AppleObject : FoodObject
    {
        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();
        }

        public override void eat()
        {
            throw new NotImplementedException();
        }
    }
}