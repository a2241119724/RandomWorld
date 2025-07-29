namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 自定义门
    /// </summary>
    [Serializable]
    public class CustomDoor : DoorItem
    {
        public CustomDoor()
        {
            this.Tile = (TileBase)ResourceManager.Instance.GetAsset("CustomDoor");
        }
    }
}
