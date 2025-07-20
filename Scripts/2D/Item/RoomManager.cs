namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 房间管理
    /// </summary>
    public class RoomManager : Singleton<RoomManager>
    {
        private static Dictionary<string, RoomInfo> rooms = new Dictionary<string, RoomInfo>();
        private int layerMask = LayerMask.GetMask("BuildTile"); // Door(Default)

        /// <summary>
        /// 添加房间
        /// </summary>
        /// <param name="name">名字</param>
        /// <param name="roomInfo">房间信息</param>
        public void AddRoom(string name, RoomInfo roomInfo)
        {
            if (rooms.ContainsKey(name))
            {
                LogManager.Instance.Log("已经有房间了", LogManager.LogLevel.Error);
            }

            rooms.Add(name, roomInfo);
        }

        /// <summary>
        /// 建造完成回调
        /// </summary>
        /// <param name="posMap">位置</param>
        public void Complete(Vector3Int posMap)
        {
            foreach (KeyValuePair<string, RoomInfo> room in rooms)
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

        /// <summary>
        /// 根据位置获取房间
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>房间信息</returns>
        public RoomInfo GetRoomByPos(Vector3Int posMap)
        {
            if (rooms.Count == 0)
            {
                return null;
            }

            RaycastHit2D hitR = Physics2D.Raycast(TileMap.Instance.MapPosToWorldPos(posMap), Vector3.right, 1000.0f, this.layerMask);
            RaycastHit2D hitL = Physics2D.Raycast(TileMap.Instance.MapPosToWorldPos(posMap), Vector3.left, 1000.0f, this.layerMask);
            RaycastHit2D hitT = Physics2D.Raycast(TileMap.Instance.MapPosToWorldPos(posMap), Vector3.up, 1000.0f, this.layerMask);
            RaycastHit2D hitD = Physics2D.Raycast(TileMap.Instance.MapPosToWorldPos(posMap), Vector3.down, 1000.0f, this.layerMask);
            int count = 0;
            Vector3Int posMap1 = default;
            if (hitR.collider != null)
            {
                posMap1 = TileMap.Instance.WorldPosToMapPos(new Vector3(hitR.point.x + 0.5f, hitR.point.y));
                TileBase tileBase = BuildMap.Instance.GetTile(posMap1);
                if (tileBase != null)
                {
                    count += tileBase.name.Equals("WallR") ? 1 : 0;
                }
            }

            if (hitL.collider != null)
            {
                posMap1 = TileMap.Instance.WorldPosToMapPos(new Vector3(hitL.point.x - 0.5f, hitL.point.y));
                TileBase tileBase = BuildMap.Instance.GetTile(posMap1);
                if (tileBase != null)
                {
                    count += tileBase.name.Equals("WallL") ? 1 : 0;
                }
            }

            if (hitT.collider != null)
            {
                posMap1 = TileMap.Instance.WorldPosToMapPos(new Vector3(hitT.point.x, hitT.point.y + 0.5f));
                TileBase tileBase = BuildMap.Instance.GetTile(posMap1);
                if (tileBase != null)
                {
                    count += tileBase.name.Equals("WallT") ? 1 : 0;
                }
            }

            if (hitD.collider != null)
            {
                posMap1 = TileMap.Instance.WorldPosToMapPos(new Vector3(hitD.point.x, hitD.point.y - 0.5f));
                TileBase tileBase = BuildMap.Instance.GetTile(posMap1);
                if (tileBase != null)
                {
                    count += tileBase.name.Equals("WallD") ? 1 : 0;
                }
            }

            // 只要有两面是正确的就认为在房间中
            if (count >= 2)
            {
                foreach (KeyValuePair<string, RoomInfo> room in rooms)
                {
                    if (room.Value.Progress == 0 && room.Value.Points.Contains(posMap1))
                    {
                        return room.Value;
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// 房间信息
    /// </summary>
    public class RoomInfo
    {
        /// <summary>
        /// 所有的墙与门的位置
        /// </summary>
        public List<Vector3Int> Points;

        /// <summary>
        /// 0表示已经完成
        /// </summary>
        public int Progress;

        /// <summary>
        /// 温度
        /// </summary>
        public float Temperature;

        /// <summary>
        /// 湿度
        /// </summary>
        public float Humidity;

        public RoomInfo()
        {
            this.Points = new List<Vector3Int>();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"温度:{this.Temperature}\n" +
                $"湿度:{this.Humidity}\n" +
                $"进度:{this.Progress}";
        }
    }
}
