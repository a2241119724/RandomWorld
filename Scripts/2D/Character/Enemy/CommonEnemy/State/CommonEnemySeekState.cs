namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 敌人搜索状态.
    /// </summary>
    public class CommonEnemySeekState : ACommonEnemyState
    {
        private const float SeekTime = 3.0f; // 敌人被攻击搜索时间
        private float recordTime = 0.0f;

        public CommonEnemySeekState(ACommonEnemy character)
            : base(character)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            // LogManager.Instance.log("SeekState", LogManager.LogLevel.Info);
            this.recordTime = 0.0f;
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            // 如果一段时间后没有找到搜索目标,那么回到游荡状态
            this.recordTime += Time.deltaTime;
            if (this.recordTime > SeekTime)
            {
                this.Character.Manager.ChangeState(TypeEnum.Wander); // 进入游荡状态
                return;
            }

            if (this.Character.Target == null)
            {
                this.Character.Manager.ChangeState(TypeEnum.Wander);
                return;
            }

            // 感知人物是否在范围内，进入追踪状态
            if (this.Character.SenseNearby(this.Character.Target.transform))
            {
                this.Character.Manager.ChangeState(TypeEnum.Chase);
                return;
            }

            // 如果受到攻击,那么向着玩家方向进行搜索
            this.Character.RotateTo(this.Character.Target.transform.position - this.Character.transform.position);
            this.Character.MoveToForward();

            // TODO可以奔跑搜索，以后实现
        }
    }
}