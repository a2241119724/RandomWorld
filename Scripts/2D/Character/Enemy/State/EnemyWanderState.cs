using Photon.Pun;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace LAB2D
{
    public class EnemyWanderState : CharacterState<Enemy>
    {
        private float _recordTime = 9999.0f; // 记录时间
        private float rotationAngle; // 转向角度
        //private static readonly LayerMask layerMask = LayerMask.GetMask("Tile", "ResourceMap"); // 射线检测层级

        public EnemyWanderState(Enemy character) : base(character)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Character.Target = null;
            // 为了再一次进入会直接转动方向
            _recordTime = 9999.0f;
            //LogManager.Instance.log("WanderState", LogManager.LogLevel.Info);
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
                    Character.Target = PlayerManager.Instance.get(i);
                    return;
                }
            }
            // 感知到周围有活着的工作者，进入追踪状态
            count = WorkerManager.Instance.count();
            for (int i = 0; i < count; i++)
            {
                if (Character.SenseNearby(WorkerManager.Instance.get(i).transform))
                {
                    Character.Manager.changeState(EnemyStateType.Chase);
                    Character.Target = WorkerManager.Instance.get(i);
                    return;
                }
            }
            //RaycastHit2D raycastHit2D = Physics2D.Raycast(Character.transform.position, 
            //    Character.EnemyHead.position - Character.transform.position, 1, layerMask); // (源,方向,距离,层级)
            // 漫游
            _recordTime += Time.deltaTime;
            if (_recordTime >= Character.rotateInterval)
            {
                rotationAngle = Random.Range(0.0f, 360.0f);
                Character.moveSpeed = Random.Range(3.0f, 4.0f);
                _recordTime = 0.0f;
            }
            //// 将Vector3转换为Quaternion类型
            //character.transform.rotation = Quaternion.Lerp(character.transform.rotation, Quaternion.Euler(0, 0, rotationAngle), Time.deltaTime * character.rotationSpeed); // (起始方向，终止方向，旋转速度)非匀速
            //character.GetComponent<PhotonView>().RPC("RotateTo", RpcTarget.All, new Vector3(Mathf.Sin(rotationAngle), Mathf.Cos(rotationAngle), 0));
            Vector3 direction = new Vector3(Mathf.Sin(rotationAngle), Mathf.Cos(rotationAngle), 0);
            Character.rotateTo(direction);
            //character.GetComponent<PhotonView>().RPC("MoveToForward", RpcTarget.All);
            // 先转再移动
            float angle = Quaternion.Angle(Character.transform.rotation, Quaternion.FromToRotation(Vector3.up, direction));
            if (angle < 1.0f)
            {
                Character.moveToForward();
            }
        }
    }
}