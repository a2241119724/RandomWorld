namespace LAB2D
{
    using System.Collections.Generic;
    using PimDeWitte.UnityMainThreadDispatcher;
    using UnityEngine;

    /// <summary>
    /// A*寻路
    /// TODO 周围方块更新要重新寻路
    /// </summary>
    public class AStar : ASeek
    {
        public AStar(Character character)
            : base(character)
        {
        }

        /// <inheritdoc/>
        protected override void DoSeek()
        {
            Vector3Int posMap = default;
            UnityMainThreadDispatcher.Instance.EnqueueAsync(() =>
            {
                posMap = TileMap.Instance.WorldPosToMapPos(this.Character.transform.position);
            }).Wait();

            // 起点就是终点
            if (posMap == this.TargetMap)
            {
                UnityMainThreadDispatcher.Instance.EnqueueAsync(() =>
                {
                    LogManager.Instance.Log(this.Character.name + ":起始==终点", LogManager.LogLevelEnum.Trace);
                }).Wait();
                this.SetResult(new SeekResult());
                return;
            }

            Spend start = this.mapSpend[posMap.x, posMap.y]; // 起点
            start.Previous = null;
            Spend end = this.mapSpend[this.TargetMap.x, this.TargetMap.y]; // 终点
            List<Spend> path = new ();
            float totalDistance = Mathf.Sqrt(Mathf.Pow(start.PosMap.X - end.PosMap.X, 2)
                + Mathf.Pow(start.PosMap.Y - end.PosMap.Y, 2));
            this.openList.Add(start);
            while (!this.isStopThread && this.openList.Count != 0)
            {
                int minIndex = 0;

                if (this.isStopThread)
                {
                    return;
                }

                // 选出当前相邻位置最小花费f在openList中的索引位置
                for (int i = 1; i < this.openList.Count; i++)
                {
                    if (this.isStopThread)
                    {
                        return;
                    }

                    if (this.openList[i].F < this.openList[minIndex].F)
                    {
                        minIndex = i;
                    }
                }

                if (this.isStopThread)
                {
                    return;
                }

                Spend curSpend = this.openList[minIndex];
                this.SeekProgress = Mathf.Sqrt(Mathf.Pow(curSpend.PosMap.X - start.PosMap.X, 2)
                    + Mathf.Pow(curSpend.PosMap.Y - start.PosMap.Y, 2)) / totalDistance;

                // 判断是否到达终点(此处只能是整数)
                if (curSpend.PosMap == end.PosMap)
                {
                    // LogManager.Instance.log("找到路径!!!", LogManager.LogLevel.Info);
                    // 找路径
                    Vector3Int lastDet = new (0, 0);
                    Spend quickCurSpend = curSpend;
                    while (!this.isStopThread && curSpend != null && curSpend.Previous != null)
                    {
                        path.Insert(0, curSpend);

                        // 可能出现循环路径
                        if (quickCurSpend != null)
                        {
                            quickCurSpend = quickCurSpend.Previous;
                            if (quickCurSpend != null)
                            {
                                quickCurSpend = quickCurSpend.Previous;
                            }
                        }

                        if (quickCurSpend != null && quickCurSpend.PosMap.X == curSpend.Previous.PosMap.X
                            && quickCurSpend.PosMap.Y == curSpend.Previous.PosMap.Y)
                        {
                            UnityMainThreadDispatcher.Instance.EnqueueAsync(() =>
                            {
                                LogManager.Instance.Log(this.Character.name + ":寻路出现环路", LogManager.LogLevelEnum.Error);
                            }).Wait();
                            break;
                        }

                        curSpend = curSpend.Previous;
                    }

                    if (this.isStopThread)
                    {
                        return;
                    }

                    break;
                }

                if (this.isStopThread)
                {
                    return;
                }

                this.openList.Remove(curSpend);
                this.closeList.Add(curSpend);

                // 对邻居进行f = g + h
                byte isCorner = 0;
                foreach (Vector2SByteLAB direction in Neighbors)
                {
                    ++isCorner;
                    int x = curSpend.PosMap.X + direction.X;
                    int y = curSpend.PosMap.Y + direction.Y;

                    // 直接从缓存读取(后台线程安全), 无需向主线程派发
                    if (!WalkabilityCache.IsWalkable(x, y))
                    {
                        continue;
                    }

                    Spend neighbor = this.mapSpend[x, y];

                    // 关闭队列不计算
                    if (this.closeList.Contains(neighbor))
                    {
                        continue;
                    }

                    float temp;
                    if (isCorner > 4)
                    {
                        // 当上下左右阻塞时，斜着不可走
                        if (!WalkabilityCache.IsWalkable(x, curSpend.PosMap.Y)
                            && !WalkabilityCache.IsWalkable(curSpend.PosMap.X, y))
                        {
                            continue;
                        }

                        temp = curSpend.G + 1.414f; // 斜着相邻
                    }
                    else
                    {
                        temp = curSpend.G + 1.0f; // 挨着相邻
                    }

                    if (this.isStopThread)
                    {
                        return;
                    }

                    // 打开队列已经计算过，赋值最小的g
                    if (this.openList.Contains(neighbor))
                    {
                        // 回溯,放弃该节点
                        if (temp >= neighbor.G)
                        {
                            continue;
                        }

                        neighbor.G = temp;
                    }

                    // 不在任何列表中
                    else
                    {
                        neighbor.G = temp;

                        if (this.isStopThread)
                        {
                            return;
                        }

                        this.openList.Add(neighbor);
                    }

                    // 加权A*，使得寻路更快，但不是最短路径
                    neighbor.H = 1.5f * (Mathf.Abs(end.PosMap.X - neighbor.PosMap.X) + Mathf.Abs(end.PosMap.Y - neighbor.PosMap.Y));
                    neighbor.F = neighbor.G + neighbor.H;
                    neighbor.Previous = curSpend; // 链接
                }
            }

            if (this.isStopThread)
            {
                return;
            }

            // 合并path
            SeekResult seekResult = new ();
            if (path.Count > 0)
            {
                int lastIndex = 0;
                while (!this.isStopThread && lastIndex < path.Count - 1)
                {
                    bool isUpdate = false;
                    start = path[lastIndex];

                    // 不加入起点第一个位置
                    if (lastIndex != 0)
                    {
                        seekResult.Path.Add(start);
                    }

                    // 在一定path范围内, 倒叙遍历最后一个直达的位置
                    int scope = Mathf.Min(30, path.Count - lastIndex - 1);
                    for (int i = lastIndex + scope; i >= lastIndex + 1; i--)
                    {
                        if (this.isStopThread)
                        {
                            return;
                        }

                        // 上下左右平移一下射线
                        Vector3 pos = TileMap.Instance.MapPosToWorldPos(start.PosMap);
                        Vector3 direction = TileMap.Instance.MapPosToWorldPos(path[i].PosMap) - TileMap.Instance.MapPosToWorldPos(start.PosMap);
                        float distance = Vector3.Distance(TileMap.Instance.MapPosToWorldPos(start.PosMap), TileMap.Instance.MapPosToWorldPos(path[i].PosMap));

                        bool isAllCanReach = true;
                        UnityMainThreadDispatcher.Instance.EnqueueAsync(() =>
                        {
                            RaycastHit2D hit;
                            foreach (var offset in this.checkOffsets)
                            {
                                hit = Physics2D.Raycast(pos + offset, direction, distance);
                                if (hit.collider != null && hit.collider.name.Contains("Map"))
                                {
                                    isAllCanReach = false;
                                    break;
                                }
                            }
                        }).Wait();
                        if (isAllCanReach)
                        {
                            lastIndex = i;
                            isUpdate = true;
                            break;
                        }
                    }

                    if (!isUpdate)
                    {
                        lastIndex++;
                    }
                }

                if (this.isStopThread)
                {
                    return;
                }

                seekResult.Path.Add(path[^1]);
            }
            else
            {
                UnityMainThreadDispatcher.Instance.EnqueueAsync(() =>
                {
                    LogManager.Instance.Log(this.Character.name + ":未找到路径 " + start.PosMap + "-->" + end.PosMap, LogManager.LogLevelEnum.Error);
                }).Wait();
            }

            this.SetResult(seekResult);
        }
    }
}
