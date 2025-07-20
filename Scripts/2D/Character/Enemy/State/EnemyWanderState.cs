namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 敌人漫游状态.
    /// </summary>
    public class EnemyWanderState : CharacterState<Enemy>
    {
        private float recordTime = 9999.0f; // 记录时间
        private float rotationAngle; // 转向角度

        // private static readonly LayerMask layerMask = LayerMask.GetMask("Tile", "ResourceMap"); // 射线检测层级
        public EnemyWanderState(Enemy character)
            : base(character)
        {
        }

        /// <summary>
        /// 进入漫游状态.
        /// </summary>
        public override void OnEnter()
        {
            base.OnEnter();
            this.Character.Target = null;

            // 为了再一次进入会直接转动方向
            this.recordTime = 9999.0f;

            // LogManager.Instance.log("WanderState", LogManager.LogLevel.Info);
        }

        /// <summary>
        /// 退出漫游状态.
        /// </summary>
        public override void OnExit()
        {
            base.OnExit();
        }

        /// <summary>
        /// 漫游状态.
        /// </summary>
        public override void OnUpdate()
        {
            // 感知到周围有活着的玩家，进入追踪状态
            int count = PlayerManager.Instance.Count();
            for (int i = 0; i < count; i++)
            {
                if (this.Character.SenseNearby(PlayerManager.Instance.Get(i).transform))
                {
                    this.Character.Manager.changeState(EnemyStateType.Chase);
                    this.Character.Target = PlayerManager.Instance.Get(i);
                    return;
                }
            }

            // 感知到周围有活着的Worker，进入追踪状态
            count = WorkerManager.Instance.Count();
            for (int i = 0; i < count; i++)
            {
                if (this.Character.SenseNearby(WorkerManager.Instance.Get(i).transform))
                {
                    this.Character.Manager.changeState(EnemyStateType.Chase);
                    this.Character.Target = WorkerManager.Instance.Get(i);
                    return;
                }
            }

            // RaycastHit2D raycastHit2D = Physics2D.Raycast(Character.transform.position,
            //     Character.EnemyHead.position - Character.transform.position, 1, layerMask); // (源,方向,距离,层级)
            // 漫游
            this.recordTime += Time.deltaTime;
            if (this.recordTime >= this.Character.RotateInterval)
            {
                this.rotationAngle = Random.Range(0.0f, 360.0f);
                this.Character.MoveSpeed = Random.Range(3.0f, 4.0f);
                this.recordTime = 0.0f;
            }
            //// 将Vector3转换为Quaternion类型
            // character.transform.rotation = Quaternion.Lerp(character.transform.rotation, Quaternion.Euler(0, 0, rotationAngle), Time.deltaTime * character.rotationSpeed); // (起始方向，终止方向，旋转速度)非匀速
            // character.GetComponent<PhotonView>().RPC("RotateTo", RpcTarget.All, new Vector3(Mathf.Sin(rotationAngle), Mathf.Cos(rotationAngle), 0));
            Vector3 direction = new Vector3(Mathf.Sin(this.rotationAngle), Mathf.Cos(this.rotationAngle), 0);
            this.Character.RotateTo(direction);

            // character.GetComponent<PhotonView>().RPC("MoveToForward", RpcTarget.All);
            // 先转再移动
            float angle = Quaternion.Angle(this.Character.transform.rotation, Quaternion.FromToRotation(Vector3.up, direction));
            if (angle < 1.0f)
            {
                this.Character.MoveToForward();
            }
        }
    }
}