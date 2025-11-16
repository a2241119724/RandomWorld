namespace LAB2D
{
    using Photon.Pun;
    using UnityEngine;

    /// <summary>
    /// 敌人死亡状态.
    /// </summary>
    public class SeekEnemyDeadState : ASeekEnemyState
    {
        private const float DeadTime = 0.5f; // 死亡时间
        private EnemyDropManager enemyDropManager;
        private float recordTime = 0.0f;

        public SeekEnemyDeadState(ASeekEnemy character)
            : base(character)
        {
            this.enemyDropManager = new EnemyDropManager();
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();

            // LogManager.Instance.log("DeadState", LogManager.LogLevel.Warning);
            // 如果敌人初次进入死亡状态,那么禁用敌人的一些组件(碰撞体组件)
            this.Character.transform.GetComponent<Collider2D>().enabled = false;
            this.Character.LastAttacker.AddExperienceValue(5); // 增加经验值

            // 播放死亡动画
            // animator.applyRootMotion = true;
            // animator.SetTrigger("toDie");
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            this.recordTime += Time.deltaTime;
            if (this.recordTime > DeadTime)
            {
                this.enemyDropManager.DropItem(this.Character.transform.position);

                // Object.Destroy(character.gameObject); // Destroy不会立即销毁,下一帧销毁
                PhotonNetwork.Destroy(this.Character.gameObject); // Destroy不会立即销毁,下一帧销毁

                // 执行OnExit并关闭脚本
                this.Character.Manager.ChangeState(TypeEnum.Move);
            }
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
            this.Character.GetComponent<ASeekEnemy>().enabled = false;
        }
    }
}