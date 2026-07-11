namespace LAB2D.Character.Enemy.SeekEnemy.State
{
    using LAB2D;
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

            if (!this.Character.Seek.IsSeeking())
            {
                // Worker.SeekLock.ReleaseLock(this.Character);
                // 寻路结束
                this.Character.Manager.ChangeState(TypeEnum.Move);
            }
        }
    }
}
