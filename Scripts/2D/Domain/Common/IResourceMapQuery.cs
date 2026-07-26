namespace LAB2D.Domain.Common
{
    /// <summary>
    /// 资源地图查询接口 — 暴露外部系统需要的资源位置和采集状态查询。
    /// 实现类: ResourceMap（Map 层）。
    /// </summary>
    public interface IResourceMapQuery
    {
        /// <summary>
        /// 检查指定位置是否有可采集的资源。
        /// </summary>
        bool HasResource(GameGridPosition posMap);

        /// <summary>
        /// 检查指定位置是否为资源节点。
        /// </summary>
        bool IsResourceNode(GameGridPosition posMap);
    }
}
