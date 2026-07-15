namespace LAB2D.Map
{
    using LAB2D;
    using LAB2D.Data;
    using LAB2D.Serializable;
    using System;
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 采集地图
    /// </summary>
    public class GatherMap : BaseTileMap, ISyncData
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static GatherMap Instance { get; private set; }

        /// <summary>
        /// 采集地图数据
        /// </summary>
        public GatherMapData GatherMapDataLAB { get; private set; }

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            Instance = this;
            this.GatherMapDataLAB = new GatherMapData();
        }

        /// <summary>
        /// 添加采集物
        /// </summary>
        /// <param name="posMap">位置</param>
        public void AddGather(Vector3Int posMap)
        {
            this.tilemap.SetTile(posMap, (TileBase)ResourceManager.Instance.GetAsset("Gather"));
            this.GatherMapDataLAB.Add(posMap, "Gather");
            this.SyncSender.Broadcast("SyncDataResp", DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(posMap)), "Gather");
        }

        /// <summary>
        /// 删除采集物
        /// </summary>
        /// <param name="posMap">位置</param>
        public void CancelGather(Vector3Int posMap)
        {
            this.tilemap.SetTile(posMap, null);
            this.GatherMapDataLAB.Remove(posMap);
            this.SyncSender.Broadcast("SyncDataResp", DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(posMap)), string.Empty, true);
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataReq(byte[] data)
        {
            base.SyncDataReq(data);
            LogManager.Instance.Log("Request: 同步地图采集数据", LogManager.LogLevelEnum.Trace);
            SyncDataTool.SyncDataRespWrapper(this.PhotonView, data, this.GatherMapDataLAB);
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataResp(byte[] data)
        {
            base.SyncDataResp(data);
            LogManager.Instance.Log("Response: 同步地图采集数据", LogManager.LogLevelEnum.Trace);
            GatherMapData gatherMapData = DataTool.FromByteArray<GatherMapData>(data);
            Dictionary<Vector3IntLAB, string>.Enumerator enumerator = gatherMapData.PosMap.GetEnumerator();
            while (enumerator.MoveNext())
            {
                this.tilemap.SetTile(
                    Vector3IntLAB.ToVector3Int(enumerator.Current.Key),
                    (TileBase)ResourceManager.Instance.GetAsset(enumerator.Current.Value));
            }
        }

        /// <summary>
        /// 同步地图采集数据
        /// </summary>
        /// <param name="vector3IntLAB">位置</param>
        /// <param name="tileBaseName">tile名称</param>
        /// <param name="isDelete">是否删除</param>
        [PunRPC]
        public void SyncDataResp(byte[] vector3IntLAB, string tileBaseName, bool isDelete = false)
        {
            LogManager.Instance.Log("Response: 同步地图采集数据", LogManager.LogLevelEnum.Trace);

            Vector3Int vector3Int = Vector3IntLAB.ToVector3Int(DataTool.FromByteArray<Vector3IntLAB>(vector3IntLAB));
            if (isDelete)
            {
                this.tilemap.SetTile(vector3Int, null);
                return;
            }

            this.tilemap.SetTile(vector3Int, (TileBase)ResourceManager.Instance.GetAsset(tileBaseName));
        }

        /// <summary>
        /// 采集数据
        /// </summary>
        [Serializable]
        public class GatherMapData : ATileMapData
        {
            public GatherMapData()
            {
            }
        }
    }
}
