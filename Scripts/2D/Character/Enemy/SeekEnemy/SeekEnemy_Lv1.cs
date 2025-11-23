namespace LAB2D
{
    using Photon.Pun;

    /// <summary>
    /// 敌人.
    /// </summary>
    public class SeekEnemy_Lv1 : ASeekEnemy
    {
        /// <inheritdoc/>
        public override void Start()
        {
            EnemyData enemyData = this.CharacterDataLAB as EnemyData;
            enemyData.AttackRange = enemyData.SightRange = 7.0f;
            enemyData.SoundRange = 4.0f;
            base.Start();

            // 添加状态
            this.Manager.AddState(ASeekEnemyState.TypeEnum.Seek, new SeekEnemySeekState(this));
            this.Manager.AddState(ASeekEnemyState.TypeEnum.Move, new SeekEnemyMoveState(this));
            this.Manager.AddState(ASeekEnemyState.TypeEnum.Dead, new SeekEnemyDeadState(this));
            this.Manager.AddState(ASeekEnemyState.TypeEnum.Attack, new SeekEnemyAttackState(this));

            // 初始化状态
            this.Manager.ChangeState(ASeekEnemyState.TypeEnum.Seek);
            this.Target = null;
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void Attack()
        {
            EnemyData enemyData = this.CharacterDataLAB as EnemyData;

            // 发射子弹
        }
    }
}
