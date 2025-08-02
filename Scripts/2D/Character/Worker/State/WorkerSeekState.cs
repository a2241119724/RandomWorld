namespace LAB2D
{
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// Worker寻找状态
    /// </summary>
    public class WorkerSeekState : WorkerState
    {
        // private bool isOne = true;
        private Vector3Int targetMap;
        private StringBuilder builder = new StringBuilder(128); // 减少GC

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
                this.Character.Manager.ChangeState(WorkerStateTypeEnum.Eat);
                return;
            }

            // this.isOne = true;

            // 没有任务
            Vector3Int posMap = TileMap.Instance.WorldPosToMapPos(this.Character.transform.position);
            this.targetMap = TileMap.Instance.GenCanReachPos(posMap);
            if (this.Character.Manager.Task != null)
            {
                // 有任务
                this.targetMap = this.Character.Manager.Task.TargetMap;
                float minDistance = 99999.0f;
                Vector3Int closedPos = default;
                foreach (Vector3Int pos in this.Character.Manager.Task.AvailableNeighborPos)
                {
                    // 由于是斜对称
                    Vector3Int temp = new (this.targetMap.x + pos.y, this.targetMap.y + pos.x, 0);
                    if (ASeek.IsCanReach(temp))
                    {
                        Vector3 worldPos = TileMap.Instance.MapPosToWorldPos(temp);
                        float distance = Mathf.Pow(worldPos.x - this.Character.transform.position.x, 2) +
                            Mathf.Pow(worldPos.y - this.Character.transform.position.y, 2);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closedPos = temp;
                        }
                    }
                }

                if (closedPos == default)
                {
                    LogManager.Instance.Log("没有邻居位置!!!", LogManager.LogLevel.Error);
                }

                this.targetMap = closedPos;
            }

            this.Character.Seek.Seek(this.targetMap);
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            base.OnUpdate();
            if (Time.frameCount % 60 == 0)
            {
                this.builder.Clear();
                this.Character.WorkerStateText.text = this.builder.Append(this.preString)
                    .Append("<color=yellow>Seeking: ")
                    .Append(Mathf.RoundToInt(this.Character.Seek.SeekProgress * 100))
                    .Append("%</color>\nTarget: ")
                    .Append(this.targetMap.x)
                    .Append(",")
                    .Append(this.targetMap.y)
                    .ToString();
            }

            // if (Worker.SeekLock.GetLock(this.Character))
            // {
            //     // 使用协程时,只能有一个在寻路(加锁),如果被锁了且锁的拥有者不是自己则阻塞,可重入
            //     if (this.isOne)
            //     {
            //         this.isOne = false;
            //         this.Character.ToTarget();
            //     }
            // }

            // // 只能有一个在寻路
            // if (this.isOne)
            // {
            //     this.isOne = false;
            //     this.Character.ToTarget();
            // }
            if (!this.Character.Seek.IsSeeking)
            {
                // Worker.SeekLock.ReleaseLock(this.Character);
                // 寻路结束
                this.Character.Manager.ChangeState(WorkerStateTypeEnum.Move);
            }
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }
    }
}
