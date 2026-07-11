namespace LAB2D.Character.Enemy.SeekEnemy.State
{
    using LAB2D;
    using UnityEngine;

    public class SeekEnemyMoveState : ASeekEnemyState
    {
        private float recordTime = 0.0f;

        public SeekEnemyMoveState(ASeekEnemy character)
        : base(character)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            this.recordTime = 0.0f;
        }

        public override void OnExit()
        {
            base.OnExit();
            this.Character.Seek.StopMove();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            // 没有武器, 不进入攻击状态
            if (this.Character.CharacterDataLAB.Weapon != null)
            {
                // 感知到周围有活着的玩家，进入追踪状态
                int count = PlayerManager.Instance.Count();
                for (int i = 0; i < count; i++)
                {
                    if (this.Character.SenseNearby(PlayerManager.Instance.Get(i).transform))
                    {
                        this.Character.Manager.ChangeState(TypeEnum.Attack);
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
                        this.Character.Manager.ChangeState(TypeEnum.Attack);
                        this.Character.Target = WorkerManager.Instance.Get(i);
                        return;
                    }
                }
            }

            // 设置视觉角度
            this.Character.SightRange.transform.rotation = Quaternion.FromToRotation(Vector3.up, this.Character.Direction);
            bool isTarget = this.Character.Seek.MoveByPath();
            if (isTarget)
            {
                this.recordTime += Time.deltaTime;

                // 休息2秒
                if (this.recordTime < 2)
                {
                    return;
                }

                this.Character.Manager.ChangeState(TypeEnum.Seek);
            }
        }
    }
}
