namespace LAB2D
{
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// Worker寻找状态
    /// </summary>
    public class WorkerSeekState : AWorkerState
    {
        // private bool isOne = true;
        private readonly StringBuilder builder = new (128); // 减少GC
        private Vector3Int targetMap;
        private long seekTimes; // 没有任务寻路的次数

        public WorkerSeekState(AWorker character)
            : base(character)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();

            // 如果饥饿并且没有吃饭任务就进入饥饿状态,做完任务再吃饭
            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;

            // this.isOne = true;

            // 没有任务
            Vector3Int posMap = TileMap.Instance.WorldPosToMapPos(this.Character.transform.position);
            this.targetMap = TileMap.Instance.GenCanReachPos(posMap);
            if (workerData.Task != null)
            {
                // 有任务
                this.targetMap = Vector3IntLAB.ToVector3Int(workerData.Task.TargetMap);
                float minDistance = 99999.0f;
                Vector3Int closedPos = default;
                foreach (Vector3IntLAB pos in workerData.Task.AvailableNeighborPos)
                {
                    // 由于是斜对称
                    Vector3Int temp = new (this.targetMap.x + pos.Y, this.targetMap.y + pos.X, 0);
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
                    LogManager.Instance.Log($"{workerData.Task.TaskType}, 没有邻居位置!", LogManager.LogLevelEnum.Warning);
                    this.Character.GiveUpTask();
                    return;
                }

                this.targetMap = closedPos;
            }
            else
            {
                LogManager.Instance.Log(this.Character.name + " 没有任务!");
                ++this.seekTimes;
                if (this.seekTimes % WorkerTaskTimeConfig.ExerciseSeekThreshold == 0)
                {
                    WorkerTaskManager.Instance.AddTask(
                        new WorkerExerciseTask.ExerciseTaskBuilder()
                        .SetTarget(this.targetMap)
                        .SetWorker(this.Character)
                        .Build(), Vector3IntLAB.Zero,
                        3);
                }
            }

            LogManager.Instance.Log(this.Character.name + " 寻路->" + this.targetMap);
            this.Character.Seek.Seek(this.targetMap);
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            base.OnUpdate();

            // 每60帧刷新一次
            if (Time.frameCount % 60 == 0)
            {
                this.builder.Clear();
                this.Character.WorkerStateText.text = this.builder.Append(this.preString)
                    .Append("<color=" + PixelUITheme.RichGold + ">Seeking: ")
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
            if (!this.Character.Seek.IsSeeking())
            {
                // 没有找到路
                if (!this.Character.Seek.IsHavePath())
                {
                    // 如果有任务
                    AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;
                    if (workerData.Task != null)
                    {
                        this.Character.GiveUpTask();
                    }
                    else
                    {
                        this.Character.Manager.ChangeState(AWorkerState.TypeEnum.Seek);
                    }

                    LogManager.Instance.Log(this.Character.name + " 没有找到路!");
                    return;
                }

                // Worker.SeekLock.ReleaseLock(this.Character);
                // 寻路结束
                this.Character.Manager.ChangeState(TypeEnum.Move);
            }
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }
    }
}
