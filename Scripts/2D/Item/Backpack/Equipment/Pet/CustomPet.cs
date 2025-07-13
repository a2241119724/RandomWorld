using System;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public abstract class CustomPet : Pet
    {
        public CustomPet()
        {
            tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomPet");
        }
    }

    public abstract class CustomPetObject : PetObject
    {
    }
}
