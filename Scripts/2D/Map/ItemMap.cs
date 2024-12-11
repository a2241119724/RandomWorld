using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class ItemMap : MonoBehaviour
    {
        public static ItemMap Instance { get; set; }
        public Tilemap ItemTileMap { get; set; }

        private void Awake()
        {
            Instance = this;
            ItemTileMap = GetComponent<Tilemap>();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            int x = Mathf.RoundToInt(collision.transform.position.x);
            int y = Mathf.RoundToInt(collision.transform.position.y);
            Vector3Int pos = new Vector3Int(y, x, 0);
            TileBase tile = ItemTileMap.GetTile(pos);
            if(tile != null)
            {
                BackpackController.Instance.addItem(ItemFactory.Instance.getByName(ItemTileMap.GetTile(pos).name));
                ItemTileMap.SetTile(pos, null);
            }
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    pos = new Vector3Int(y + i, x + j, 0);
                    tile = ItemTileMap.GetTile(pos);
                    if (tile != null)
                    {
                        BackpackController.Instance.addItem(ItemFactory.Instance.getByName(ItemTileMap.GetTile(pos).name));
                        ItemTileMap.SetTile(pos, null);
                    }
                }
            }
        }
    }
}
