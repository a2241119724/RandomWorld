namespace LAB2D.Core.Seek
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A* 搜索工作区池。
    /// 工作区仅包含扁平值类型数组，不再为地图上的每个格子创建 Spend 对象。
    /// </summary>
    internal static class PathfindingWorkspacePool
    {
        private const int WorkspaceCount = 2;
        private static readonly Stack<PathfindingWorkspace> Pool = new (WorkspaceCount);
        private static readonly object PoolLock = new ();
        private static int width;
        private static int height;
        private static int maxIterations;
        private static volatile bool initialized;

        public static void Initialize(int newWidth, int newHeight, int newMaxIterations)
        {
            if (initialized && width == newWidth && height == newHeight && maxIterations == newMaxIterations)
            {
                return;
            }

            lock (PoolLock)
            {
                if (initialized && width == newWidth && height == newHeight && maxIterations == newMaxIterations)
                {
                    return;
                }

                Pool.Clear();

                initialized = false;
                width = newWidth;
                height = newHeight;
                maxIterations = newMaxIterations;

                for (int i = 0; i < WorkspaceCount; i++)
                {
                    Pool.Push(new PathfindingWorkspace(width, height, maxIterations));
                }

                initialized = true;
            }
        }

        public static PathfindingWorkspace Rent()
        {
            lock (PoolLock)
            {
                if (Pool.Count > 0)
                {
                    return Pool.Pop();
                }
            }

            // 全局调度器最多只运行两个搜索，正常情况下不会进入这里。
            return new PathfindingWorkspace(width, height, maxIterations);
        }

        public static void Return(PathfindingWorkspace workspace)
        {
            lock (PoolLock)
            {
                if (workspace != null && workspace.Width == width && workspace.Height == height)
                {
                    Pool.Push(workspace);
                }
            }
        }

        /// <summary>
        /// 应用退出时释放所有工作区的大数组引用，
        /// 避免 Mono GC 在 Unity 关闭流程中回收 200MB+ 数组导致长时间卡顿。
        /// </summary>
        public static void Clear()
        {
            lock (PoolLock)
            {
                while (Pool.Count > 0)
                {
                    Pool.Pop()?.ReleaseArrays();
                }

                initialized = false;
                width = 0;
                height = 0;
                maxIterations = 0;
            }
        }
    }

    /// <summary>
    /// 单次 A* 搜索使用的可复用工作区。
    /// 通过搜索代数区分有效节点，从而避免每次清空整张地图的数组。
    /// </summary>
    public sealed class PathfindingWorkspace
    {
        private const int ClosedHeapPosition = -2;
        private int[] visitGenerations;
        private float[] gCosts;
        private float[] fCosts;
        private int[] parents;
        private int[] heapPositions;
        private int[] heap;
        private int searchGeneration;
        private int heapCount;

        public PathfindingWorkspace(int width, int height, int maxIterations)
        {
            this.Width = width;
            this.Height = height;
            int nodeCount = checked(width * height);
            int heapCapacity = Math.Min(nodeCount, checked((maxIterations * 4) + 8));

            this.visitGenerations = new int[nodeCount];
            this.gCosts = new float[nodeCount];
            this.fCosts = new float[nodeCount];
            this.parents = new int[nodeCount];
            this.heapPositions = new int[nodeCount];
            this.heap = new int[Math.Max(1, heapCapacity)];
        }

        public int Width { get; }

        public int Height { get; }

        public int OpenCount => this.heapCount;

        /// <summary>
        /// 释放所有内部数组引用，帮助 GC 回收大对象。
        /// 仅在应用退出时调用。
        /// </summary>
        public void ReleaseArrays()
        {
            // 将数组引用置 null，释放 ~20MB 托管内存
            this.visitGenerations = null;
            this.gCosts = null;
            this.fCosts = null;
            this.parents = null;
            this.heapPositions = null;
            this.heap = null;
        }

        public void BeginSearch()
        {
            this.heapCount = 0;
            if (this.searchGeneration == int.MaxValue)
            {
                Array.Clear(this.visitGenerations, 0, this.visitGenerations.Length);
                this.searchGeneration = 1;
            }
            else
            {
                this.searchGeneration++;
            }
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < this.Width && y < this.Height;
        }

        public int ToIndex(int x, int y)
        {
            return (y * this.Width) + x;
        }

        public int GetX(int index)
        {
            return index % this.Width;
        }

        public int GetY(int index)
        {
            return index / this.Width;
        }

        public bool IsClosed(int index)
        {
            return this.IsVisited(index) && this.heapPositions[index] == ClosedHeapPosition;
        }

        public float GetGCost(int index)
        {
            return this.gCosts[index];
        }

        public int GetParent(int index)
        {
            return this.parents[index];
        }

        /// <summary>
        /// 将新节点加入开放堆，或降低已在开放堆中的节点花费。
        /// </summary>
        public bool AddOrDecrease(int index, float gCost, float fCost, int parent)
        {
            if (!this.IsVisited(index))
            {
                this.visitGenerations[index] = this.searchGeneration;
                this.gCosts[index] = gCost;
                this.fCosts[index] = fCost;
                this.parents[index] = parent;
                this.Insert(index);
                return true;
            }

            int heapPosition = this.heapPositions[index];
            if (heapPosition == ClosedHeapPosition || gCost >= this.gCosts[index])
            {
                return false;
            }

            this.gCosts[index] = gCost;
            this.fCosts[index] = fCost;
            this.parents[index] = parent;
            this.BubbleUp(heapPosition);
            return true;
        }

        public int ExtractMin()
        {
            int min = this.heap[0];
            int lastIndex = --this.heapCount;
            if (lastIndex > 0)
            {
                int lastNode = this.heap[lastIndex];
                this.heap[0] = lastNode;
                this.heapPositions[lastNode] = 0;
                this.BubbleDown(0);
            }

            this.heapPositions[min] = ClosedHeapPosition;
            return min;
        }

        private bool IsVisited(int index)
        {
            return this.visitGenerations[index] == this.searchGeneration;
        }

        private void Insert(int index)
        {
            if (this.heapCount >= this.heap.Length)
            {
                throw new InvalidOperationException("Pathfinding open heap capacity exceeded.");
            }

            int position = this.heapCount++;
            this.heap[position] = index;
            this.heapPositions[index] = position;
            this.BubbleUp(position);
        }

        private void BubbleUp(int position)
        {
            while (position > 0)
            {
                int parentPosition = (position - 1) / 2;
                if (this.Compare(this.heap[position], this.heap[parentPosition]) >= 0)
                {
                    break;
                }

                this.Swap(position, parentPosition);
                position = parentPosition;
            }
        }

        private void BubbleDown(int position)
        {
            while (true)
            {
                int left = (position * 2) + 1;
                if (left >= this.heapCount)
                {
                    return;
                }

                int right = left + 1;
                int smallest = right < this.heapCount && this.Compare(this.heap[right], this.heap[left]) < 0
                    ? right
                    : left;
                if (this.Compare(this.heap[smallest], this.heap[position]) >= 0)
                {
                    return;
                }

                this.Swap(position, smallest);
                position = smallest;
            }
        }

        private int Compare(int leftNode, int rightNode)
        {
            int result = this.fCosts[leftNode].CompareTo(this.fCosts[rightNode]);
            if (result != 0)
            {
                return result;
            }

            // F 相同时优先选择已经走得更远的节点，减少大面积平局时的扩展数量。
            return this.gCosts[rightNode].CompareTo(this.gCosts[leftNode]);
        }

        private void Swap(int leftPosition, int rightPosition)
        {
            int leftNode = this.heap[leftPosition];
            int rightNode = this.heap[rightPosition];
            this.heap[leftPosition] = rightNode;
            this.heap[rightPosition] = leftNode;
            this.heapPositions[leftNode] = rightPosition;
            this.heapPositions[rightNode] = leftPosition;
        }
    }
}
