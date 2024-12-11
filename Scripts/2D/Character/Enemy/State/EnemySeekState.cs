using Photon.Pun;
using UnityEngine;

namespace LAB2D
{
    public class EnemySeekState : CharacterState<Enemy>
    {
        private float seekTime = 3.0f; // 敌人被攻击搜索时间

        public EnemySeekState(Enemy character) : base(character)
        {
        }

        public override void OnEnter()
        {
            //Debug.Log("SeekState");
            Character.continueTiming = 0.0f; // 重新计时
        }

        public override void OnExit()
        {
            Character.continueTiming = 0.0f; // 清空时间
        }

        public override void OnUpdate()
        {
            // 感知到周围有活着的玩家，进入追踪状态
            int count = PlayerManager.Instance.count();
            for (int i = 0; i < count; i++)
            {
                if (Character.SenseNearby(PlayerManager.Instance.get(i).transform))
                {
                    Character.Manager.changeState(EnemyStateType.Chase);
                    if (Character.target == null)
                    {
                        Character.target = PlayerManager.Instance.get(i);
                        return;
                    }
                }
            }
            // 感知到周围有活着的工作者，进入追踪状态
            count = WorkerManager.Instance.count();
            for (int i = 0; i < count; i++)
            {
                if (Character.SenseNearby(WorkerManager.Instance.get(i).transform))
                {
                    Character.Manager.changeState(EnemyStateType.Chase);
                    if (Character.target == null)
                    {
                        Character.target = WorkerManager.Instance.get(i);
                        return;
                    }
                }
            }
            // 如果一段时间后没有找到搜索目标,那么回到游荡状态
            Character.continueTiming += Time.deltaTime;
            if (Character.continueTiming > seekTime)
            {
                Character.Manager.changeState(EnemyStateType.Wander); // 进入游荡状态
                Character.target = null;
                return;
            }
            // 如果受到攻击,那么向着玩家方向进行搜索
            //character.GetComponent<PhotonView>().RPC("RotateTo", RpcTarget.All, character.target.transform.position - character.transform.position);
            Character.RotateTo(PlayerManager.Instance.Mine.transform.position - Character.transform.position);
            //character.GetComponent<PhotonView>().RPC("MoveToForward", RpcTarget.All);
            Character.MoveToForward();
            //可以奔跑搜索，以后实现
        }
    }
}