namespace LAB2D
{
    using Photon.Pun;
    using UnityEngine;

    /// <summary>
    /// 敌人.
    /// </summary>
    public class Enemy_Lv1 : Enemy
    {
        /// <inheritdoc/>
        [PunRPC]
        public override void Attack()
        {
            // 发射子弹
            GameObject g = Tool.Instantiate(ResourceManager.Instance.GetPrefab("EnemyBullet"), this.EnemyHead.position, Quaternion.identity);

            // GameObject g = Instantiate(enemyBullet, enemyHead.position, Quaternion.identity);
            g.GetComponent<EnemyBullet>().Direction = this.EnemyHead.position - this.transform.position;
            g.GetComponent<EnemyBullet>().BulletSpeed = this.bulletSpeed;
            this.damage = UnityEngine.Random.Range(1, 10);
            g.GetComponent<EnemyBullet>().Damage = this.damage;
            g.transform.SetParent(this.transform.parent, false);
        }

        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            this.name = "Enemy_Lv1";
        }

        /// <inheritdoc/>
        protected override void Start()
        {
            // 画视觉,听觉,攻击范围
            Tool.DrawSectorSolid(10, this.AttackRange, new Color32(255, 0, 0, 50), this.transform);
            Tool.DrawSectorSolid(this.SightAngle, this.SightRange, new Color32(0, 255, 0, 50), this.transform);
            Tool.DrawSectorSolid(360, this.SoundRange, new Color32(0, 0, 255, 50), this.transform);
            base.Start();

            // 添加状态
            this.Manager.AddState(EnemyState.EnemyStateTypeEnum.Wander, new EnemyWanderState(this));
            this.Manager.AddState(EnemyState.EnemyStateTypeEnum.Chase, new EnemyChaseState(this));
            this.Manager.AddState(EnemyState.EnemyStateTypeEnum.Dead, new EnemyDeadState(this));
            this.Manager.AddState(EnemyState.EnemyStateTypeEnum.Seek, new EnemySeekState(this));
            this.Manager.AddState(EnemyState.EnemyStateTypeEnum.Attack, new EnemyAttackState(this));

            // 初始化状态
            this.Manager.ChangeState(EnemyState.EnemyStateTypeEnum.Wander);
            this.Target = null;
        }
    }
}
