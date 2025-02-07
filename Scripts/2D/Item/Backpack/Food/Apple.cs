using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    [Serializable]
    public class Apple : Food
    {
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