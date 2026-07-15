namespace LAB2D.Map
{
    using LAB2D;
    using LAB2D.Data;
    using LAB2D.Network;
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

        private static string alreadyShowMap = string.Empty;

        /// <summary>
        /// 网络同步发送器。在线为 PunSyncSender，离线为 NullSyncSender。
        /// 子类通过此属性发送同步数据，不再直接调用 PhotonView.RPC。
        /// </summary>
        protected ISyncSender SyncSender { get; private set; }

        /// <summary>
        /// PhotonView — 仅保留用于 [PunRPC] 接收端和 SyncDataTool。
        /// 发送端请使用 SyncSender。
        /// </summary>
        public PhotonView PhotonView { get; set; }

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
            if (!LAB2D.Tool.Tool.IsUIInputActive() && Input.GetKeyUp(InputKeyConstant.ShowTileInfo))
            {
                TileInfoUI.Instance.Init();
            }

            // 选择鼠标左键才会显示,在进度条界面不显示
            if (LAB2D.Tool.Tool.IsUIInputActive() || !Input.GetKey(InputKeyConstant.ShowTileInfo) || PanelController.Instance.Panels.Peek() == AsyncProgressPanel.Instance)
            {
                return;
            }

            Vector3Int posMap = TileMap.Instance.GetMapPosByMouse();
            if (this.tilemap.HasTile(posMap) && (BaseTileMap.alreadyShowMap.Equals(string.Empty) || BaseTileMap.alreadyShowMap.Equals(this.GetType().Name)))
            {
                BaseTileMap.alreadyShowMap = this.GetType().Name;
                TileInfoUI.Instance.SetContent(this.tilemap.GetTile(posMap).name);
                TileInfoUI.Instance.SetPostion(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            }
            else
            {
                // 已经抢到显示的Map退出,则关闭显示
                if (BaseTileMap.alreadyShowMap.Equals(this.GetType().Name))
                {
                    BaseTileMap.alreadyShowMap = string.Empty;
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
