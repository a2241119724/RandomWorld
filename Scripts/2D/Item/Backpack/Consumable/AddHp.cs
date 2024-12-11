using System;

namespace LAB2D
{
    [Serializable]
    public class AddHp : Consumable { 
    }

    public class AddHpObject : ConsumableObject
    {
        public float value;

        protected override void Awake()
        {
            base.Awake();
            name = "AddHp";
        }

        public override void use()
        {
            PlayerManager.Instance.Mine.addHp(value);
        }
    }
}