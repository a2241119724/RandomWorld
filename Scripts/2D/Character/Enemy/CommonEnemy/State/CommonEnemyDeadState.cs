namespace LAB2D.Character.Enemy.CommonEnemy.State
{
    using LAB2D;
    using Photon.Pun;
    using UnityEngine;

    /// <summary>
    /// 敌人死亡状态.
    /// </summary>
    public class CommonEnemyDeadState : ACommonEnemyState
    {
        private const float DeadTime = 0.5f; // 死亡时间
        private float recordTime = 0.0f;

        public CommonEnemyDeadState(ACommonEnemy character)
            : base(character)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();

            // LogManager.Instance.log("DeadState", LogManager.LogLevel.Warning);
            // 如果敌人初次进入死亡状态,那么禁用敌人的一些组件(碰撞体组件)
            this.Character.transform.GetComponent<Collider2D>().enabled = false;
            this.Character.LastAttacker.AddExperienceValue(5); // 增加经验值
            // experienceReward=0：经验值已通过 AddExperienceValue -> RecordExperienceGained 记录，避免重复统计
            Core.GameServices.EnemyDefeatedProvider((AEnemy)this.Character, this.Character.LastAttacker, 0);

            // 播放死亡动画
            // animator.applyRootMotion = true;
            // animator.SetTrigger("toDie");
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            this.recordTime += this.Character.DeltaTime;
            if (this.recordTime > DeadTime)
            {
                int waveIndex = Core.GameServices.WaveIndexProvider();
                AWorkerTask.EnemyLootProvider().TryDropLoot(this.Character.transform.position, System.Math.Max(0, waveIndex));

                // Object.Destroy(character.gameObject); // Destroy不会立即销毁,下一帧销毁
                Core.GameServices.NetworkDestroyProvider(this.Character.gameObject); // Destroy不会立即销毁,下一帧销毁

                // 执行OnExit并关闭脚本
                this.Character.Manager.ChangeState(TypeEnum.Wander);
            }
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
            this.Character.GetComponent<ACommonEnemy>().enabled = false;
        }
    }
}