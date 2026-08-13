namespace LAB2D.Character.Enemy.CommonEnemy.State
{
    using LAB2D;
    using Photon.Pun;
    using UnityEngine;

    /// <summary>
    /// 敌人攻击状态.
    /// </summary>
    public class CommonEnemyAttackState : ACommonEnemyState
    {
        private static readonly float AttackInterval = 1.0f;
        private float recordTime = 0.0f;

        public CommonEnemyAttackState(ACommonEnemy character)
            : base(character)
        {
        }

        /// <summary>
        /// 在攻击状态持续的时间.
        /// </summary>
        public float AttackTime { get; private set; } = 0.0f;

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            this.AttackTime = 0.0f;

            // 状态切换：进入攻击状态（一次性事件，非每帧攻击循环）
            AWorkerTask.LogProvider(
                $"[EnemyDiag] {this.Character.name} → Attack target={this.Character.Target?.name}",
                LogManager.LogLevelEnum.Debug);
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();

            // 状态切换：离开攻击状态
            AWorkerTask.LogProvider(
                $"[EnemyDiag] {this.Character.name} ← Attack",
                LogManager.LogLevelEnum.Debug);
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            this.AttackTime += this.Character.DeltaTime;
            ACommonEnemy.EnemyData enemyData = this.Character.CharacterDataLAB as ACommonEnemy.EnemyData;

            // 打击目标死亡
            if (this.Character.Target == null)
            {
                this.Character.Manager.ChangeState(TypeEnum.Wander);
                return;
            }

            // 如果玩家与敌人的距离大于敌人的攻击距离，那么进入追踪状态
            if (Vector3.Distance(this.Character.Target.transform.position, this.Character.transform.position) > enemyData.AttackRange)
            {
                this.Character.Manager.ChangeState(TypeEnum.Chase);
                return;
            }

            if (this.Character.SenseNearby(this.Character.Target.transform))
            {
                // animator.SetBool("isAttack", true);
                this.recordTime += this.Character.DeltaTime;

                // 攻击间隔时间
                if (this.recordTime > AttackInterval)
                {
                    this.recordTime = 0.0f;

                    // 攻击
                    if (this.Character.NetworkView.IsOnline)
                    {
                        this.Character.NetworkView.RPC("Attack", RpcTarget.All);
                    }
                    else
                    {
                        this.Character.Attack();
                    }

                    // 攻击执行事件（受 AttackInterval 1s 门控，非每帧）
                    AWorkerTask.LogProvider(
                        $"[EnemyDiag] {this.Character.name} 攻击执行 target={this.Character.Target?.name}",
                        LogManager.LogLevelEnum.Debug);

                    // g.GetComponent<Rigidbody2D>().velocity = g.transform.TransformDirection(character.characterForward.normalized * character.characterBulletSpeed); // 刚体的速度
                    // Object.Destroy(g, 1.0f);
                }

                // animator.SetBool("isAttack", false);
                return;
            }

            // 如果敌人感知范围内没有玩家，进入搜寻状态
            this.Character.Manager.ChangeState(TypeEnum.Seek);

            // animator.SetBool("isAttack", false);
        }

        /// <inheritdoc/>
        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (this.Character.Target == null) return;

            Vector3 direction = this.Character.Target.transform.position - this.Character.transform.position;
            this.Character.RotateTo(direction); // 旋转

            // 2米之外向玩家移动
            if (direction.magnitude > 3.0f)
            {
                this.Character.MoveToForward();
            }
        }
    }
}