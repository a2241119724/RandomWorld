namespace LAB2D
{
    using Photon.Pun;
    using UnityEngine;

    /// <summary>
    /// 敌人.
    /// </summary>
    public class CommonEnemy_Lv1 : ACommonEnemy
    {
        /// <inheritdoc/>
        public override void Start()
        {
            EnemyData enemyData = this.CharacterDataLAB as EnemyData;
            enemyData.AttackRange = 7.0f;
            base.Start();

            // 添加状态
            this.Manager.AddState(ACommonEnemyState.TypeEnum.Wander, new CommonEnemyWanderState(this));
            this.Manager.AddState(ACommonEnemyState.TypeEnum.Chase, new CommonEnemyChaseState(this));
            this.Manager.AddState(ACommonEnemyState.TypeEnum.Dead, new CommonEnemyDeadState(this));
            this.Manager.AddState(ACommonEnemyState.TypeEnum.Seek, new CommonEnemySeekState(this));
            this.Manager.AddState(ACommonEnemyState.TypeEnum.Attack, new CommonEnemyAttackState(this));

            // 初始化状态
            this.Manager.ChangeState(ACommonEnemyState.TypeEnum.Wander);
            this.Target = null;
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void Attack()
        {
            EnemyData enemyData = this.CharacterDataLAB as EnemyData;

            // 发射子弹
            ParticleSystem ps = AttackEffectManager.Instance.GetEffect(AttackEffectManager.EffectTypeEnum.Bullet, (this.transform.rotation.eulerAngles.z + 90) * Mathf.Deg2Rad);
            ps.transform.parent = this.transform.parent;
            ps.transform.position = this.Head.position;
            ps.Play();
            AttackEffect ae = ps.GetComponent<AttackEffect>();
            ae.AttackLayers = this.AttackLayers;
            ae.AttackTags = this.AttackTags;
            ae.Speed = enemyData.BulletSpeed;
            ae.Damage = enemyData.GetDamage(false);
            ae.Onwer = this;
        }
    }
}
