using Photon.Pun;
using UnityEngine;

namespace LAB2D
{
    public class EnemyChaseState : CharacterState<Enemy>
    {
        public EnemyChaseState(Enemy character) : base(character)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            //Debug.Log("ChaseState");
        }

        public override void OnUpdate()
        {
            //int count = PlayerManager.Instance.count();
            //for (int i = 0; i < count; i++)
            //{
            //    if (character.SenseNearby(PlayerManager.Instance.get(i).transform))
            //    {
            //        //如果玩家与敌人的距离小于敌人的攻击距离，那么进入攻击状态
            //        if (Vector3.Distance(PlayerManager.Instance.get(i).transform.position, character.transform.position) <= character.attackRange)
            //        {
            //            character.manager.changeState(EnemyStateType.Attack);
            //            return;
            //        }
            //        character.RotateTo(PlayerManager.Instance.get(i).transform.position - character.transform.position);
            //        character.MoveToForward();
            //        return;
            //    }
            //}
            // 仅感知捕捉的玩家
            if (Character.SenseNearby(Character.target.transform))
            {
                //如果玩家与敌人的距离小于敌人的攻击距离，那么进入攻击状态
                if (Vector3.Distance(Character.target.transform.position, Character.transform.position) <= Character.attackRange)
                {
                    Character.Manager.changeState(EnemyStateType.Attack);
                    return;
                }
                //character.GetComponent<PhotonView>().RPC("RotateTo", RpcTarget.All, character.target.transform.position - character.transform.position);
                Character.RotateTo(Character.target.transform.position - Character.transform.position);
                //character.GetComponent<PhotonView>().RPC("MoveToForward", RpcTarget.All);
                Character.MoveToForward();
                return;
            }
            //如果敌人感知范围内没有玩家，进入搜索状态
            Character.target = null;
            Character.Manager.changeState(EnemyStateType.Seek);
        }
    }
}