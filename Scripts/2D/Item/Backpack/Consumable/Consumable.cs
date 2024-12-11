using System;

namespace LAB2D {
    [Serializable]
    public abstract class Consumable : BackpackItem { 
    }

    public abstract class ConsumableObject : BackpackItemObject
    {
        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();
        }

        public abstract void use();
    }
}