using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class GatherMap : MonoBehaviour
    {
        public static GatherMap Instance { get; private set; }

        private Tilemap gatherMap; 

        private void Awake()
        {
            Instance = this;
            gatherMap = gameObject.GetComponent<Tilemap>();
        }

        public void addGather(Vector3Int posMap)
        {
            gatherMap.SetTile(posMap,(TileBase)ResourcesManager.Instance.getAsset("Gather"));
        }

        public void cancelGather(Vector3Int posMap)
        {
            gatherMap.SetTile(posMap, null);
        }
    }
}
