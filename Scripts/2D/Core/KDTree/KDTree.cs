namespace LAB2D.Core.KDTree
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LAB2D.Serializable;

    public class KDTree
    {
        private const int RebalanceThreshold = 100; // 每插入100次后重平衡
        private readonly Random random = new Random();
        private KDNode root;
        private int insertCountSinceRebalance = 0;

        public int Count { get; private set; }

        public bool IsEmpty => this.root == null;

        public void Insert(Vector2ShortLAB point)
        {
            this.root = this.Insert(this.root, point, 0);
            this.Count++;
            this.insertCountSinceRebalance++;

            // 自动重平衡策略
            // 如果高度超过2*log2(n)
            if (this.insertCountSinceRebalance >= RebalanceThreshold ||
                this.GetHeight(this.root) > 2 * Math.Log(this.Count, 2))
            {
                this.Rebalance();
                this.insertCountSinceRebalance = 0;
            }
        }

        /// <summary>
        /// 基于平衡因子的重平衡
        /// </summary>
        /// <returns>是否需要</returns>
        public bool NeedsRebalance()
        {
            return this.IsUnbalanced(this.root);
        }

        // 查找最近邻
        public Vector2ShortLAB FindNearestNeighbor(Vector2ShortLAB target)
        {
            if (this.root == null)
            {
                throw new InvalidOperationException("Tree is empty");
            }

            return this.FindNearestNeighbor(this.root, target, this.root.Point, double.MaxValue).Node.Point;
        }

        // 查找k个最近邻
        public List<Vector2ShortLAB> FindKNearestNeighbors(Vector2ShortLAB target, int k)
        {
            if (this.root == null)
            {
                throw new InvalidOperationException("Tree is empty");
            }

            SortedDictionary<double, Vector2ShortLAB> neighbors = new SortedDictionary<double, Vector2ShortLAB>();
            this.FindKNearestNeighbors(this.root, target, k, neighbors);
            return neighbors.Values.ToList();
        }

        // 范围搜索
        public List<Vector2ShortLAB> RangeSearch(Vector2ShortLAB center, double radius)
        {
            var result = new List<Vector2ShortLAB>();
            this.RangeSearch(this.root, center, radius, result);
            return result;
        }

        // 重新平衡树
        public void Rebalance()
        {
            var points = new List<Vector2ShortLAB>();
            this.CollectVector2ShortLABs(this.root, points);
            this.root = this.BuildTree(points, 0);
        }

        // 打印树结构（用于调试）
        public void PrintTree()
        {
            this.PrintTree(this.root, string.Empty, true);
        }

        // 计算树的高度
        private int GetHeight(KDNode node)
        {
            if (node == null)
            {
                return 0;
            }

            return 1 + Math.Max(this.GetHeight(node.Left), this.GetHeight(node.Right));
        }

        private bool IsUnbalanced(KDNode node)
        {
            if (node == null)
            {
                return false;
            }

            int leftHeight = this.GetHeight(node.Left);
            int rightHeight = this.GetHeight(node.Right);

            // 如果左右子树高度差大于1，认为不平衡
            if (Math.Abs(leftHeight - rightHeight) > 1)
            {
                return true;
            }

            return this.IsUnbalanced(node.Left) || this.IsUnbalanced(node.Right);
        }

        // 递归构建平衡KD树
        private KDNode BuildTree(List<Vector2ShortLAB> points, int depth)
        {
            if (points == null || points.Count == 0)
            {
                return null;
            }

            int axis = depth % 2;

            // 使用快速选择找到中位数，避免完全排序
            int medianIndex = points.Count / 2;
            this.QuickSelect(points, medianIndex, axis);

            var node = new KDNode(points[medianIndex], axis);

            // 递归构建左右子树
            if (medianIndex > 0)
            {
                node.Left = this.BuildTree(points.GetRange(0, medianIndex), depth + 1);
            }

            if (medianIndex + 1 < points.Count)
            {
                node.Right = this.BuildTree(points.GetRange(medianIndex + 1, points.Count - medianIndex - 1), depth + 1);
            }

            return node;
        }

        // 快速选择算法 - O(n)时间找到第k小的元素
        private void QuickSelect(List<Vector2ShortLAB> points, int k, int axis)
        {
            int left = 0;
            int right = points.Count - 1;

            while (left < right)
            {
                int pivotIndex = this.Partition(points, left, right, axis);

                if (k == pivotIndex)
                {
                    return;
                }
                else if (k < pivotIndex)
                {
                    right = pivotIndex - 1;
                }
                else
                {
                    left = pivotIndex + 1;
                }
            }
        }

        private int Partition(List<Vector2ShortLAB> points, int left, int right, int axis)
        {
            int pivotIndex = left + this.random.Next(right - left + 1);
            double pivotValue = points[pivotIndex][axis];

            // 将pivot移到末尾
            this.Swap(points, pivotIndex, right);

            int storeIndex = left;
            for (int i = left; i < right; i++)
            {
                if (points[i][axis] < pivotValue)
                {
                    this.Swap(points, storeIndex, i);
                    storeIndex++;
                }
            }

            // 将pivot移到最终位置
            this.Swap(points, storeIndex, right);
            return storeIndex;
        }

        private void Swap(List<Vector2ShortLAB> points, int i, int j)
        {
            var temp = points[i];
            points[i] = points[j];
            points[j] = temp;
        }

        private KDNode Insert(KDNode node, Vector2ShortLAB point, int depth)
        {
            if (node == null)
            {
                return new KDNode(point, depth % 2);
            }

            int axis = node.Axis;
            if (point[axis] < node.Point[axis])
            {
                node.Left = this.Insert(node.Left, point, depth + 1);
            }
            else
            {
                node.Right = this.Insert(node.Right, point, depth + 1);
            }

            return node;
        }

        private (KDNode Node, double Distance) FindNearestNeighbor(KDNode node, Vector2ShortLAB target, Vector2ShortLAB best, double bestDistance)
        {
            if (node == null)
            {
                return (null, bestDistance);
            }

            double distance = node.Point.DistanceTo(target);
            if (distance < bestDistance)
            {
                best = node.Point;
                bestDistance = distance;
            }

            int axis = node.Axis;
            double diff = target[axis] - node.Point[axis];

            // 决定先搜索哪边子树
            KDNode firstChild = diff < 0 ? node.Left : node.Right;
            KDNode secondChild = diff < 0 ? node.Right : node.Left;

            // 先搜索更可能包含最近点的子树
            var firstResult = this.FindNearestNeighbor(firstChild, target, best, bestDistance);
            best = firstResult.Node?.Point ?? best;
            bestDistance = firstResult.Distance;

            // 如果另一子树可能包含更近的点，则搜索另一子树
            if (diff * diff < bestDistance)
            {
                var secondResult = this.FindNearestNeighbor(secondChild, target, best, bestDistance);
                if (secondResult.Distance < bestDistance)
                {
                    best = secondResult.Node.Point;
                    bestDistance = secondResult.Distance;
                }
            }

            return (new KDNode(best, -1), bestDistance); // 临时节点用于返回结果
        }

        private void FindKNearestNeighbors(KDNode node, Vector2ShortLAB target, int k, SortedDictionary<double, Vector2ShortLAB> neighbors)
        {
            if (node == null)
            {
                return;
            }

            double distance = node.Point.DistanceTo(target);

            // 如果邻居数量不足k个，或者当前点比最远的邻居更近
            if (neighbors.Count < k || distance < neighbors.Keys.Last())
            {
                neighbors[distance] = node.Point;

                // 如果超过k个，移除最远的一个
                if (neighbors.Count > k)
                {
                    var lastKey = neighbors.Keys.Last();
                    neighbors.Remove(lastKey);
                }
            }

            int axis = node.Axis;
            double diff = target[axis] - node.Point[axis];

            // 决定搜索顺序
            KDNode firstChild = diff < 0 ? node.Left : node.Right;
            KDNode secondChild = diff < 0 ? node.Right : node.Left;

            this.FindKNearestNeighbors(firstChild, target, k, neighbors);

            // 检查是否需要搜索另一子树
            double maxDistance = neighbors.Count == k ? neighbors.Keys.Last() : double.MaxValue;
            if (diff * diff < maxDistance)
            {
                this.FindKNearestNeighbors(secondChild, target, k, neighbors);
            }
        }

        private void RangeSearch(KDNode node, Vector2ShortLAB center, double radius, List<Vector2ShortLAB> result)
        {
            if (node == null)
            {
                return;
            }

            double distance = node.Point.DistanceTo(center);
            if (distance <= radius)
            {
                result.Add(node.Point);
            }

            int axis = node.Axis;
            double diff = center[axis] - node.Point[axis];

            // 如果搜索范围与分割平面相交，需要搜索两个子树
            if (Math.Abs(diff) <= radius)
            {
                this.RangeSearch(node.Left, center, radius, result);
                this.RangeSearch(node.Right, center, radius, result);
            }
            else if (diff < 0)
            {
                this.RangeSearch(node.Left, center, radius, result);
            }
            else
            {
                this.RangeSearch(node.Right, center, radius, result);
            }
        }

        private void CollectVector2ShortLABs(KDNode node, List<Vector2ShortLAB> points)
        {
            if (node == null)
            {
                return;
            }

            points.Add(node.Point);
            this.CollectVector2ShortLABs(node.Left, points);
            this.CollectVector2ShortLABs(node.Right, points);
        }

        private void PrintTree(KDNode node, string indent, bool last)
        {
            if (node == null)
            {
                return;
            }

            Console.Write(indent);
            if (last)
            {
                Console.Write("└─");
                indent += "  ";
            }
            else
            {
                Console.Write("├─");
                indent += "│ ";
            }

            Console.WriteLine($"Axis {node.Axis}: {node.Point}");

            this.PrintTree(node.Left, indent, false);
            this.PrintTree(node.Right, indent, true);
        }
    }
}
