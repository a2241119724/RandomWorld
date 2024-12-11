using Photon.Pun;
using UnityEngine;

namespace LAB2D
{
    public class EnemyWanderState : CharacterState<Enemy>
    {
        private float recordTime = 0; // 记录时间
        private float rotationAngle; // 转向角度

        public EnemyWanderState(Enemy character) : base(character)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            //Debug.Log("WanderState");
        }

        public override void OnExit()
        {
            base.OnExit();
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
                    Character.target = PlayerManager.Instance.get(i);
                    return;
                }
            }
            // 漫游
            recordTime += Time.deltaTime;
            if (recordTime >= Character.rotateInterval)
            {
                rotationAngle = Random.Range(0.0f, 360.0f);
                Character.moveSpeed = Random.Range(2.0f, 4.0f);
                recordTime = 0.0f;
            }
            //// 将Vector3转换为Quaternion类型
            //character.transform.rotation = Quaternion.Lerp(character.transform.rotation, Quaternion.Euler(0, 0, rotationAngle), Time.deltaTime * character.rotationSpeed); // (起始方向，终止方向，旋转速度)非匀速
            //character.GetComponent<PhotonView>().RPC("RotateTo", RpcTarget.All, new Vector3(Mathf.Sin(rotationAngle), Mathf.Cos(rotationAngle), 0));
            Character.RotateTo(new Vector3(Mathf.Sin(rotationAngle), Mathf.Cos(rotationAngle), 0));
            //character.GetComponent<PhotonView>().RPC("MoveToForward", RpcTarget.All);
            Character.MoveToForward();
        }
    }
}