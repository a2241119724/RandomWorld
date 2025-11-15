namespace LAB2D
{
    using UnityEngine;

    public class SeekEnemySeekState : ASeekEnemyState
    {
        private Vector3Int targetMap;

        public SeekEnemySeekState(ASeekEnemy character)
        : base(character)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            this.Character.Target = null;
            Vector3Int posMap = TileMap.Instance.WorldPosToMapPos(this.Character.transform.position);
            this.targetMap = TileMap.Instance.GenCanReachPos(posMap);
            this.Character.Seek.Seek(this.targetMap);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            // 感知到周围有活着的玩家，进入追踪状态
            int count = PlayerManager.Instance.Count();
            for (int i = 0; i < count; i++)
            {
                if (this.Character.SenseNearby(PlayerManager.Instance.Get(i).transform))
                {
                    this.Character.Manager.ChangeState(TypeEnum.Move);
                    this.Character.Target = PlayerManager.Instance.Get(i);
                    return;
                }
            }

            // 感知到周围有活着的Worker，进入追踪状态
            count = WorkerManager.Instance.Count();
            for (int i = 0; i < count; i++)
            {
                if (this.Character.SenseNearby(WorkerManager.Instance.Get(i).transform))
                {
                    this.Character.Manager.ChangeState(TypeEnum.Move);
                    this.Character.Target = WorkerManager.Instance.Get(i);
                    return;
                }
            }

            if (!this.Character.Seek.IsSeeking)
            {
                // Worker.SeekLock.ReleaseLock(this.Character);
                // 寻路结束
                this.Character.Manager.ChangeState(TypeEnum.Move);
            }
        }
    }
}
