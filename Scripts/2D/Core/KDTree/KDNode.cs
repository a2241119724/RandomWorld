namespace LAB2D.Core.KDTree
{
    using LAB2D.Serializable;

    public class KDNode
    {
        public KDNode(Vector2ShortLAB point, int axis)
        {
            this.Point = point;
            this.Axis = axis;
        }

        /// <summary>
        /// 坐标
        /// </summary>
        public Vector2ShortLAB Point { get; set; }

        /// <summary>
        /// 左节点
        /// </summary>
        public KDNode Left { get; set; }

        /// <summary>
        /// 右节点
        /// </summary>
        public KDNode Right { get; set; }

        /// <summary>
        /// 分割的维度
        /// </summary>
        public int Axis { get; set; }

        public bool IsLeaf => this.Left == null && this.Right == null;
    }
}
