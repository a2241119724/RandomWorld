namespace LAB2D
{
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 基地图
    /// </summary>
    public abstract class BaseTileMap : AMonoSaveData, ISyncData
    {
        /// <summary>
        /// 地图
        /// </summary>
        protected Tilemap tilemap;

        private static string alreadyShowMap = string.Empty; // 所有的Map是否已经在显示

        /// <summary>
        /// 地图纵向长度
        /// </summary>
        public static int Height { get; set; }

        /// <summary>
        /// 地图横向长度
        /// </summary>
        public static int Width { get; set; }

        /// <summary>
        /// 同步数据
        /// </summary>
        public PhotonView PhotonView { get; set; }

        public virtual void Awake()
        {
            this.tilemap = this.GetComponent<Tilemap>();
            this.PhotonView = this.GetComponent<PhotonView>();
        }

        public virtual void Update()
        {
            if (Input.GetKeyUp(KeyCode.LeftControl))
            {
                TileInfoUI.Instance.Init();
            }

            // 选择鼠标左键才会显示,在进度条界面不显示
            if (!Input.GetKey(KeyCode.LeftControl) || PanelController.Instance.Panels.Peek() == AsyncProgressPanel.Instance)
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
