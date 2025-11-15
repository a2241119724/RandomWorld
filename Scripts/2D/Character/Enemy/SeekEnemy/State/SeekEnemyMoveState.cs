namespace LAB2D
{
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

        public override void OnUpdate()
        {
            base.OnUpdate();
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
