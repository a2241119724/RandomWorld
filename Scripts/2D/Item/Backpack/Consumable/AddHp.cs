using System;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class AddHp : Consumable { 
        public AddHp() 
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("AddHp");
        }
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