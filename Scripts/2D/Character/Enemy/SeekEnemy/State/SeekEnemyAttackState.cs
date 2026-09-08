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
        /// 被打时重置攻击持续时长（ASeekEnemy.ReduceHp 调用，2026-09-08 用户报告"被打突然走开"）：
        /// 换目标判定"AttackTime > ChangeTarget"语义从"本状态累计 5s"变为"距上次挨打 5s"——
        /// 贴身互殴中每刀重置，敌人不会在持续交战中被其他攻击者拉走（攻击状态只被
        /// "5 秒没挨打"的新攻击者打断）。AttackTime 唯一外部读取处就是该判定，重置无副作用。
        /// </summary>
        public void ResetAttackTimeOnHit() => this.AttackTime = 0.0f;

        /// <summary>
        /// 攻击被墙遮挡的放弃阈值（秒）：武器持续就绪却被墙挡（发射不出）超阈值 →
        /// 放弃目标回 Seek 换目标漫游。治"互相看见却打不到"的隔墙僵持
        /// ——双方锁着对方空转谁也不退（2026-09-08 用户报告，同 WorkerAttackState）。
        /// </summary>
        private const float AttackBlockedGiveUpSeconds = 3f;

        /// <summary>
        /// 攻击脱离距离系数：与目标距离超过 AttackRange×本值才退出攻击回 Move 追击。
        /// 滞回防攻击距离边缘 Attack↔Move 抖动（对齐 WorkerAttackState.ChaseRangeFactor=1.2）。
        /// 目标后撤拉远时武器特效实际够不着还站定空挥——检测命中是开火许可，
        /// 不是持续站桩许可（修"实际攻击范围与检测范围不一致"2026-09-08）。
        /// </summary>
        private const float LeaveRangeFactor = 1.2f;

        /// <summary>
        /// 武器组件缓存：OnUpdate 每帧 GetComponent&lt;AWeaponObject&gt; 是热点
        /// （N=100 敌人战斗时即每帧 100 次反射式查找）。武器在 OnEnter 确保就绪、OnExit 销毁，
        /// 生命周期覆盖整个攻击状态，OnEnter 缓存一次即可；OnExit 置空防跨状态持有已销毁对象。
        /// </summary>
        private AWeaponObject cachedWeaponObject;

        // ---- AimDev 日志零分配化（GC 排查 2026-09-06，仿 ASeek seekPathDiag 范本）----
        // 偏离持续期间 OnUpdate 每帧进入：key 插值（含 .name getter，每次跨原生分配）与
        // 捕获局部变量的闭包即使被节流也每次分配。key/委托缓存 + 偏移量拷实例字段，
        // 命中节流时零分配；委托体只在真正打日志时求值（0.5s 一条）。
        private string aimDevKey;
        private System.Func<string> aimDevLog;
        private float aimDevDeg;    // 委托读（触发时刻的攻击方向偏差角）
        private float aimWeaponDeg; // 委托读（触发时刻的武器朝向角）
        private float aimTargetDeg; // 委托读（触发时刻的目标方向角）

        private string BuildAimDevLog()
        {
            Vector3 selfPos = this.Character.transform.position;
            Vector3 targetPos = this.Character.Target.transform.position;
            return $"[EnemyDiag] {this.Character.name}@({selfPos.x:F0},{selfPos.y:F0}) 攻击方向偏差 {this.aimDevDeg:0.0}° 武器={this.aimWeaponDeg:0.0}° 目标={this.Character.Target.name}@({targetPos.x:F0},{targetPos.y:F0}) 目标角={this.aimTargetDeg:0.0}°";
        }

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

            // 脱离攻击距离（×滞回系数）→ 回 Move 追击重新贴近：Move.OnEnter 追击分支
            // 按 Target 重新寻路（Target 在本状态 OnExit 不清，仅 Seek.OnEnter 清）。
            // 门槛与 SeekEnemyMoveState.TryEnterAttack 同源：min(AttackRange, 武器实际
            // 射程 AttackReach)——剑光真实命中 ~1 格，单用 AttackRange=4 拿剑敌人
            // 贴到 4 格站定仍空挥（2026-09-08 第二轮）。
            {
                AEnemy.EnemyData enemyData = this.Character.CharacterDataLAB as AEnemy.EnemyData;
                float attackGate = Mathf.Min(
                    enemyData.AttackRange,
                    this.Character.CharacterDataLAB.Weapon?.AttackReach ?? enemyData.AttackRange);
                float dist = Vector3.Distance(
                    this.Character.transform.position, this.Character.Target.transform.position);
                if (dist > attackGate * LeaveRangeFactor)
                {
                    AWorkerTask.LogProvider(
                        $"[EnemyDiag] {this.Character.name} 目标脱离攻击距离 dist={dist:F1} > {attackGate * LeaveRangeFactor:F1} → Move 追击",
                        LogManager.LogLevelEnum.Debug);
                    this.Character.Manager.ChangeState(TypeEnum.Move);
                    return;
                }
            }

            // 玩家朝向设置为攻击方向（近零保护：与目标重合时不写零向量——
            // 零方向会让感知扇形 Vector3.Angle 恒 90° 不命中 + 扇形渲染抖动）
            Vector3 dirToTarget = this.Character.Target.transform.position - this.Character.transform.position;
            if (dirToTarget.sqrMagnitude > 0.0001f)
            {
                this.Character.Direction = dirToTarget;
            }

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
                    // 零分配节流：偏移量拷字段供委托读，key/委托缓存复用（见字段区注释）
                    this.aimDevDeg = dev;
                    this.aimWeaponDeg = aimAngle;
                    this.aimTargetDeg = targetAngle;
                    this.aimDevKey ??= $"{this.Character.name}|AimDev";
                    this.aimDevLog ??= this.BuildAimDevLog;
                    AWorkerTask.LogProviderThrottled(
                        this.aimDevKey, 0.5f,
                        this.aimDevLog,
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

            // 隔墙僵持熔断：武器持续就绪却被墙挡（发射不出）超阈值 → 放弃目标回 Seek
            //（Seek.OnEnter 清 Target 选漫游格，不会原地重锁同一目标）
            if (weaponObject.AttackBlockedSeconds > AttackBlockedGiveUpSeconds)
            {
                AWorkerTask.LogProvider(
                    $"[EnemyDiag] {this.Character.name} 攻击被墙遮挡 {weaponObject.AttackBlockedSeconds:F1}s，放弃目标 → 回Seek",
                    LogManager.LogLevelEnum.Debug);
                this.Character.Manager.ChangeState(TypeEnum.Seek);
                return;
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
            this.Character.Direction = Vector3.zero; // 清攻击方向：Direction getter 攻击方向优先，清零后感知扇形回落到寻路方向

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
