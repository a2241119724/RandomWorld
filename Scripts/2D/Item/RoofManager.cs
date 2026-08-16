namespace LAB2D.Item
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 屋顶管理 — Worker 房间创建完成后生成覆盖整个房间矩形的屋顶。
    /// 本地玩家进入房间内部时隐藏屋顶（看得到屋内），离开房间后恢复显示。
    /// 屋顶物体挂 All/Building 下，sortingLayer=Highest（盖住房间内一切）。
    /// 跟随 WorldYSortManager 的懒创建单例模式，自带 Update 做玩家位置检测。
    /// </summary>
    public class RoofManager : MonoBehaviour
    {
        /// <summary>屋顶图片名（Resources/Images/Item/Build/Roof.png）。</summary>
        private const string RoofSpriteName = "Roof";

        /// <summary>
        /// Roof sprite 原始世界尺寸（1024px / PPU 100 = 10.24 世界单位）。
        /// 房间 5~7 格 → 缩放约 0.49~0.68 覆盖房间矩形。
        /// </summary>
        private const float RoofWorldSize = 10.24f;

        /// <summary>房间 → 屋顶渲染器（以 RoomInfo 引用为 key，与 RoomManager 实例对齐）。</summary>
        private readonly Dictionary<RoomInfo, SpriteRenderer> roofs = new Dictionary<RoomInfo, SpriteRenderer>();

        // Update 每帧用到的服务引用，懒缓存避免每帧 ServiceLocator 查询
        private PlayerManager playerManager;
        private TileMap tileMap;
        private RoomManager roomManager;

        private static RoofManager instance;

        /// <summary>
        /// 获取单例；不存在则懒创建。
        /// </summary>
        public static RoofManager Ensure()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindObjectOfType<RoofManager>();
            if (instance == null)
            {
                GameObject go = new GameObject("RoofManager");
                instance = go.AddComponent<RoofManager>();
                DontDestroyOnLoad(go);
            }

            return instance;
        }

        /// <summary>
        /// 房间创建完成后生成覆盖整个房间矩形的屋顶。
        /// </summary>
        /// <param name="room">已注册的房间（引用作为关联 key）。</param>
        /// <param name="center">房间中心（地图坐标）。</param>
        /// <param name="roomWidth">房间外墙宽度（tile 格数）。</param>
        /// <param name="roomHeight">房间外墙高度（tile 格数）。</param>
        /// <param name="ownerName">房间所有者名称（用于屋顶物体命名）。</param>
        public void AddRoof(RoomInfo room, Vector3Int center, int roomWidth, int roomHeight, string ownerName)
        {
            if (room == null || this.roofs.ContainsKey(room))
            {
                return;
            }

            Sprite roofSprite = Core.ServiceLocator.Get<ResourceManager>().GetImage(RoofSpriteName);
            if (roofSprite == null)
            {
                AWorkerTask.LogProvider(
                    $"[BuildDiag] 屋顶生成失败: Sprite 'Roof' 未找到, 请确认 Resources/Images/Item/Build/Roof.png",
                    LogManager.LogLevelEnum.Error);
                return;
            }

            // All/Building 层级（参考 ShopNPCGenerator.SpawnAll）
            Transform buildingParent = GameObject.Find("All/Building")?.transform;
            if (buildingParent == null)
            {
                buildingParent = new GameObject("Building").transform;
                GameObject all = GameObject.Find("All");
                if (all != null)
                {
                    buildingParent.SetParent(all.transform);
                }
            }

            GameObject roofGo = new GameObject($"Roof_{ownerName}");
            roofGo.transform.SetParent(buildingParent, false);

            TileMap tileMap = this.tileMap != null ? this.tileMap : (this.tileMap = Core.ServiceLocator.Get<TileMap>());
            roofGo.transform.position = tileMap.MapPosToWorldPos(center);

            SpriteRenderer sr = roofGo.AddComponent<SpriteRenderer>();
            sr.sprite = roofSprite;
            sr.sortingLayerName = "Highest";
            sr.sortingOrder = 0;

            // 世界坐标 = tile 坐标 45° 转置：tile 宽(roomWidth)→世界高、tile 高(roomHeight)→世界宽。
            // sprite 原始世界尺寸 RoofWorldSize×RoofWorldSize，缩放后正好覆盖房间矩形。
            roofGo.transform.localScale = new Vector3(
                roomHeight / RoofWorldSize,
                roomWidth / RoofWorldSize,
                1f);

            this.roofs.Add(room, sr);

            AWorkerTask.LogProvider(
                $"[BuildDiag] 屋顶已生成: {ownerName} 中心=({center.x},{center.y}) 尺寸={roomWidth}x{roomHeight} " +
                $"pos=({roofGo.transform.position.x:F1},{roofGo.transform.position.y:F1}) scale=({roofGo.transform.localScale.x:F2},{roofGo.transform.localScale.y:F2})",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 房间失效时移除其屋顶（房间边界建筑被拆除/房间被移除时调用）。
        /// 幂等：屋顶不存在则直接返回。
        /// </summary>
        /// <param name="room">要移除屋顶的房间。</param>
        public void RemoveRoof(RoomInfo room)
        {
            if (room == null || !this.roofs.TryGetValue(room, out SpriteRenderer sr))
            {
                return;
            }

            this.roofs.Remove(room);
            if (sr != null)
            {
                Object.Destroy(sr.gameObject);
            }
        }

        private void Update()
        {
            // 懒清扫已销毁的屋顶渲染器（场景重载/屋顶被外部销毁时防御）
            if (this.roofs.Count > 0)
            {
                List<RoomInfo> removed = null;
                foreach (KeyValuePair<RoomInfo, SpriteRenderer> kv in this.roofs)
                {
                    if (kv.Value == null)
                    {
                        if (removed == null)
                        {
                            removed = new List<RoomInfo>();
                        }

                        removed.Add(kv.Key);
                    }
                }

                if (removed != null)
                {
                    foreach (RoomInfo key in removed)
                    {
                        this.roofs.Remove(key);
                    }
                }
            }

            if (this.roofs.Count == 0)
            {
                return;
            }

            if (this.playerManager == null)
            {
                this.playerManager = Core.ServiceLocator.Get<PlayerManager>();
            }

            Player mine = this.playerManager != null ? this.playerManager.Mine : null;
            if (mine == null)
            {
                return;
            }

            if (this.tileMap == null)
            {
                this.tileMap = Core.ServiceLocator.Get<TileMap>();
            }

            if (this.roomManager == null)
            {
                this.roomManager = Core.ServiceLocator.Get<RoomManager>();
            }

            Vector3Int playerMapPos = this.tileMap.WorldPosToMapPos(mine.transform.position);
            RoomInfo playerRoom = this.roomManager.GetRoomInterior(playerMapPos);

            // 进出房间是事件点：仅当可见性需要切换时打日志，绝不在每帧打
            // 隐藏条件：玩家在房间内部（GetRoomInterior），或站在该房间墙壁/门格上（Points 含墙）
            foreach (KeyValuePair<RoomInfo, SpriteRenderer> kv in this.roofs)
            {
                bool inRoom = ReferenceEquals(kv.Key, playerRoom)
                    || (kv.Key.Points != null && kv.Key.Points.Contains(playerMapPos));
                bool wantEnabled = !inRoom;
                if (kv.Value.enabled != wantEnabled)
                {
                    kv.Value.enabled = wantEnabled;
                    AWorkerTask.LogProvider(
                        $"[BuildDiag] 屋顶{(wantEnabled ? "显示" : "隐藏")}: {kv.Value.name} " +
                        $"player=({playerMapPos.x},{playerMapPos.y})",
                        LogManager.LogLevelEnum.Debug);
                }
            }
        }
    }
}
