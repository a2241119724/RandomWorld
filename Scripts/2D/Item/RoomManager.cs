using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class RoomManager : Singleton<RoomManager>
    {
        private static Dictionary<string, RoomInfo> rooms = new Dictionary<string, RoomInfo>();
        /// <summary>
        /// Door(Default)
        /// </summary>
        private int layerMask = LayerMask.GetMask("BuildTile");

        public void addRoom(string name, RoomInfo roomInfo) {
            if (rooms.ContainsKey(name))
            {
                Debug.LogError("已经有房间了");
            }
            rooms.Add(name, roomInfo);
        }

        public void complete(Vector3Int posMap) { 
            foreach(KeyValuePair<string, RoomInfo> room in rooms)
            {
                if (room.Value.Progress != 0)
                {
                    room.Value.Progress -= room.Value.Points.Contains(posMap) ? 1 : 0;
                    if (room.Value.Progress == 0)
                    {
                        room.Value.Temperature = 25.0f;
                        room.Value.Humidity = 25.0f;
                    }
                }
            }
        }

        public RoomInfo getRoomByPos(Vector3Int posMap) {
            if (rooms.Count == 0) return null;
            RaycastHit2D hitR = Physics2D.Raycast(TileMap.Instance.mapPosToWorldPos(posMap), Vector3.right, 1000.0f, layerMask);
            RaycastHit2D hitL = Physics2D.Raycast(TileMap.Instance.mapPosToWorldPos(posMap), Vector3.left, 1000.0f, layerMask);
            RaycastHit2D hitT = Physics2D.Raycast(TileMap.Instance.mapPosToWorldPos(posMap), Vector3.up, 1000.0f, layerMask);
            RaycastHit2D hitD = Physics2D.Raycast(TileMap.Instance.mapPosToWorldPos(posMap), Vector3.down, 1000.0f, layerMask);
            int count = 0;
            Vector3Int _posMap = default;
            if (hitR.collider != null)
            {
                _posMap = TileMap.Instance.worldPosToMapPos(new Vector3(hitR.point.x + 0.5f, hitR.point.y));
                TileBase tileBase = BuildMap.Instance.getTile(_posMap);
                if (tileBase != null)
                {
                    count += tileBase.name.Equals("WallR") ? 1 : 0;
                }
            }
            if (hitL.collider != null)
            {
                _posMap = TileMap.Instance.worldPosToMapPos(new Vector3(hitL.point.x - 0.5f, hitL.point.y));
                TileBase tileBase = BuildMap.Instance.getTile(_posMap);
                if (tileBase != null)
                {
                    count += tileBase.name.Equals("WallL") ? 1 : 0;
                }
            }
            if (hitT.collider != null)
            {
                _posMap = TileMap.Instance.worldPosToMapPos(new Vector3(hitT.point.x, hitT.point.y + 0.5f));
                TileBase tileBase = BuildMap.Instance.getTile(_posMap);
                if (tileBase != null)
                {
                    count += tileBase.name.Equals("WallT") ? 1 : 0;
                }

            }
            if (hitD.collider != null)
            {
                _posMap = TileMap.Instance.worldPosToMapPos(new Vector3(hitD.point.x, hitD.point.y - 0.5f));
                TileBase tileBase = BuildMap.Instance.getTile(_posMap);
                if (tileBase != null)
                {
                    count += tileBase.name.Equals("WallD") ? 1 : 0;
                }
            }
            // 只要有两面是正确的就认为在房间中
            if(count >= 2)
            {
                foreach (KeyValuePair<string, RoomInfo> room in rooms)
                {
                    if (room.Value.Progress == 0 && room.Value.Points.Contains(_posMap))
                    {
                        return room.Value;
                    }
                }
            }
            return null;
        }
    }

    public class RoomInfo {
        /// <summary>
        /// 所有的墙与门的位置
        /// </summary>
        public List<Vector3Int> Points;
        /// <summary>
        /// 0表示已经完成
        /// </summary>
        public int Progress;
        public float Temperature;
        public float Humidity;

        public RoomInfo() {
            Points = new List<Vector3Int>();
        }

        public override string ToString()
        {
            return $"温度:{Temperature}\n" +
                $"湿度:{Humidity}\n" + 
                $"进度:{Progress}";
        }
    }
}
