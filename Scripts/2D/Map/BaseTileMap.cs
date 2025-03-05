using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public abstract class BaseTileMap : AMonoSaveData
    {
        protected Tilemap tilemap;
        public static int Height { set; get; } // 地图纵向长度
        public static int Width { set; get; }  // 地图横向长度

        protected virtual void Awake()
        {
            tilemap = GetComponent<Tilemap>();
        }

        public virtual TileBase getTile(Vector3Int pos)
        {
            return tilemap.GetTile(pos);
        }

        public virtual void setTile(Vector3Int pos, TileBase tileBase)
        {
            tilemap.SetTile(pos, tileBase);
        }

        /// <summary>
        /// 判断该坐标是否可用
        /// </summary>
        /// <param name="posMap"></param>
        /// <returns></returns>
        public virtual bool isFreeTile(Vector3Int posMap)
        {
            return tilemap.GetTile(posMap) == null;
        }

        /// <summary>
        /// 判断该坐标是否没有碰撞体
        /// </summary>
        /// <param name="posMap"></param>
        /// <returns></returns>
        public virtual bool isCanReach(Vector3Int posMap)
        {
            return tilemap.GetColliderType(posMap) == Tile.ColliderType.None;
        }
    }
}
