namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 敌人追击状态.
    /// </summary>
    public class EnemyChaseState : CharacterState<Enemy>
    {
        public EnemyChaseState(Enemy character)
            : base(character)
        {
        }

        /// <summary>
        /// 进入追击状态.
        /// </summary>
        public override void OnEnter()
        {
            base.OnEnter();

            // LogManager.Instance.log("ChaseState", LogManager.LogLevel.Info);
        }

        /// <summary>
        /// 追击状态.
        /// </summary>
        public override void OnUpdate()
        {
            // int count = PlayerManager.Instance.count();
            // for (int i = 0; i < count; i++)
            // {
            //     if (character.SenseNearby(PlayerManager.Instance.get(i).transform))
            //     {
            //         //如果玩家与敌人的距离小于敌人的攻击距离，那么进入攻击状态
            //         if (Vector3.Distance(PlayerManager.Instance.get(i).transform.position, character.transform.position) <= character.attackRange)
            //         {
            //             character.manager.changeState(EnemyStateType.Attack);
            //             return;
            //         }
            //         character.RotateTo(PlayerManager.Instance.get(i).transform.position - character.transform.position);
            //         character.MoveToForward();
            //         return;
            //     }
            // }
            // 仅感知捕捉的玩家
            if (this.Character.SenseNearby(this.Character.Target.transform))
            {
                // 如果玩家与敌人的距离小于敌人的攻击距离，那么进入攻击状态
                if (Vector3.Distance(this.Character.Target.transform.position, this.Character.transform.position)
                    <= this.Character.AttackRange)
                {
                    this.Character.Manager.changeState(EnemyStateType.Attack);
                    return;
                }

                // character.GetComponent<PhotonView>().RPC("RotateTo", RpcTarget.All, character.target.transform.position - character.transform.position);
                this.Character.RotateTo(this.Character.Target.transform.position - this.Character.transform.position);

                // character.GetComponent<PhotonView>().RPC("MoveToForward", RpcTarget.All);
                this.Character.MoveToForward();
                return;
            }

            // 如果敌人感知范围内没有玩家，进入搜索状态
            this.Character.Manager.changeState(EnemyStateType.Seek);
        }
    }
}