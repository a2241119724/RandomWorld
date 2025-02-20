using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D 
{
    [Serializable]
    public abstract class CustomPet : Pet
    {
        public CustomPet()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomPet");
        }
    }

    public abstract class CustomPetObject : PetObject
    {
    }
}
