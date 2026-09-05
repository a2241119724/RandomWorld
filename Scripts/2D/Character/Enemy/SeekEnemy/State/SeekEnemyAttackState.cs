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

        /// <summary>
        /// 武器组件缓存：OnUpdate 每帧 GetComponent&lt;AWeaponObject&gt; 是热点
        /// （N=100 敌人战斗时即每帧 100 次反射式查找）。武器在 OnEnter 确保就绪、OnExit 销毁，
        /// 生命周期覆盖整个攻击状态，OnEnter 缓存一次即可；OnExit 置空防跨状态持有已销毁对象。
        /// </summary>
        private AWeaponObject cachedWeaponObject;

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

            // 每帧热点缓存：武器在本状态生命周期内不变（OnExit 才销毁），GetComponent 只做一次
            this.cachedWeaponObject = this.Character.Weapon != null ? this.Character.Weapon.GetComponent<AWeaponObject>() : null;

            // 拿起武器后立即朝攻击目标（初始朝向），避免武器 prefab 默认朝上（z=0）导致"拿起即拐到一边再拐回来"。
            // 后续武器朝向由 AWeaponObject.Update 按范围内最近目标持续动态跟踪，不固定（见 bug-fixes.md 2026-08-16）。
            if (this.Character.Weapon != null && this.Character.Target != null)
            {
                Vector3 dirToTarget = this.Character.Target.transform.position - this.Character.transform.position;
                if (dirToTarget.sqrMagnitude > 0.001f)
                {
                    this.Character.Weapon.transform.rotation = Quaternion.FromToRotation(Vector3.up, dirToTarget);
                }
            }

            // 状态切换：进入攻击状态（节流 2s/条，见 bug-fixes.md 2026-08-15）
            AWorkerTask.LogProviderThrottled(
                $"{this.Character.name}|AttackIn", 2f,
                $"[EnemyDiag] {this.Character.name} → Attack target={this.Character.Target?.name}",
                LogManager.LogLevelEnum.Debug);
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

            // 玩家朝向设置为攻击方向
            Vector3 dirToTarget = this.Character.Target.transform.position - this.Character.transform.position;
            this.Character.Direction = dirToTarget;

            // 设置视觉，攻击方向（直接用目标方向，不依赖武器 rotation：武器由
            // AWeaponObject.Update 动态跟踪，进攻击状态首帧可能尚未矫正，跟随它会导致
            // AttackRange 朝空方向闪一下再拐回，见 bug-fixes.md 2026-08-16）
            Quaternion attackRotation = dirToTarget.sqrMagnitude > 0.001f
                ? Quaternion.FromToRotation(Vector3.up, dirToTarget)
                : this.Character.Weapon.transform.rotation;
            this.Character.SightRange.transform.rotation = attackRotation;
            this.Character.AttackRange.transform.rotation = attackRotation;

            // 攻击方向诊断（事件点，仅武器明显偏离攻击目标时记录 + 节流 0.5s）：定位"拐到其他方向攻击"
            {
                Vector3 weaponUp = this.Character.Weapon.transform.up;
                Vector3 toTarget = this.Character.Target.transform.position - this.Character.transform.position;
                float aimAngle = Mathf.Atan2(weaponUp.y, weaponUp.x) * MathHelper.Rad2Deg;
                float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * MathHelper.Rad2Deg;
                float dev = Mathf.Abs(Mathf.DeltaAngle(aimAngle, targetAngle));
                if (dev > 20f)
                {
                    // 惰性求值：OnUpdate 每帧进入（持续偏离期间每帧都触发），被节流时
                    // 不再每帧付取位置 + 插值串分配
                    AWorkerTask.LogProviderThrottled(
                        $"{this.Character.name}|AimDev", 0.5f,
                        () =>
                        {
                            Vector3 selfPos = this.Character.transform.position;
                            Vector3 targetPos = this.Character.Target.transform.position;
                            return $"[EnemyDiag] {this.Character.name}@({selfPos.x:F0},{selfPos.y:F0}) 攻击方向偏差 {dev:0.0}° 武器={aimAngle:0.0}° 目标={this.Character.Target.name}@({targetPos.x:F0},{targetPos.y:F0}) 目标角={targetAngle:0.0}°";
                        },
                        LogManager.LogLevelEnum.Debug);
                }
            }

            AWeaponObject weaponObject = this.cachedWeaponObject; // OnEnter 缓存，避免每帧 GetComponent

            // 武器跟踪攻击目标（而非范围内最近目标）：防止武器拐向旁边的其他角色，
            // 造成"攻击 player、武器却朝最近的韩东瑜"的方向不一致（见 bug-fixes.md 2026-08-16）。
            if (this.Character.Target != null)
            {
                weaponObject.AimTarget = this.Character.Target.transform;
            }

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
                AWeaponObject weaponObject = this.Character.Weapon.GetComponent<AWeaponObject>();
                if (weaponObject != null)
                {
                    weaponObject.AimTarget = null; // 退出攻击，不再锁定攻击目标
                }
                GameObject.Destroy(this.Character.Weapon.gameObject);
                this.Character.Weapon = null;
            }

            this.cachedWeaponObject = null; // 武器已销毁，防跨状态持有已销毁组件

            // 状态切换：离开攻击状态（节流 2s/条）
            AWorkerTask.LogProviderThrottled(
                $"{this.Character.name}|AttackOut", 2f,
                $"[EnemyDiag] {this.Character.name} ← Attack",
                LogManager.LogLevelEnum.Debug);
        }

        public override void Reset()
        {
            base.Reset();
        }
    }
}
