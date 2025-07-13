using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class GatherMap : BaseTileMap
    {
        public static GatherMap Instance { get; private set; }


        protected override void Awake()
        {
            base.Awake();
            Instance = this;
        }

        public void addGather(Vector3Int posMap)
        {
            tilemap.SetTile(posMap, (TileBase)ResourcesManager.Instance.GetAsset("Gather"));
        }

        public void cancelGather(Vector3Int posMap)
        {
            tilemap.SetTile(posMap, null);
        }
    }
}
