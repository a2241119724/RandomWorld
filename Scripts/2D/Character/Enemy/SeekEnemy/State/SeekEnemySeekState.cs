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
            Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(this.Character.transform.position);
            this.targetMap = AWorkerTask.GenCanReachPosProvider(posMap);
            this.Character.Seek.Seek(this.targetMap);

            // 状态切换：进入寻路状态（高频漫游事件，节流 2s/条）
            AWorkerTask.LogProviderThrottled(
                $"{this.Character.name}|SeekIn", 2f,
                $"[EnemyDiag] {this.Character.name} → Seek target=({this.targetMap.x},{this.targetMap.y})",
                LogManager.LogLevelEnum.Debug);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (!this.Character.Seek.IsSeeking())
            {
                // Worker.SeekLock.ReleaseLock(this.Character);
                // 寻路结束
                // 寻路失败事件（一次性）：目标不可达/无路径时记录目标坐标
                if (!this.Character.Seek.IsHavePath())
                {
                    AWorkerTask.LogProviderThrottled(
                        $"{this.Character.name}|SeekFail", 2f,
                        $"[EnemyDiag] {this.Character.name} 寻路失败 target=({this.targetMap.x},{this.targetMap.y}) " +
                        $"pos=({this.Character.transform.position.x:F1},{this.Character.transform.position.y:F1})",
                        LogManager.LogLevelEnum.Debug);
                }

                this.Character.Manager.ChangeState(TypeEnum.Move);
            }
        }
    }
}
