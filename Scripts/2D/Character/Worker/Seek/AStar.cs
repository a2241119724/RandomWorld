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
        protected override void DoSeek(Vector3Int targetMap)
        {
            this.TargetMap = targetMap;
            this.IsSeeking = true;
            this.openList.Clear();
            this.closeList.Clear();
            this.path.Clear();
            this.SeekProgress = 0.0f;
            Vector3Int posMap = default;
            UnityMainThreadDispatcher.Instance.EnqueueAsync(() =>
            {
                posMap = TileMap.Instance.WorldPosToMapPos(this.Character.transform.position);
                this.UpdateLine();
            }).Wait();

            // 起点就是终点
            if (posMap.x == this.TargetMap.x && posMap.y == this.TargetMap.y)
            {
                this.StopSeek();
                return;
            }

            Spend start = this.mapSpend[posMap.x, posMap.y]; // 起点
            start.Previous = null;
            Spend end = this.mapSpend[this.TargetMap.x, this.TargetMap.y]; // 终点
            this.isStopThread = false;
            List<Spend> path = new ();
            float totalDistance = Mathf.Sqrt(Mathf.Pow(start.PosMap.x - end.PosMap.x, 2)
                + Mathf.Pow(start.PosMap.y - end.PosMap.y, 2));
            this.openList.Add(start);
            while (!this.isStopThread && this.openList.Count != 0)
            {
                int minIndex = 0;

                if (this.isStopThread)
                {
                    this.StopSeek();
                    return;
                }

                // 选出当前相邻位置最小花费f在openList中的索引位置
                for (int i = 1; i < this.openList.Count; i++)
                {
                    if (this.isStopThread)
                    {
                        this.StopSeek();
                        return;
                    }

                    if (this.openList[i].F < this.openList[minIndex].F)
                    {
                        minIndex = i;
                    }
                }

                if (this.isStopThread)
                {
                    this.StopSeek();
                    return;
                }

                Spend curSpend = this.openList[minIndex];
                this.SeekProgress = Mathf.Sqrt(Mathf.Pow(curSpend.PosMap.x - start.PosMap.x, 2)
                    + Mathf.Pow(curSpend.PosMap.y - start.PosMap.y, 2)) / totalDistance;

                // 判断是否到达终点(此处只能是整数)
                if ((int)curSpend.PosMap.x == (int)end.PosMap.x && (int)curSpend.PosMap.y == (int)end.PosMap.y)
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

                        if (quickCurSpend != null && quickCurSpend.PosMap.x == curSpend.Previous.PosMap.x
                            && quickCurSpend.PosMap.y == curSpend.Previous.PosMap.y)
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
                        this.StopSeek();
                        return;
                    }

                    break;
                }

                if (this.isStopThread)
                {
                    this.StopSeek();
                    return;
                }

                this.openList.Remove(curSpend);
                this.closeList.Add(curSpend);

                // 对邻居进行f = g + h
                byte isCorner = 0;
                foreach (Vector2SByteLAB direction in Neighbors)
                {
                    ++isCorner;
                    int x = curSpend.PosMap.x + direction.X;
                    int y = curSpend.PosMap.y + direction.Y;

                    bool isReach = true;
                    UnityMainThreadDispatcher.Instance.EnqueueAsync(() =>
                    {
                        isReach = ASeek.IsCanReach(new Vector3Int(x, y, 0));
                    }).Wait();

                    // 数组下标
                    if (!isReach)
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
                        UnityMainThreadDispatcher.Instance.EnqueueAsync(() =>
                        {
                            isReach = ASeek.IsCanReach(new Vector3Int(x, curSpend.PosMap.y, 0)) || ASeek.IsCanReach(new Vector3Int(curSpend.PosMap.x, y, 0));
                        }).Wait();

                        // 当上下左右阻塞时，斜着不可走
                        if (!isReach)
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
                        this.StopSeek();
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
                            this.StopSeek();
                            return;
                        }

                        this.openList.Add(neighbor);
                    }

                    // 加权A*，使得寻路更快，但不是最短路径
                    neighbor.H = 1.5f * (Mathf.Abs(end.PosMap.x - neighbor.PosMap.x) + Mathf.Abs(end.PosMap.y - neighbor.PosMap.y));
                    neighbor.F = neighbor.G + neighbor.H;
                    neighbor.Previous = curSpend; // 链接
                }
            }

            if (this.isStopThread)
            {
                this.StopSeek();
                return;
            }

            // 合并path
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
                        this.path.Add(start);
                    }

                    // 在一定path范围内, 倒叙遍历最后一个直达的位置
                    int scope = Mathf.Min(30, path.Count - lastIndex - 1);
                    for (int i = lastIndex + scope; i >= lastIndex + 1; i--)
                    {
                        if (this.isStopThread)
                        {
                            this.StopSeek();
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
                    this.StopSeek();
                    return;
                }

                this.path.Add(path[^1]);
            }
            else
            {
                UnityMainThreadDispatcher.Instance.EnqueueAsync(() =>
                {
                    LogManager.Instance.Log(this.Character.name + ":未找到路径 " + start.PosMap.y + ":" + start.PosMap.x + "-->" + end.PosMap.y + ":" + end.PosMap.x, LogManager.LogLevelEnum.Error);
                }).Wait();
            }

            this.StopSeek();
        }
    }
}