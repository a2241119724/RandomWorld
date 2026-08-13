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
                string name = AWorkerTask.ItemDataProvider(workerData.Weapon.Id).Name;
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
                weaponObject.Item = workerData.Weapon;
            }
            else
            {
                // 攻击入口诊断（事件点）：进入攻击状态但无可用武器，立即转为逃跑。
                AWorkerTask.LogProvider(
                    $"[StateDiag] {this.Character.name} 进入攻击状态但无武器, 转为逃跑",
                    LogManager.LogLevelEnum.Debug);
                this.Character.Manager.ChangeState(TypeEnum.Escape);
            }
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            base.OnUpdate();

            // 超时退出：1.5秒足够完成 1-2 次攻击，避免剑光碰墙后持续无效攻击
            this.recordTime += this.Character.DeltaTime;
            if (this.recordTime > 1.5f)
            {
                this.Character.Manager.ChangeState(TypeEnum.Seek);
                return;
            }

            if (this.Character.Weapon != null)
            {
                AWeaponObject weaponObject = this.Character.Weapon.GetComponent<AWeaponObject>();
                if (weaponObject != null)
                {
                    weaponObject.Attack();
                }
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
    }
}
