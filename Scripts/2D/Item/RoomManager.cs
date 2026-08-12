namespace LAB2D.Item
{
    using LAB2D;
    using LAB2D.Character;
    using LAB2D.Character.Enemy;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 房间管理
    /// </summary>
    public class RoomManager : Singleton<RoomManager>
    {
        private static readonly Dictionary<string, RoomInfo> Rooms = new ();
        private readonly int layerMask = LayerMask.GetMask("BuildTile"); // 门（默认层）

        /// <summary>
        /// 添加房间
        /// </summary>
        /// <param name="name">名字</param>
        /// <param name="roomInfo">房间信息</param>
        public void AddRoom(string name, RoomInfo roomInfo)
        {
            if (Rooms.ContainsKey(name))
            {
                AWorkerTask.LogProvider("已经有房间了", LogManager.LogLevelEnum.Error);
            }

            // 计算房间内部包围盒
            roomInfo.ComputeBounds();
            Rooms.Add(name, roomInfo);
        }

        /// <summary>
        /// 建造完成回调
        /// </summary>
        /// <param name="posMap">位置</param>
        public void Complete(Vector3Int posMap)
        {
            foreach (KeyValuePair<string, RoomInfo> room in Rooms)
            {
                if (room.Value.Progress != 0)
                {
                    room.Value.Progress -= room.Value.Points.Contains(posMap) ? 1 : 0;
                    if (room.Value.Progress == 0)
                    {
                        room.Value.Temperature = 25.0f;
                        room.Value.Humidity = 25.0f;
                        room.Value.ComputeBounds(); // 完成后重新计算包围盒
                    }
                }
            }
        }

        /// <summary>
        /// 获取所有房间的只读字典。
        /// </summary>
        /// <returns>房间名到 RoomInfo 的映射</returns>
        public IReadOnlyDictionary<string, RoomInfo> GetAllRooms() => Rooms;

        /// <summary>
        /// 根据位置获取房间
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>房间信息</returns>
        public RoomInfo GetRoomByPos(Vector3Int posMap)
        {
            if (Rooms.Count == 0)
            {
                return null;
            }

            // 射线检测是否有房间
            RaycastHit2D hitR = Physics2D.Raycast(Core.ServiceLocator.Get<TileMap>().MapPosToWorldPos(posMap), Vector3.right, 1000.0f, this.layerMask);
            RaycastHit2D hitL = Physics2D.Raycast(Core.ServiceLocator.Get<TileMap>().MapPosToWorldPos(posMap), Vector3.left, 1000.0f, this.layerMask);
            RaycastHit2D hitT = Physics2D.Raycast(Core.ServiceLocator.Get<TileMap>().MapPosToWorldPos(posMap), Vector3.up, 1000.0f, this.layerMask);
            RaycastHit2D hitD = Physics2D.Raycast(Core.ServiceLocator.Get<TileMap>().MapPosToWorldPos(posMap), Vector3.down, 1000.0f, this.layerMask);
            int count = 0;
            Vector3Int posMap1 = default;
            if (hitR.collider != null)
            {
                posMap1 = Core.ServiceLocator.Get<TileMap>().WorldPosToMapPos(new Vector3(hitR.point.x + 0.5f, hitR.point.y));
                TileBase tileBase = Core.ServiceLocator.Get<BuildMap>().GetTile(posMap1);
                if (tileBase != null)
                {
                    count += tileBase.name.Contains("Wall") ? 1 : 0;
                }
            }

            if (hitL.collider != null)
            {
                posMap1 = Core.ServiceLocator.Get<TileMap>().WorldPosToMapPos(new Vector3(hitL.point.x - 0.5f, hitL.point.y));
                TileBase tileBase = Core.ServiceLocator.Get<BuildMap>().GetTile(posMap1);
                if (tileBase != null)
                {
                    count += tileBase.name.Contains("Wall") ? 1 : 0;
                }
            }

            if (hitT.collider != null)
            {
                posMap1 = Core.ServiceLocator.Get<TileMap>().WorldPosToMapPos(new Vector3(hitT.point.x, hitT.point.y + 0.5f));
                TileBase tileBase = Core.ServiceLocator.Get<BuildMap>().GetTile(posMap1);
                if (tileBase != null)
                {
                    count += tileBase.name.Contains("Wall") ? 1 : 0;
                }
            }

            if (hitD.collider != null)
            {
                posMap1 = Core.ServiceLocator.Get<TileMap>().WorldPosToMapPos(new Vector3(hitD.point.x, hitD.point.y - 0.5f));
                TileBase tileBase = Core.ServiceLocator.Get<BuildMap>().GetTile(posMap1);
                if (tileBase != null)
                {
                    count += tileBase.name.Contains("Wall") ? 1 : 0;
                }
            }

            // 只要有两面是正确的就认为在房间中
            if (count >= 2)
            {
                foreach (KeyValuePair<string, RoomInfo> room in Rooms)
                {
                    if (room.Value.Progress == 0 && room.Value.Points.Contains(posMap1))
                    {
                        return room.Value;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 检查指定位置是否在某个已完成房间的内部区域（非墙壁/门位置）。
        /// 使用包围盒快速判断，避免每帧射线检测的性能开销。
        /// </summary>
        /// <param name="posMap">地图坐标。</param>
        /// <returns>该位置所在的房间信息，不在任何房间内返回 null。</returns>
        public RoomInfo GetRoomInterior(Vector3Int posMap)
        {
            foreach (KeyValuePair<string, RoomInfo> kv in Rooms)
            {
                RoomInfo room = kv.Value;
                if (room.Progress != 0) continue; // 建造中不限制
                if (!room.IsInterior(posMap)) continue;
                return room;
            }

            return null;
        }

        /// <summary>
        /// 检查角色是否可以进入指定位置的房间内部。
        /// Enemy 不能进入任何 Worker 房间；Worker 只能进入自己的房间。
        /// </summary>
        /// <param name="posMap">目标位置。</param>
        /// <param name="character">要进入的角色。</param>
        /// <returns>是否可以进入。</returns>
        public bool CanCharacterEnter(Vector3Int posMap, Character character)
        {
            RoomInfo room = this.GetRoomInterior(posMap);
            if (room == null) return true; // 不在任何房间内，可以进入

            // 公共房间（OwnerName="玩家"）所有人可进入
            if (room.OwnerName == "玩家") return true;

            // Enemy 不能进入任何私人房间
            if (character is AEnemy) return false;

            // Worker 只能进入自己的房间
            if (character is AWorker && !string.IsNullOrEmpty(room.OwnerName))
            {
                return room.OwnerName == character.name;
            }

            return true;
        }

        /// <summary>
        /// 获取房间所有者名称。
        /// </summary>
        /// <param name="posMap">地图坐标。</param>
        /// <returns>所有者名称，不在房间内返回 null。</returns>
        public string GetRoomOwner(Vector3Int posMap)
        {
            RoomInfo room = this.GetRoomInterior(posMap);
            return room?.OwnerName;
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

        /// <summary>
        /// 房间所有者名称
        /// </summary>
        public string OwnerName;

        /// <summary>房间内部包围盒（不含墙壁），用于快速判断位置是否在房间内部。</summary>
        public int MinX { get; private set; }
        public int MaxX { get; private set; }
        public int MinY { get; private set; }
        public int MaxY { get; private set; }

        public RoomInfo()
        {
            this.Points = new List<Vector3Int>();
        }

        /// <summary>
        /// 根据墙壁/门位置计算房间内部包围盒。
        /// 内部区域 = 墙壁包围盒向内收缩一格。
        /// </summary>
        public void ComputeBounds()
        {
            if (this.Points == null || this.Points.Count == 0) return;

            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            foreach (Vector3Int p in this.Points)
            {
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }

            // 内部区域 = 墙壁向内收缩一格
            this.MinX = minX + 1;
            this.MaxX = maxX - 1;
            this.MinY = minY + 1;
            this.MaxY = maxY - 1;
        }

        /// <summary>
        /// 判断指定位置是否在房间内部（非墙壁/门）。
        /// </summary>
        public bool IsInterior(Vector3Int posMap)
        {
            if (this.Points == null || this.Points.Count == 0) return false;
            if (this.Points.Contains(posMap)) return false; // 墙壁/门位置不算内部
            return posMap.x >= this.MinX && posMap.x <= this.MaxX
                && posMap.y >= this.MinY && posMap.y <= this.MaxY;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"所有者:{this.OwnerName}\n" +
                $"温度:{this.Temperature}\n" +
                $"湿度:{this.Humidity}\n" +
                $"进度:{this.Progress}";
        }
    }
}

