namespace LAB2D.Character.Enemy.SeekEnemy.State
{
    using LAB2D;
    using Photon.Pun;
    using UnityEngine;

    public class SeekEnemyAttackState : ASeekEnemyState
    {
        private float recordTime = 0.0f;

        public SeekEnemyAttackState(ASeekEnemy character)
        : base(character)
        {
        }

        /// <summary>
        /// 攻击时间
        /// </summary>
        public float AttackTime { get; private set; } = 0.0f;

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            this.recordTime = 0.0f;
            this.AttackTime = 0.0f;
            this.Character.AttackRange.SetActive(true);

            // 拿起武器
            if (this.Character.Weapon == null && this.Character.CharacterDataLAB.Weapon != null)
            {
                // 实例化武器
                string name = AWorkerTask.ItemDataProvider(this.Character.CharacterDataLAB.Weapon.Id).Name;
                this.Character.Weapon = Core.GameServices.ResourceInstantiateProvider(name, false);
                if (this.Character.Weapon == null)
                {
                    AWorkerTask.LogProvider("武器实例化错误!", LogManager.LogLevelEnum.Error);
                    return;
                }

                this.Character.Weapon.name = name;
                this.Character.Weapon.transform.SetParent(this.Character.transform, false);
                AWeaponObject weaponObject = this.Character.Weapon.GetComponent<AWeaponObject>();
                weaponObject.SetCharacter(this.Character);
            }
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            base.OnUpdate();
            this.AttackTime += this.Character.DeltaTime;

            // 打击目标死亡
            if (this.Character.Target == null)
            {
                this.Character.Manager.ChangeState(TypeEnum.Seek);
                return;
            }

            // 设置视觉，攻击觉方向
            this.Character.SightRange.transform.rotation = this.Character.Weapon.transform.rotation;
            this.Character.AttackRange.transform.rotation = this.Character.Weapon.transform.rotation;

            // 玩家朝向设置为攻击方向
            this.Character.Direction = this.Character.Target.transform.position - this.Character.transform.position;
            AWeaponObject weaponObject = this.Character.Weapon.GetComponent<AWeaponObject>();
            weaponObject.Attack();
            if (this.Character.NetworkView.IsOnline)
            {
                this.Character.NetworkView.RPC("Attack", RpcTarget.All);
            }
            else
            {
                this.Character.Attack();
            }

            if (!this.Character.SenseNearby(this.Character.Target.transform))
            {
                // 追踪两秒
                this.recordTime += this.Character.DeltaTime;
                if (this.recordTime >= 2.0f)
                {
                    this.Character.Manager.ChangeState(TypeEnum.Seek);
                }
            }
            else
            {
                this.recordTime = 0.0f;
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            this.Character.AttackRange.SetActive(false);

            // 放下武器
            if (this.Character.Weapon != null)
            {
                GameObject.Destroy(this.Character.Weapon.gameObject);
                this.Character.Weapon = null;
            }
        }

        public override void Reset()
        {
            base.Reset();
        }
    }
}
