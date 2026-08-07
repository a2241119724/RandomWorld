namespace LAB2D.Character.Enemy.CommonEnemy.State
{
    using LAB2D;
    using UnityEngine;

    /// <summary>
    /// 敌人追击状态.
    /// </summary>
    public class CommonEnemyChaseState : ACommonEnemyState
    {
        public CommonEnemyChaseState(ACommonEnemy character)
            : base(character)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();

            // LogManager.Instance.log("ChaseState", LogManager.LogLevel.Info);
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            if (this.Character.Target == null)
            {
                this.Character.Manager.ChangeState(TypeEnum.Wander);
                return;
            }

            // 仅感知捕捉的玩家
            if (this.Character.SenseNearby(this.Character.Target.transform))
            {
                ACommonEnemy.EnemyData enemyData = this.Character.CharacterDataLAB as ACommonEnemy.EnemyData;

                // 如果玩家与敌人的距离小于敌人的攻击距离，那么进入攻击状态
                if (Vector3.Distance(this.Character.Target.transform.position, this.Character.transform.position) <= enemyData.AttackRange)
                {
                    this.Character.Manager.ChangeState(TypeEnum.Attack);
                    return;
                }

                // character.GetComponent<PhotonView>().RPC("RotateTo", RpcTarget.All, character.target.transform.position - character.transform.position);
                this.Character.RotateTo(this.Character.Target.transform.position - this.Character.transform.position);

                // character.GetComponent<PhotonView>().RPC("MoveToForward", RpcTarget.All);
                this.Character.MoveToForward();
                return;
            }

            // 如果敌人感知范围内没有玩家，进入搜索状态
            this.Character.Manager.ChangeState(TypeEnum.Seek);
        }
    }
}