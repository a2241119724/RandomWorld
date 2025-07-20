namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 自定义盾牌
    /// </summary>
    [Serializable]
    public class CustomShield : Shield
    {
        public CustomShield()
        {
            this.Tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomShield");
        }
    }

    /// <summary>
    /// 自定义盾牌对象
    /// </summary>
    public class CustomShieldObject : ShieldObject
    {
    }
}
