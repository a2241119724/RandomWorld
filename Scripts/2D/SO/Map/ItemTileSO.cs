namespace LAB2D
{
    using UnityEngine;
    using UnityEngine.Tilemaps;

    [CreateAssetMenu(fileName = "New ItemTileSO", menuName = "SO/Tile/ItemTileSO")]
    public class ItemTileSO : TileBase
    {
        /// <summary>
        /// 物品ID
        /// </summary>
        public int ItemId;

        public void OnEnable()
        {
            Debug.Log(ItemDataManager.Instance.GetByName(this.name));
        }
    }
}
