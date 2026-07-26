namespace LAB2D.Domain.Common
{
    using System;

    /// <summary>
    /// 资源信息 — 简单的数据载体，用于 ColonyDiagnosticContext 委托签名
    /// 及跨模块资源传递。从 LAB2D.Item 迁移至 Domain 层以消除反向依赖。
    /// </summary>
    [Serializable]
    public class ResourceInfo
    {
        /// <summary>物品 ID。id=-1 表示空。</summary>
        public int Id;

        /// <summary>数量。</summary>
        public int Count;

        public ResourceInfo(int id)
        {
            this.Id = id;
        }

        public ResourceInfo(int id, int count)
        {
            this.Id = id;
            this.Count = count;
        }
    }
}
