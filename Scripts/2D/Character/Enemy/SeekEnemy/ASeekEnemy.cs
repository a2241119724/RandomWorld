namespace LAB2D
{
    using Photon.Pun;
    using UnityEngine;

    public abstract class ASeekEnemy : AEnemy
    {
        /// <summary>
        /// 寻路
        /// </summary>
        public ASeek Seek { get; set; }

        /// <summary>
        /// 敌人状态管理器.
        /// </summary>
        [HideInInspector]
        public SeekEnemyStateManager<ICharacterState, ASeekEnemyState.TypeEnum, ASeekEnemy> Manager { get; set; }

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            this.Seek = new AStar(this);
            this.Manager = new SeekEnemyStateManager<ICharacterState, ASeekEnemyState.TypeEnum, ASeekEnemy>(this);
        }

        public void Update()
        {
            // 执行当前状态的函数
            this.Manager.CurrentState.OnUpdate();
        }

        /// <inheritdoc/>
        public override void ReduceHp(float hp, Character attacker, bool isCRT = false)
        {
            if (this.Manager.CurrentStateType != ASeekEnemyState.TypeEnum.Attack ||
                (this.Manager.CurrentStateType == ASeekEnemyState.TypeEnum.Attack
                && ((CommonEnemyAttackState)this.Manager.CurrentState).AttackTime > ChangeTarget))
            {
                this.Manager.ChangeState(ASeekEnemyState.TypeEnum.Move); // TODO 进入追踪状态
            }

            base.ReduceHp(hp, attacker, isCRT);
            this.statusBar.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
        }

        /// <inheritdoc/>
        public override Vector3 GetDirection()
        {
            return Vector3.up;
        }

        /// <inheritdoc/>
        protected override void Death()
        {
            this.statusBar.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
            if (!NetworkConnect.Instance.IsOnline || PhotonNetwork.IsMasterClient)
            {
                EnemyManager.Instance.Remove(this);
            }

            this.Manager.ChangeState(ASeekEnemyState.TypeEnum.Dead); // 进入死亡状态
        }
    }
}
