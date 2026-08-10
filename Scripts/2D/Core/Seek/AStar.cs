namespace LAB2D.Core.Seek
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 基于扁平值数组工作区的加权 A* 寻路。
    /// </summary>
    public class AStar : ASeek
    {
        private static readonly int[] NeighborX = { 0, 1, 0, -1 };
        private static readonly int[] NeighborY = { 1, 0, -1, 0 };
        private int maxIterations;
        private readonly List<int> reusablePath = new (256);
        private readonly SeekResult reusableResult = new ();

        public AStar(LAB2D.Character.Character character)
            : base(character)
        {
            var tileMap = s_tileMap.TileMapDataLAB;
            this.maxIterations = CalculateMaxIterations(
                tileMap.MapTiles.GetLength(0),
                tileMap.MapTiles.GetLength(1));
        }

        /// <inheritdoc/>
        protected override void DoSeek(
            int generation,
            Vector3Int startMap,
            Vector3Int targetMap,
            PathfindingWorkspace workspace)
        {
            this.maxIterations = CalculateMaxIterations(workspace.Width, workspace.Height);
            SeekResult result = this.reusableResult;
            result.Reset();
            if (!workspace.IsInBounds(startMap.x, startMap.y)
                || !workspace.IsInBounds(targetMap.x, targetMap.y))
            {
                result.IsReachable = false;
                this.TrySetResult(result, generation);
                return;
            }

            if (startMap == targetMap)
            {
                this.TrySetResult(result, generation);
                return;
            }

            if (!WalkabilityCache.IsWalkable(targetMap.x, targetMap.y))
            {
                result.IsReachable = false;
                this.TrySetResult(result, generation);
                return;
            }

            workspace.BeginSearch();
            List<int> path = this.reusablePath;
            path.Clear();

            int startIndex = workspace.ToIndex(startMap.x, startMap.y);
            int endIndex = workspace.ToIndex(targetMap.x, targetMap.y);
            float totalDistance = (float)Math.Sqrt(
                ((targetMap.x - startMap.x) * (targetMap.x - startMap.x))
                + ((targetMap.y - startMap.y) * (targetMap.y - startMap.y)));
            workspace.AddOrDecrease(
                startIndex,
                0.0f,
                CalculateHeuristic(startMap.x, startMap.y, targetMap.x, targetMap.y),
                -1);

            bool found = false;
            int iterationCount = 0;
            while (!this.ShouldStop(generation) && workspace.OpenCount > 0)
            {
                if (++iterationCount > this.maxIterations)
                {
                    result.IsReachable = false;
                    this.TrySetResult(result, generation);
                    return;
                }

                int currentIndex = workspace.ExtractMin();
                int currentX = workspace.GetX(currentIndex);
                int currentY = workspace.GetY(currentIndex);
                float progressedDistance = (float)Math.Sqrt(
                    ((currentX - startMap.x) * (currentX - startMap.x))
                    + ((currentY - startMap.y) * (currentY - startMap.y)));
                this.SeekProgress = totalDistance > 0.0f ? progressedDistance / totalDistance : 1.0f;

                if (currentIndex == endIndex)
                {
                    found = this.ReconstructPath(generation, workspace, startIndex, endIndex, path);
                    break;
                }

                float nextGCost = workspace.GetGCost(currentIndex) + 1.0f;
                for (int i = 0; i < NeighborX.Length; i++)
                {
                    int neighborX = currentX + NeighborX[i];
                    int neighborY = currentY + NeighborY[i];
                    if (!workspace.IsInBounds(neighborX, neighborY)
                        || !WalkabilityCache.IsWalkable(neighborX, neighborY))
                    {
                        continue;
                    }

                    int neighborIndex = workspace.ToIndex(neighborX, neighborY);
                    if (workspace.IsClosed(neighborIndex))
                    {
                        continue;
                    }

                    workspace.AddOrDecrease(
                        neighborIndex,
                        nextGCost,
                        nextGCost + CalculateHeuristic(neighborX, neighborY, targetMap.x, targetMap.y),
                        currentIndex);
                }
            }

            if (this.ShouldStop(generation))
            {
                result.IsReachable = false;
                this.TrySetResult(result, generation);
                return;
            }

            if (!found || path.Count == 0)
            {
                result.IsReachable = false;
                this.TrySetResult(result, generation);
                return;
            }

            this.CompressPath(generation, workspace, path, result.Path);
            if (this.ShouldStop(generation))
            {
                result.IsReachable = false;
                this.TrySetResult(result, generation);
                return;
            }

            if (result.Path.Count == 0)
            {
                result.IsReachable = false;
            }

            this.TrySetResult(result, generation);
        }

        private static float CalculateHeuristic(int x, int y, int targetX, int targetY)
        {
            return 1.5f * (Math.Abs(targetX - x) + Math.Abs(targetY - y));
        }

        private static bool IsLineWalkable(int fromX, int fromY, int toX, int toY)
        {
            int dx = Math.Abs(toX - fromX);
            int dy = Math.Abs(toY - fromY);
            int sx = fromX < toX ? 1 : -1;
            int sy = fromY < toY ? 1 : -1;
            int error = dx - dy;
            int x = fromX;
            int y = fromY;

            while (true)
            {
                if (!WalkabilityCache.IsWalkable(x, y))
                {
                    return false;
                }

                if (x == toX && y == toY)
                {
                    return true;
                }

                int doubledError = 2 * error;
                if (doubledError > -dy)
                {
                    error -= dy;
                    x += sx;
                }

                if (doubledError < dx)
                {
                    error += dx;
                    y += sy;
                }
            }
        }

        private bool ReconstructPath(
            int generation,
            PathfindingWorkspace workspace,
            int startIndex,
            int endIndex,
            List<int> path)
        {
            int currentIndex = endIndex;
            int guard = 0;
            while (currentIndex != startIndex && currentIndex >= 0 && guard++ <= this.maxIterations)
            {
                if (this.ShouldStop(generation))
                {
                    return false;
                }

                path.Add(currentIndex);
                currentIndex = workspace.GetParent(currentIndex);
            }

            if (currentIndex != startIndex)
            {
                return false;
            }

            path.Reverse();
            return true;
        }

        private void CompressPath(
            int generation,
            PathfindingWorkspace workspace,
            List<int> path,
            List<Vector3Int> result)
        {
            int lastIndex = 0;
            while (!this.ShouldStop(generation) && lastIndex < path.Count - 1)
            {
                int startIndex = path[lastIndex];
                if (lastIndex != 0)
                {
                    result.Add(ToPosition(workspace, startIndex));
                }

                bool advanced = false;
                int scope = Math.Min(30, path.Count - lastIndex - 1);
                for (int i = lastIndex + scope; i >= lastIndex + 1; i--)
                {
                    int targetIndex = path[i];
                    if (IsLineWalkable(
                        workspace.GetX(startIndex),
                        workspace.GetY(startIndex),
                        workspace.GetX(targetIndex),
                        workspace.GetY(targetIndex)))
                    {
                        lastIndex = i;
                        advanced = true;
                        break;
                    }
                }

                if (!advanced)
                {
                    lastIndex++;
                }
            }

            if (!this.ShouldStop(generation))
            {
                result.Add(ToPosition(workspace, path[path.Count - 1]));
            }
        }

        private static Vector3Int ToPosition(PathfindingWorkspace workspace, int index)
        {
            return new Vector3Int(workspace.GetX(index), workspace.GetY(index), 0);
        }
    }
}
