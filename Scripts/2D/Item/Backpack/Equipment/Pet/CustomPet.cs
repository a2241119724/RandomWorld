namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 自定义宠物
    /// </summary>
    [Serializable]
    public abstract class CustomPet : Pet
    {
        public CustomPet()
        {
            this.Tile = (TileBase)ResourceManager.Instance.GetAsset("CustomPet");
        }
    }

    /// <summary>
    /// 自定义宠物类型
    /// </summary>
    public abstract class CustomPetObject : PetObject
    {
    }
}
