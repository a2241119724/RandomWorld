namespace LAB2D.Data
{
    using LAB2D;
    /// <summary>
    /// 同步数据接口,传输字节数组
    /// </summary>
    public interface ISyncData
    {
        /// <summary>
        /// 请求同步数据
        /// </summary>
        /// <param name="data">请求的玩家信息,使得响应可以点对点</param>
        void SyncDataReq(byte[] data);

        /// <summary>
        /// 响应同步数据
        /// </summary>
        /// <param name="data">数据</param>
        void SyncDataResp(byte[] data);
    }
}
