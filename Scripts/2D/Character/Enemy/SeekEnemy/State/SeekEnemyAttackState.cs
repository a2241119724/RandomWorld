namespace LAB2D
{
    using Photon.Pun;
    using UnityEngine;

    public class SeekEnemyAttackState : ASeekEnemyState
    {
        private float recordTime = 0.0f;

        public SeekEnemyAttackState(ASeekEnemy character)
        : base(character)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            this.Character.Seek.StopMove();
            this.recordTime = 0.0f;

            // 拿起武器
            if (this.Character.Weapon == null && this.Character.CharacterDataLAB.Weapon != null)
            {
                // 实例化武器
                string name = ItemDataManager.Instance.GetById(this.Character.CharacterDataLAB.Weapon.Id).EnName;
                this.Character.Weapon = ResourceManager.Instance.Instantiate(name, false);
                if (this.Character.Weapon == null)
                {
                    LogManager.Instance.Log("武器实例化错误!", LogManager.LogLevelEnum.Error);
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
            this.recordTime += Time.deltaTime;
            if (this.recordTime >= 2.0f)
            {
                this.Character.Manager.ChangeState(TypeEnum.Seek);
                return;
            }

            AWeaponObject weaponObject = this.Character.Weapon.GetComponent<AWeaponObject>();
            weaponObject.Attack();
            if (NetworkConnect.Instance.IsOnline)
            {
                this.Character.pv.RPC("Attack", RpcTarget.All);
            }
            else
            {
                this.Character.Attack();
            }
        }

        public override void OnExit()
        {
            base.OnExit();

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
            this.recordTime = 0.0f;
        }
    }
}
