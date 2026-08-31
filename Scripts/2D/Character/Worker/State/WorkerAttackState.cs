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

        /// <summary>
        /// "仍在战斗"判定距离：与武器追踪范围（AWeaponObject.raduis=8）一致。
        /// 最近攻击者在此范围内且存活 → Worker 继续反击，不因超时放下武器。
        /// 否则会"退出攻击→被打→又进攻击"反复销毁重建武器，拿起瞬间方向跳变（见 bug-fixes.md 2026-08-16）。
        /// </summary>
        private const float CounterAttackRange = 8.0f;

        /// <summary>
        /// 被打锁定期（秒）：被打后继续攻击当前反击目标不转头。与 Enemy 的"持续攻击几秒"等效——
        /// Enemy 被打会切 Move 重新寻路（有状态切换缓冲），Worker 换目标是即时指针替换，不锁定
        /// 就会"谁攻击 Worker 他立即转头"（见 bug-fixes.md 2026-08-16）。
        /// </summary>
        private const float FocusDuration = 5.0f;

        /// <summary>
        /// 进入攻击状态后的累计时间（专注期时间轴）。OnEnter 置 0、OnUpdate 累加、Reset() 不重置
        /// （Reset 只重置超时计时；被打也会 Reset，若 Reset 清零时间轴，锁定期判断就失去参考）。
        /// </summary>
        public float AttackTime { get; private set; }

        /// <summary>
        /// 被打锁定期截止时刻（以 AttackTime 为轴）。被打时 = AttackTime + FocusDuration，
        /// 期间被任何目标打都不换反击目标；锁定中再次被打不刷新，让锁定按时到期。
        /// OnEnter 重置为极小值，使每次进入攻击的第一次被打都开启新锁定期。
        /// </summary>
        private float focusEndTime = float.MinValue;

        /// <summary>
        /// 打带跑开关：攻击冷却中拉开与目标的距离（风筝），冷却好了再贴近——
        /// 置 false 退化为原地站桩输出。
        /// </summary>
        private const bool HitAndRunEnabled = true;

        /// <summary>
        /// 追击触发距离系数：与目标距离超过 攻击距离×该系数 才追击
        ///（攻击距离边缘的抖动不触发移动）。
        /// </summary>
        private const float ChaseRangeFactor = 1.2f;

        /// <summary>
        /// 上一次声明的战斗移动意图种类（等值短路防每帧风暴，与 Locomotion.SetIntent 短路互补）。
        /// </summary>
        private WorkerMoveIntentKind lastIntentKind = WorkerMoveIntentKind.None;

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
            this.AttackTime = 0f; // 专注期时间轴从进入攻击状态起
            this.focusEndTime = float.MinValue; // 新攻击周期：第一次被打重新锁定当前目标
            // 停止一切移动意图（含战斗类残留意图）+ 清速度防滑行。
            // 原 Seek.StopMove() 已收口：ChangeState 钩子只清 ToMap 意图，
            // 战斗中从上一轮战斗意图（Chase/KeepDistance）切入时需在此显式清空。
            this.Character.Locomotion.Stop();
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

            // 拿起武器后锁定反击目标（当前攻击目标）：进入攻击锁定 LastAttacker，
            // 之后被其他目标打才换、被当前攻击目标打保持——与 Enemy.ReduceHp 对称
            // （见 bug-fixes.md 2026-08-16）。
            this.Character.AttackTarget = this.Character.LastAttacker;

            // 拿起武器后立即朝反击目标（初始朝向），避免武器 prefab 默认朝上（z=0）导致"拿起即拐到一边再拐回来"。
            // 后续武器朝向由 AWeaponObject.Update 按 AimTarget 持续动态跟踪，不固定（见 bug-fixes.md 2026-08-16）。
            if (this.Character.Weapon != null && this.Character.AttackTarget != null)
            {
                Vector3 dirToTarget = this.Character.AttackTarget.transform.position - this.Character.transform.position;
                if (dirToTarget.sqrMagnitude > 0.001f)
                {
                    this.Character.Weapon.transform.rotation = Quaternion.FromToRotation(Vector3.up, dirToTarget);
                }
            }
        }

        /// <summary>
        /// 被打钩子（AWorker.ReduceHp 攻击分支调用）：开启当前目标的锁定期。锁定中
        /// （AttackTime &lt; focusEndTime）再次被打不刷新——否则持续被打会永远刷新锁定、
        /// 永不换目标，与"被攻击就切换目标"的设计冲突。
        /// </summary>
        public void OnHit()
        {
            if (this.AttackTime > this.focusEndTime)
            {
                this.focusEndTime = this.AttackTime + FocusDuration;
            }
        }

        /// <summary>
        /// 当前是否允许换反击目标：被打锁定期已过（AttackTime 超过被打后 FocusDuration）。
        /// </summary>
        public bool CanSwitchTarget() => this.AttackTime > this.focusEndTime;

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            base.OnUpdate();
            this.AttackTime += this.Character.DeltaTime; // 专注期时间轴持续累积（不受 Reset 重置）

            // 超时退出：1.5秒足够完成 1-2 次攻击，避免剑光碰墙后持续无效攻击。
            // 但若仍在战斗（LastAttacker 存活且未远离），继续反击不退场——否则敌人持续攻击时
            // Worker 反复 攻击→Seek→攻击，武器每次退出销毁重建、拿起瞬间方向跳变（见 bug-fixes.md 2026-08-16）。
            this.recordTime += this.Character.DeltaTime;
            if (this.recordTime > 1.5f)
            {
                if (this.IsStillUnderAttack())
                {
                    this.recordTime = 0.0f; // 继续反击，不放下武器
                }
                else
                {
                    this.Character.Manager.ChangeState(TypeEnum.Seek);
                    return;
                }
            }

            if (this.Character.Weapon != null)
            {
                // 战斗移动意图（移动执行由 WorkerLocomotion 在 FixedUpdate 统一驱动，
                // 状态只声明意图——攻击时移动：追击贴近 / 打带跑风筝 / 站定挥砍）。
                this.UpdateCombatIntent();

                // 攻击方向诊断（事件点，仅武器明显偏离反击目标时记录 + 节流 0.5s）：定位"拐到其他方向攻击"
                if (this.Character.AttackTarget != null)
                {
                    Vector3 weaponUp = this.Character.Weapon.transform.up;
                    Vector3 toTarget = this.Character.AttackTarget.transform.position - this.Character.transform.position;
                    float aimAngle = Mathf.Atan2(weaponUp.y, weaponUp.x) * MathHelper.Rad2Deg;
                    float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * MathHelper.Rad2Deg;
                    float dev = Mathf.Abs(Mathf.DeltaAngle(aimAngle, targetAngle));
                    if (dev > 20f)
                    {
                        Vector3 selfPos = this.Character.transform.position;
                        Vector3 targetPos = this.Character.AttackTarget.transform.position;
                        AWorkerTask.LogProviderThrottled(
                            $"{this.Character.name}|AimDev", 0.5f,
                            $"[StateDiag] {this.Character.name}@({selfPos.x:F0},{selfPos.y:F0}) 攻击方向偏差 {dev:0.0}° 武器={aimAngle:0.0}° 目标={this.Character.AttackTarget.name}@({targetPos.x:F0},{targetPos.y:F0}) 目标角={targetAngle:0.0}°",
                            LogManager.LogLevelEnum.Debug);
                    }
                }

                AWeaponObject weaponObject = this.Character.Weapon.GetComponent<AWeaponObject>();
                if (weaponObject != null)
                {
                    // 武器跟踪锁定反击目标（AttackTarget）：防止武器拐向旁边其他角色
                    // （见 bug-fixes.md 2026-08-16）。
                    if (this.Character.AttackTarget != null)
                    {
                        weaponObject.AimTarget = this.Character.AttackTarget.transform;
                    }
                    weaponObject.Attack();
                }
            }
        }

        /// <summary>
        /// 计算并声明战斗移动意图（每帧调用，同参数由 Locomotion.SetIntent 短路防风暴）：
        /// - 目标死亡/丢失 → Stop 站定（等超时退出战斗）；
        /// - 超出攻击距离（&gt;AttackRange×1.2）或攻击就绪 → Chase 贴近；
        /// - 攻击冷却中（HitAndRunEnabled）→ KeepDistance 拉开（打带跑：冷却好了再贴近）；
        /// - 其余（带内且打带跑关闭）→ Stop 站定输出。
        /// </summary>
        private void UpdateCombatIntent()
        {
            Character target = this.Character.AttackTarget;
            WorkerLocomotion locomotion = this.Character.Locomotion;

            if (target == null || target.CharacterDataLAB == null || target.CharacterDataLAB.Hp <= 0f)
            {
                this.DeclareIntent(WorkerMoveIntentKind.None, 0f, false);
                locomotion.Stop();
                return;
            }

            AWeaponObject weaponObject = this.Character.Weapon != null
                ? this.Character.Weapon.GetComponent<AWeaponObject>()
                : null;
            float attackRange = weaponObject != null ? weaponObject.AttackRange : CounterAttackRange;
            bool isReady = weaponObject != null && weaponObject.IsAttackReady;
            float dist = Vector3.Distance(this.Character.transform.position, target.transform.position);

            if (dist > attackRange * ChaseRangeFactor || isReady)
            {
                // 打不着或正好该出手 → 追击贴近
                this.DeclareIntent(WorkerMoveIntentKind.Chase, dist, isReady);
                locomotion.Chase(target, attackRange);
            }
            else if (HitAndRunEnabled && weaponObject != null)
            {
                // 攻击冷却中 → 打带跑：拉开到攻击距离外（带=range+1 ~ range+3），冷却好了再贴近
                this.DeclareIntent(WorkerMoveIntentKind.KeepDistance, dist, isReady);
                locomotion.KeepDistance(target, attackRange + 1f, attackRange + 3f);
            }
            else
            {
                this.DeclareIntent(WorkerMoveIntentKind.None, dist, isReady);
                locomotion.Stop();
            }
        }

        /// <summary>
        /// 意图种类变化时输出节流诊断（同种类不刷——具体参数变化由 Locomotion 的意图日志覆盖）。
        /// </summary>
        private void DeclareIntent(WorkerMoveIntentKind kind, float dist, bool isReady)
        {
            if (kind == this.lastIntentKind)
            {
                return;
            }

            this.lastIntentKind = kind;
            AWorkerTask.LogProviderThrottled(
                $"{this.Character.name}|CombatIntent", 0.5f,
                $"[StateDiag] {this.Character.name} 战斗意图 {kind} dist={dist:F1} ready={isReady}",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 是否仍处于战斗：最近攻击者存活且在反击范围内。
        /// 用于攻击超时后决定"继续反击"还是"放下武器回 Seek"，
        /// 避免敌人持续攻击时反复销毁重建武器（拿起瞬间方向跳变）。
        /// </summary>
        private bool IsStillUnderAttack()
        {
            Character attacker = this.Character.AttackTarget;
            if (attacker == null)
            {
                return false;
            }

            if (attacker.CharacterDataLAB.Hp <= 0f)
            {
                return false; // 攻击者已死亡，战斗结束
            }

            float dist = Vector3.Distance(
                this.Character.transform.position,
                attacker.transform.position);
            return dist <= CounterAttackRange;
        }

        public override void OnExit()
        {
            base.OnExit();
            this.Character.AttackTarget = null; // 退出攻击，不再锁定反击目标

            // 战斗结束显式诊断：任务保持不清空——回 Seek 后 OnEnter 走"有任务"分支
            // 重寻路继续干活（原"打完继续赶路"隐式回路，现显式化便于观测）。
            AWorker.WorkerData exitWd = this.Character.CharacterDataLAB as AWorker.WorkerData;
            AWorkerTask.LogProvider(
                $"[StateDiag] {this.Character.name} 战斗结束, 任务保持 type={exitWd?.Task?.TaskType.ToString() ?? "null"} → 回Seek重寻路",
                LogManager.LogLevelEnum.Debug);

            // 放下武器
            if (this.Character.Weapon != null)
            {
                AWeaponObject weaponObject = this.Character.Weapon.GetComponent<AWeaponObject>();
                if (weaponObject != null)
                {
                    weaponObject.AimTarget = null; // 退出攻击，不再锁定反击目标
                }
                GameObject.Destroy(this.Character.Weapon.gameObject);
                this.Character.Weapon = null;
            }
        }
    }
}
