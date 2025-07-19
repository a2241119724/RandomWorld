namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 工作者寻找状态
    /// </summary>
    public class WorkerSeekState : WorkerState
    {
        private Vector3Int targetMap;
        private bool isOne = true;

        public WorkerSeekState(Worker character)
            : base(character)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();

            // 如果饥饿并且没有吃饭任务就进入饥饿状态,做完任务再吃饭
            if (this.Character.CurHungry < Worker.ThresholdHungry && this.Character.Manager.Task == null)
            {
                this.Character.Manager.changeState(WorkerStateType.Hungry);
                return;
            }

            this.isOne = true;

            // 没有任务
            Vector3Int posMap = TileMap.Instance.worldPosToMapPos(this.Character.transform.position);
            this.targetMap = TileMap.Instance.genCanReachPos(posMap);
            if (this.Character.Manager.Task != null)
            {
                // 有任务
                this.targetMap = this.Character.Manager.Task.TargetMap;

                // 找旁边的位置进行建造
                float minDistance = 99999.0f;
                Vector3Int closedPos = default(Vector3Int);
                foreach (Vector3Int pos in this.Character.Manager.Task.AvailableNeighborPos)
                {
                    // 由于是斜对称
                    Vector3Int temp = new Vector3Int(this.targetMap.x + pos.y, this.targetMap.y + pos.x, 0);
                    if (this.Character.IsCanReach(temp))
                    {
                        Vector3 worldPos = TileMap.Instance.mapPosToWorldPos(temp);
                        float distance = Mathf.Pow(worldPos.x - this.Character.transform.position.x, 2) +
                            Mathf.Pow(worldPos.y - this.Character.transform.position.y, 2);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closedPos = temp;
                        }
                    }
                }

                if (closedPos == default(Vector3Int))
                {
                    LogManager.Instance.Log("没有邻居位置!!!", LogManager.LogLevel.Error);
                }

                this.targetMap = closedPos;
            }

            this.Character.InitSeek(this.targetMap);
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            base.OnUpdate();
            this.Character.WorkerState.text = this.preString + $"<color=yellow>Seeking:{Mathf.RoundToInt(this.Character.SeekProgress * 100)}%</color>\n" +
                $"Target: {this.targetMap.x},{this.targetMap.y}";
            if (Worker.SeekLock.GetLock(this.Character))
            {
                // 只能有一个在寻路(加锁),如果被锁了且锁的拥有者不是自己则阻塞，可重入
                if (this.isOne)
                {
                    this.isOne = false;
                    this.Character.ToTarget();
                }
            }

            if (!this.Character.IsSeeking)
            {
                Worker.SeekLock.ReleaseLock(this.Character);

                // 寻路结束
                this.Character.Manager.changeState(WorkerStateType.Move);
            }
        }
    }
}
