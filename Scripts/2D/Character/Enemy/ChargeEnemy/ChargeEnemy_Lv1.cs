namespace LAB2D.Character.Enemy.ChargeEnemy
{
    using LAB2D;
    using LAB2D.Character.Enemy.SeekEnemy;
    using LAB2D.Character.Enemy.SeekEnemy.State;
    using LAB2D.Domain.Common;
    using Photon.Pun;
    using UnityEngine;

    /// <summary>
    /// 冲锋妖兽（野猪）— 高速贴近、近战冲撞。
    /// Attack 用刀光特效走 AttackEffect 粒子碰撞通路（可拆建筑），补上旧 Seek 系近战不拆墙的缺口。
    /// </summary>
    public class ChargeEnemy_Lv1 : ASeekEnemy
    {
        /// <inheritdoc/>
        public override void Start()
        {
            EnemyData enemyData = this.CharacterDataLAB as EnemyData;
            enemyData.AttackRange = enemyData.SightRange = 5.5f;
            enemyData.SoundRange = 5.0f;
            base.Start();
            this.MoveSpeed = 3.2f; // 冲锋野猪比常速快（AEnemy.Start 写死 2f，此处覆盖）

            // 添加状态（复用 Seek 系四态机）
            this.Manager.AddState(ASeekEnemyState.TypeEnum.Seek, new SeekEnemySeekState(this));
            this.Manager.AddState(ASeekEnemyState.TypeEnum.Move, new SeekEnemyMoveState(this));
            this.Manager.AddState(ASeekEnemyState.TypeEnum.Dead, new SeekEnemyDeadState(this));
            this.Manager.AddState(ASeekEnemyState.TypeEnum.Attack, new SeekEnemyAttackState(this));

            // 初始化状态
            this.Manager.ChangeState(ASeekEnemyState.TypeEnum.Seek);
            this.Target = null;
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void Attack()
        {
            EnemyData enemyData = this.CharacterDataLAB as EnemyData;

            // 近战冲撞：原地刀光（AttackEffect 粒子碰撞命中无 Character 的碰撞体时走 DamageBuildingAt，可拆墙）
            // ASeekEnemy 系无 Head 属性（ACommonEnemy 特有），发射点用根 transform
            ParticleSystem ps = Core.GameServices.AttackEffectProvider(AttackEffectManager.EffectTypeEnum.KnifeLight, (this.transform.rotation.eulerAngles.z + 90) * MathHelper.Deg2Rad);
            ps.transform.parent = this.transform.parent;
            ps.transform.position = this.transform.position;
            ps.Play();
            AttackEffect ae = ps.GetComponent<AttackEffect>();
            ae.AttackLayers = this.AttackLayers;
            ae.AttackTags = this.AttackTags;
            ae.Damage = enemyData.GetDamage(false);
            ae.Onwer = this;
        }
    }
}
