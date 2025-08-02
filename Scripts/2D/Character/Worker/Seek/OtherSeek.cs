namespace LAB2D
{
    public class OtherSeek : ASeek
    {
        public OtherSeek(Worker character)
        : base(character)
        {
        }

        // /// <summary>
        // /// 建造不可行
        // /// </summary>
        // /// <param name="targetMap">目标坐标</param>
        // /// <returns>迭代器</returns>
        // public IEnumerator ToTargetLAB(Vector3Int targetMap)
        // {
        //     if (!TileMap.Instance.IsFreeTile(targetMap))
        //     {
        //         LogManager.Instance.Log("超出边界!!!", LogManager.LogLevel.Error);
        //         this.IsSeeking = false;
        //         yield break;
        //     }
        //     Vector3Int posMap = TileMap.Instance.WorldPosToMapPos(this.transform.position);
        //     Spend start = this.mapSpend[posMap.x, posMap.y]; // 起点
        //     Spend end = this.mapSpend[targetMap.x, targetMap.y]; // 终点
        //     while (true)
        //     {
        //         Spend mid = this.StraightMove(start, end);
        //         this.path.Add(mid);
        //         // 到达终点
        //         if (mid.PosMap.x == end.PosMap.x && mid.PosMap.y == end.PosMap.y)
        //         {
        //             break;
        //         }
        //         start = this.FindNext(mid, end);
        //         yield return this.StartCoroutine(this.ToTargetAStar(mid, start));
        //     }
        //     this.IsSeeking = false;
        // }

        // /// <summary>
        // /// 朝着目标直线走
        // /// </summary>
        // /// <param name="start">起始位置</param>
        // /// <param name="end">终点位置</param>
        // /// <returns>最后碰到障碍物后走到的位置</returns>
        // private Spend StraightMove(Spend start, Spend end)
        // {
        //     float totalDistance = Mathf.Sqrt(Mathf.Pow(start.PosMap.x - end.PosMap.x, 2) + Mathf.Pow(start.PosMap.y - end.PosMap.y, 2));
        //     int detX = end.PosMap.x - start.PosMap.x;
        //     int detY = end.PosMap.y - start.PosMap.y;
        //     do
        //     {
        //         start = this.mapSpend[end.PosMap.x - detX, end.PosMap.y - detY];
        //         this.SeekProgress = Mathf.Sqrt(Mathf.Pow(start.PosMap.x - end.PosMap.x, 2) + Mathf.Pow(start.PosMap.y - end.PosMap.y, 2)) / totalDistance;
        //         // 到达目标
        //         if (detX == 0 && detY == 0)
        //         {
        //             return end;
        //         }
        //         int max = Mathf.Abs(detX) > Mathf.Abs(detY) ? Mathf.Abs(detX) : Mathf.Abs(detY);
        //         detX -= Mathf.RoundToInt(detX * 1.0f / max);
        //         detY -= Mathf.RoundToInt(detY * 1.0f / max);
        //     }
        //     while (this.IsCanReach(new Vector3Int(end.PosMap.x - detX, end.PosMap.y - detY, 0)));
        //     return start;
        // }

        // /// <summary>
        // /// 遇到障碍物之后，获取障碍物对面最近的可用位置
        // /// </summary>
        // /// <param name="start">起始位置</param>
        // /// <param name="end">终点位置</param>
        // /// <returns>障碍物对面最近的可用位置</returns>
        // private Spend FindNext(Spend start, Spend end)
        // {
        //     int detX = end.PosMap.x - start.PosMap.x;
        //     int detY = end.PosMap.y - start.PosMap.y;
        //     do
        //     {
        //         // 到达目标
        //         if (detX == 0 && detY == 0)
        //         {
        //             return end;
        //         }
        //         int max = Mathf.Abs(detX) > Mathf.Abs(detY) ? Mathf.Abs(detX) : Mathf.Abs(detY);
        //         detX -= Mathf.RoundToInt(detX * 1.0f / max);
        //         detY -= Mathf.RoundToInt(detY * 1.0f / max);
        //     }
        //     while (!this.IsCanReach(new Vector3Int(end.PosMap.x - detX, end.PosMap.y - detY, 0)));
        //     return this.mapSpend[end.PosMap.x - detX, end.PosMap.y - detY];
        // }
    }
}
