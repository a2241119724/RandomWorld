using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    [Serializable]
    public abstract class Food : BackpackItem
    {
    }

    public abstract class FoodObject : BackpackItemObject
    {
        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();
        }

        public abstract void eat();
    }
}
