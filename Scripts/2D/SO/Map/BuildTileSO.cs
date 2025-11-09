namespace LAB2D
{
    using UnityEngine;
    using UnityEngine.Tilemaps;

    [CreateAssetMenu(fileName = "New BuildTileSO", menuName = "SO/Tile/BuildTileSO")]
    public class BuildTileSO : Tile
    {
        /// <summary>
        /// 地图tile中存储的数据
        /// </summary>
        public BuildItemData Data;
    }
}
