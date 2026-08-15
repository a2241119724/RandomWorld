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
        /// <summary>
        /// 路径合并开关（设置面板 WorkerPathMerge 控制，默认 true=压缩路径）。
        /// false = 直接输出 A* 原始逐格 path（不压缩），用于验证「压缩产生的跨格直线」是否为
        /// 门口卡死根因：关闭后仍卡 → 问题在 A* path 本身或缓存；不卡 → 问题在压缩逻辑。
        /// 切换只影响后续生成的路径，已发布的路径不受影响。
        /// </summary>
        public static bool EnablePathMerge { get; set; } = true;

        /// <summary>
        /// 设置面板 WorkerPathMerge Toggle 的 onValueChanged 绑定入口（Unity 支持绑定静态方法）。
        /// </summary>
        public static void SetPathMerge(bool enabled)
        {
            EnablePathMerge = enabled;
            LAB2D.Character.Worker.Task.AWorkerTask.LogProvider(
                $"[SeekDiag] 路径合并开关={enabled}",
                LAB2D.Manager.LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 启动时自动查找设置面板「WorkerPathMerge」容器下的 Toggle 并绑定到 SetPathMerge。
        /// 同步初始状态为 EnablePathMerge（默认 true=合并），与「默认合并」语义一致，
        /// 避免场景 Toggle 初始 m_IsOn=0 但实际合并生效的 UI/逻辑不一致。
        /// 用户运行时切换 Toggle 即实时控制后续寻路的路径合并。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBindPathMergeToggle()
        {
            UnityEngine.UI.Toggle[] toggles = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Toggle>(true);
            foreach (UnityEngine.UI.Toggle toggle in toggles)
            {
                if (toggle.transform.parent != null && toggle.transform.parent.name == "WorkerPathMerge")
                {
                    toggle.onValueChanged.RemoveAllListeners();
                    toggle.onValueChanged.AddListener(SetPathMerge);
                    toggle.isOn = EnablePathMerge; // 触发一次 SetPathMerge(true)，同步显示为勾选
                    return;
                }
            }
        }

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

            this.CompressPath(generation, workspace, path, result);
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
            return IsLineWalkable(fromX, fromY, toX, toY, WalkabilityCache.IsWalkable);
        }

        /// <summary>
        /// 直线可通行性走查（Bresenham）。public 供 ASeek 路径段重验证
        /// 与 Editor 纯函数单测复用（委托注入可通行判定，见 ASeekSlideDirectionTests 模式）。
        /// </summary>
        public static bool IsLineWalkable(int fromX, int fromY, int toX, int toY, Func<int, int, bool> isWalkable)
        {
            int dx = Math.Abs(toX - fromX);
            int dy = Math.Abs(toY - fromY);
            // 直线主导方向：压缩后角色沿主导方向长距离直行，宽度需求在垂直方向两侧。
            bool dominantX = dx >= dy;
            int sx = fromX < toX ? 1 : -1;
            int sy = fromY < toY ? 1 : -1;
            int error = dx - dy;
            int x = fromX;
            int y = fromY;
            int prevX = x;
            int prevY = y;

            while (true)
            {
                if (!isWalkable(x, y))
                {
                    return false;
                }

                // 角落检测：当一步中同时移动了 X 和 Y（对角线移动），
                // 任一角格不可通行即拒绝——标准 no-corner-cutting。
                // 修复前用 &&（要求 both 不可通）会放行压缩后的对角路径穿过墙角
                // （网格判可通、物理 Sprite 碰撞体挡 → 卡死，见 bug-fixes.md）。
                if (x != prevX && y != prevY)
                {
                    int corner1X = prevX;
                    int corner1Y = y;
                    int corner2X = x;
                    int corner2Y = prevY;

                    if (!isWalkable(corner1X, corner1Y)
                        || !isWalkable(corner2X, corner2Y))
                    {
                        return false;
                    }
                }

                // 宽度检查（模拟碰撞体两端切面，2026-08-16）：合并检测的是零宽度格中心线，
                // 实际移动是半径 0.1 的圆、从格内偏移点出发——格中心可通但直线边缘擦物理碰撞体
                // （床主格 Sprite、墙边）会卡死。要求主导方向两侧邻格可通（≥3 格宽通道），
                // 保证合并直线两侧有缓冲、不贴墙角；Worker 在格内任意偏移仍留安全余量。
                // 合并失败 → 回退 A* 逐格路径（原本可走），安全兜底不卡死。
                if (dominantX)
                {
                    if (!isWalkable(x, y - 1) || !isWalkable(x, y + 1))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!isWalkable(x - 1, y) || !isWalkable(x + 1, y))
                    {
                        return false;
                    }
                }

                if (x == toX && y == toY)
                {
                    return true;
                }

                prevX = x;
                prevY = y;
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
            SeekResult result)
        {
            // 卡床排查 2026-08-16：后台线程不调 LogProvider（ASeek 约定：LogProvider 不可在线程池内
            // 直接调用），改为把诊断字符串填到 result 字段，主线程 MoveByPath 在 PathIndex==0 时打印。
            // 目标：定位「压缩首点需经过缓存不可通格」矛盾——(205,400) 缓存不可通，压缩首点 (204,402)
            // 按 Bresenham 必经 (205,400)，此日志直接显示 A* path[0]/path[1] 与压缩跳转判定。
            if (this.Character != null)
            {
                int head = Math.Min(path.Count, 6);
                string raw = string.Empty;
                for (int j = 0; j < head; j++)
                {
                    raw += $"({workspace.GetX(path[j])},{workspace.GetY(path[j])})";
                }

                if (path.Count > head)
                {
                    raw += "...(" + path.Count + ")";
                }

                result.RawPathDiag = $"path[{path.Count}] 头{raw}";
            }

            // 路径合并开关（WorkerPathMerge）关闭：直接输出 A* 原始逐格 path，不做跨格直线压缩。
            // 用于验证压缩逻辑是否为卡死根因（若关闭后不再卡 → 根因在压缩产生的跨格直线）。
            if (!EnablePathMerge)
            {
                for (int i = 0; i < path.Count; i++)
                {
                    result.Path.Add(ToPosition(workspace, path[i]));
                }

                return;
            }

            int lastIndex = 0;
            while (!this.ShouldStop(generation) && lastIndex < path.Count - 1)
            {
                int startIndex = path[lastIndex];
                if (lastIndex != 0)
                {
                    result.Path.Add(ToPosition(workspace, startIndex));
                }

                bool advanced = false;
                int scope = Math.Min(30, path.Count - lastIndex - 1);
                int attemptedTarget = -1;
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

                    // 记录最近一次直线不可通候选（i 递减，最终为最接近 startIndex 的失败候选）。
                    attemptedTarget = targetIndex;
                }

                if (!advanced)
                {
                    lastIndex++;
                }
                else if (this.Character != null && attemptedTarget >= 0)
                {
                    // 本轮压缩跳转成功但存在失败候选——记录跳转轨迹，供主线程打印核对直线判定。
                    result.CompressJumpDiag = $"({workspace.GetX(startIndex)},{workspace.GetY(startIndex)})→" +
                        $"({workspace.GetX(path[lastIndex])},{workspace.GetY(path[lastIndex])}) " +
                        $"最近失败候选→({workspace.GetX(attemptedTarget)},{workspace.GetY(attemptedTarget)})";
                }
            }

            if (!this.ShouldStop(generation))
            {
                result.Path.Add(ToPosition(workspace, path[path.Count - 1]));
            }
        }

        private static Vector3Int ToPosition(PathfindingWorkspace workspace, int index)
        {
            return new Vector3Int(workspace.GetX(index), workspace.GetY(index), 0);
        }
    }
}
