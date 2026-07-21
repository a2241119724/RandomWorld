namespace LAB2D.Character.Worker.State
{
    using LAB2D;
    using UnityEngine;

    /// <summary>
    /// Worker攻击状态
    /// </summary>
    public class WorkerAttackState : AWorkerState
    {
        private float recordTime = 0.0f;

        public WorkerAttackState(AWorker worker)
            : base(worker)
        {
        }

        public override void Reset()
        {
            this.recordTime = 0.0f;
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            this.Reset();
            this.Character.Seek.StopMove();
            this.Character.WorkerStateText.text = this.preString;

            // 拿起武器
            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;
            if (this.Character.Weapon == null && workerData.Weapon != null)
            {
                // 实例化武器
                string name = AWorkerTask.ItemDataProvider(workerData.Weapon.Id).EnName;
                this.Character.Weapon = AWorkerTask.ResourceInstantiateProvider(name, false);
                if (this.Character.Weapon == null)
                {
                    LogManager.Instance.Log("武器实例化错误!", LogManager.LogLevelEnum.Error);
                    return;
                }

                this.Character.Weapon.name = name;
                this.Character.Weapon.transform.SetParent(this.Character.transform, false);
                AWeaponObject weaponObject = this.Character.Weapon.GetComponent<AWeaponObject>();
                weaponObject.SetCharacter(this.Character);
                weaponObject.Item = workerData.Weapon;
            }
            else
            {
                this.Character.Manager.ChangeState(TypeEnum.Escape);
            }
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            base.OnUpdate();

            // 若一段时间没有被攻击，那么回到寻路状态
            this.recordTime += this.Character.DeltaTime;
            if (this.recordTime > 5)
            {
                this.Character.Manager.ChangeState(TypeEnum.Seek);
                return;
            }

            AWeaponObject weaponObject = this.Character.Weapon.GetComponent<AWeaponObject>();
            weaponObject.Attack();
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
    }
}
