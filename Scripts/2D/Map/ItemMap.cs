using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class ItemMap : MonoBehaviour
    {
        public static ItemMap Instance { private set; get; }
        public Tilemap ItemTileMap { get; set; }

        private void Awake()
        {
            Instance = this;
            ItemTileMap = GetComponent<Tilemap>();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            Vector3Int posMap = TileMap.Instance.worldPosToMapPos(collision.transform.position);
            TileBase tile = ItemTileMap.GetTile(posMap);
            if(tile != null)
            {
                BackpackController.Instance.addItem(ItemFactory.Instance.getItemByName(ItemTileMap.GetTile(posMap).name));
                ItemTileMap.SetTile(posMap, null);
            }
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    posMap = new Vector3Int(posMap.x + i, posMap.y + j, 0);
                    tile = ItemTileMap.GetTile(posMap);
                    if (tile != null)
                    {
                        BackpackController.Instance.addItem(ItemFactory.Instance.getItemByName(ItemTileMap.GetTile(posMap).name));
                        ItemTileMap.SetTile(posMap, null);
                    }
                }
            }
        }
    }
}
