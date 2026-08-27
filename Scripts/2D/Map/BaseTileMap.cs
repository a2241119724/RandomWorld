namespace LAB2D.Map
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Data;
    using LAB2D.Network;
    using LAB2D.UnityAdapter;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 基地图。
    /// 通过 ISyncSender 解耦网络同步，不再直接持有 PhotonView。
    /// PhotonView 仅保留用于 [PunRPC] 方法接收端。
    /// </summary>
    public abstract class BaseTileMap : AMonoSaveData
    {
        protected Tilemap tilemap;

        /// <summary>
        /// 网络同步发送器。在线为 PunSyncSender，离线为 NullSyncSender。
        /// </summary>
        protected ISyncSender SyncSender { get; private set; }

        public PhotonView PhotonView { get; set; }

        private TileInfoCoordinator tileInfoCoordinator;

        private TileInfoCoordinator TileInfo
        {
            get
            {
                if (this.tileInfoCoordinator == null)
                {
                    if (!ServiceLocator.TryGet(out this.tileInfoCoordinator))
                    {
                        this.tileInfoCoordinator = new TileInfoCoordinator();
                        ServiceLocator.Register(this.tileInfoCoordinator);
                    }
                }

                return this.tileInfoCoordinator;
            }
        }

        public virtual void Awake()
        {
            this.tilemap = this.GetComponent<Tilemap>();
            this.PhotonView = this.GetComponent<PhotonView>();
            this.SyncSender = NetworkConnect.Instance != null && NetworkConnect.Instance.IsOnline
                ? new PunSyncSender(this.PhotonView)
                : NullSyncSender.Instance;
        }

        public virtual void Update()
        {
            if (UnityGlobalInputAdapter.GetShowTileInfoReleased())
            {
                TileInfoUI.Instance.Init();
            }

            if (!UnityGlobalInputAdapter.GetShowTileInfoHeld() || Core.ServiceLocator.Get<PanelController>().Panels.Peek() == AsyncProgressPanel.Instance)
            {
                return;
            }

            Vector3Int posMap = Core.ServiceLocator.Get<TileMap>().GetMapPosByMouse();
            string mapType = this.GetType().Name;
            if (this.HasTile(posMap) && (this.TileInfo.ActiveMapType == string.Empty || this.TileInfo.ActiveMapType == mapType))
            {
                this.TileInfo.ActiveMapType = mapType;
                ServiceLocator.Get<TileInfoUI>().SetContent(this.GetTile(posMap).name);
                ServiceLocator.Get<TileInfoUI>().SetPostion(UnityGlobalInputAdapter.GetMouseWorldPosition(Camera.main));
            }
            else
            {
                if (this.TileInfo.ActiveMapType == mapType)
                {
                    this.TileInfo.ActiveMapType = string.Empty;
                    TileInfoUI.Instance.Init();
                }
            }
        }

        /// <summary>
        /// 获取瓦片
        /// </summary>
        /// <param name="pos">位置</param>
        /// <returns>瓦片</returns>
        public virtual TileBase GetTile(Vector3Int pos)
        {
            return this.tilemap.GetTile(pos);
        }

        /// <summary>
        /// 设置瓦片
        /// </summary>
        /// <param name="pos">位置</param>
        /// <param name="tileBase">瓦片</param>
        public virtual void SetTile(Vector3Int pos, TileBase tileBase)
        {
            this.tilemap.SetTile(pos, tileBase);
        }

        /// <summary>
        /// 判断该坐标是否可用
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>是否</returns>
        public virtual bool IsFreeTile(Vector3Int posMap)
        {
            return this.tilemap.GetTile(posMap) == null;
        }

        /// <summary>
        /// 判断该坐标是否有瓦片（虚方法，子类可用 Chunk 系统重写）
        /// </summary>
        /// <param name="pos">位置</param>
        /// <returns>是否有瓦片</returns>
        public virtual bool HasTile(Vector3Int pos)
        {
            return this.tilemap.HasTile(pos);
        }

        /// <summary>
        /// 判断该坐标是否没有碰撞体
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>是否</returns>
        public virtual bool IsCanReach(Vector3Int posMap)
        {
            return this.tilemap.GetColliderType(posMap) == Tile.ColliderType.None;
        }

        public virtual void SyncDataReq(byte[] data)
        {
        }

        public virtual void SyncDataResp(byte[] data)
        {
        }
    }
}
