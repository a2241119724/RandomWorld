namespace LAB2D.Character.Enemy.SeekEnemy.State
{
    using LAB2D;
    using LAB2D.Domain.Time;
    using Photon.Pun;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 攻城状态（P4 Defend 重设计；W3 目标扩展 + 全天化）— 敌人追不上角色时的第二目标：
    /// 寻路至最近阵营核心邻格朝核心开火（子弹命中核心格 → BuildMap.DamageBuilding
    /// → FactionManager.DamageCoreAt）；无阵营核心时拆最近已建成房间墙格（夜袭民宅，
    /// 子弹撞墙 collider 走啃墙管线）。感知到角色回 Attack（先经 Move）、
    /// 目标建筑不存在（核心陷落或墙被拆穿）回 Seek 漫游换目标（不分昼夜）。
    /// 感知严格复制 Move 状态的 30 帧（0.5s）相位轮询 + 单目标轮询，禁逐帧分配。
    /// </summary>
    public class SeekEnemySiegeState : ASeekEnemyState
    {
        /// <summary>视为抵达目标跟前、开始开火的 Chebyshev 距离（格）。</summary>
        private const int FireDistance = 3;

        /// <summary>攻城卡死熔断次数（围城走位被挡时放弃回 Seek）。</summary>
        private const int MaxStuckStreak = 3;

        /// <summary>攻城目标格（阵营核心主格或房间墙格）。</summary>
        private Vector3Int coreMap;

        /// <summary>目标是阵营核心（true 走 IsCoreCell 存活判定；false 走 PosMap 建筑存在判定）。</summary>
        private bool isCoreTarget;

        /// <summary>武器组件缓存（OnEnter 缓存一次，OnExit 随武器销毁置空，照 AttackState）。</summary>
        private AWeaponObject cachedWeaponObject;

        /// <summary>感知帧相位（实例 id 低 6 位，照 MoveState 错峰，30 帧周期）。</summary>
        private readonly int sensePhase;

        /// <summary>感知目标轮询索引（每感知帧只检查一个目标）。</summary>
        private int senseTargetIndex;

        private int stuckStreak;

        public SeekEnemySiegeState(ASeekEnemy character)
            : base(character)
        {
            this.sensePhase = character.GetInstanceID() & 63;
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();

            // 选定攻城目标（W3 扩展 + 全天）：最近已放置阵营核心优先；无核心 → 最近已建成
            // 房间墙格；全无 → 回 Seek 漫游
            Vector3Int selfMap = AWorkerTask.TileMapWorldToMapProvider(this.Character.transform.position);
            Vector3Int? target = null;
            if (Core.ServiceLocator.TryGet(out Gameplay.FactionManager factionManager)
                && factionManager.GetNearestPlacedCore(selfMap) is Vector3Int core)
            {
                target = core;
                this.isCoreTarget = true;
            }
            else
            {
                target = FindNearestWallCell(selfMap);
                this.isCoreTarget = false;
            }

            if (target == null)
            {
                this.Character.Manager.ChangeState(TypeEnum.Seek);
                return;
            }

            this.coreMap = target.Value;
            this.stuckStreak = 0;

            // 拿起武器（照 AttackState：实例化 + 挂载 + 初始朝向核心）
            if (this.Character.Weapon == null && this.Character.CharacterDataLAB.Weapon != null)
            {
                string weaponName = AWorkerTask.ItemDataProvider(this.Character.CharacterDataLAB.Weapon.Id).Name;
                this.Character.Weapon = Core.GameServices.ResourceInstantiateProvider(weaponName, false);
                if (this.Character.Weapon == null)
                {
                    AWorkerTask.LogProvider("武器实例化错误!", LogManager.LogLevelEnum.Error);
                    this.Character.Manager.ChangeState(TypeEnum.Seek);
                    return;
                }

                this.Character.Weapon.name = weaponName;
                this.Character.Weapon.transform.SetParent(this.Character.transform, false);
                AWeaponObject weaponObject = this.Character.Weapon.GetComponent<AWeaponObject>();
                weaponObject.SetCharacter(this.Character);
            }

            this.cachedWeaponObject = this.Character.Weapon != null
                ? this.Character.Weapon.GetComponent<AWeaponObject>()
                : null;

            // 锁定武器朝向：攻城必须朝建筑目标开火（子弹撞墙格走拆家管线），不锁会被
            // 范围内最近角色劫持武器方向——守卫靠近时子弹全打守卫、拆不动墙，且宝剑与
            // 感知扇形朝向分叉。朝向由 OnUpdate 每帧写 siegeRotation 维持。
            if (this.cachedWeaponObject != null)
            {
                this.cachedWeaponObject.LockAim = true;
            }

            // 寻路至核心附近可走格（核心主格不可走）
            Vector3Int siegePos = AWorkerTask.GenCanReachPosProvider(this.coreMap);
            this.Character.Seek.Seek(siegePos);

            AWorkerTask.LogProvider(
                $"[EnemyDiag] {this.Character.name} → Siege {(this.isCoreTarget ? "core" : "wall")}=({this.coreMap.x},{this.coreMap.y}) 集结=({siegePos.x},{siegePos.y})",
                LogManager.LogLevelEnum.Debug);
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            base.OnUpdate();

            // 目标建筑不存在（核心陷落/解散，或墙格被拆穿）→ 回 Seek 换目标
            bool targetExists = this.isCoreTarget
                ? Core.ServiceLocator.TryGet(out Gameplay.FactionManager factionManager)
                    && factionManager.IsCoreCell(this.coreMap)
                : Core.ServiceLocator.TryGet(out LAB2D.Map.BuildMap buildMap)
                    && buildMap.GetBuildTileData(this.coreMap) != null;
            if (!targetExists)
            {
                this.Character.Manager.ChangeState(TypeEnum.Seek);
                return;
            }

            // 感知轮询（照 MoveState：30 帧 ≈0.5s 相位 × 单目标轮询）— 感知到角色立即转攻
            if (this.Character.CharacterDataLAB.Weapon != null
                && (Time.frameCount + this.sensePhase) % 30 == 0)
            {
                int playerCount = Core.GameServices.PlayerCountProvider();
                int workerCount = Core.GameServices.WorkerCountProvider();
                int totalTargets = playerCount + workerCount;
                if (totalTargets > 0)
                {
                    this.senseTargetIndex = (this.senseTargetIndex + 1) % totalTargets;
                    Transform target = this.senseTargetIndex < playerCount
                        ? Core.GameServices.PlayerGetProvider(this.senseTargetIndex).transform
                        : Core.GameServices.WorkerGetProvider(this.senseTargetIndex - playerCount).transform;

                    if (target != null && this.Character.SenseNearby(target))
                    {
                        // 距离门槛（对齐 MoveState.TryEnterAttack 的 attackGate）：感知命中
                        // 只说明看得见（Sight=10/Sound=4），守卫在门槛外不转攻——继续拆家
                        // 等它自己靠近。原实现无门槛直接切 Attack，进 Attack 首帧即被
                        // 脱离判定（门槛×1.2）踢回 Move：Siege→Attack→Move 7ms 一圈
                        //（game.log 2026-09-08 13:02:10.906-913 实证 dist=4.8），每圈
                        // 拿放武器 + Move.OnEnter 强制重寻路（异步等待期站定）——围殴下
                        // 反复横跳 = 用户报告"被攻击站着不动但偶尔正常攻击"。
                        AEnemy.EnemyData enemyData = this.Character.CharacterDataLAB as AEnemy.EnemyData;
                        float attackGate = Mathf.Min(
                            enemyData.AttackRange,
                            this.Character.CharacterDataLAB.Weapon?.AttackReach ?? enemyData.AttackRange);
                        if (Vector3.Distance(target.position, this.Character.transform.position) <= attackGate)
                        {
                            // 先赋 Target 再切状态（OnEnter 初始朝向读目标，见 bug-fixes.md 2026-08-16）
                            this.Character.Target = target.GetComponent<LAB2D.Character.Character>();
                            this.Character.Manager.ChangeState(TypeEnum.Attack);
                            AWorkerTask.LogProvider(
                                $"[EnemyDiag] {this.Character.name} 攻城中感知到 {target.name}，转攻",
                                LogManager.LogLevelEnum.Debug);
                            return;
                        }

                        // 门槛外：感知到了但不追（转攻丢拆家目标），留在 Siege 继续攻城
                    }
                }
            }

            // 朝向核心（视觉 + 武器）
            Vector3 coreWorld = AWorkerTask.TileMapPositionProvider(this.coreMap);
            Vector3 dirToCore = coreWorld - this.Character.transform.position;
            this.Character.Direction = dirToCore;
            Quaternion siegeRotation = dirToCore.sqrMagnitude > 0.001f
                ? Quaternion.FromToRotation(Vector3.up, dirToCore)
                : this.Character.Weapon.transform.rotation;
            this.Character.SightRange.transform.rotation = siegeRotation;
            this.Character.AttackRange.transform.rotation = siegeRotation;
            if (this.Character.Weapon != null && dirToCore.sqrMagnitude > 0.001f)
            {
                // 武器同朝向（LockAim 已让 AWeaponObject.Update 不跟踪目标，这里维持攻城朝向）
                this.Character.Weapon.transform.rotation = siegeRotation;
            }

            // 抵达核心跟前 → 开火（子弹撞核心格 collider 走啃墙管线）
            bool reached = dirToCore.sqrMagnitude <= FireDistance * FireDistance; // 格≈世界单位 1:1
            if (reached && this.cachedWeaponObject != null)
            {
                this.cachedWeaponObject.AimTarget = null; // 固定朝向核心，不锁角色
                this.cachedWeaponObject.Attack();
                if (this.Character.NetworkView.IsOnline)
                {
                    this.Character.NetworkView.RPC("Attack", RpcTarget.All);
                }
                else
                {
                    this.Character.Attack();
                }
            }
        }

        /// <inheritdoc/>
        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            this.Character.Seek.MoveByPath();

            // 攻城走位卡死熔断（核心被围死/寻路不可达）：放弃回 Seek 换目标
            if (this.Character.Seek.LastStuckResult == BugCheckResult.None)
            {
                this.stuckStreak = 0;
                return;
            }

            if (++this.stuckStreak >= MaxStuckStreak)
            {
                this.stuckStreak = 0;
                this.Character.AbandonMovementStuck();
            }
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
            this.Character.Seek.StopMove();
            this.Character.Direction = Vector3.zero; // 清攻城方向：感知扇形回落到寻路方向

            // 放下武器（照 AttackState.OnExit：解除锁定 + 销毁）
            if (this.Character.Weapon != null)
            {
                AWeaponObject weaponObject = this.Character.Weapon.GetComponent<AWeaponObject>();
                if (weaponObject != null)
                {
                    weaponObject.AimTarget = null;
                    weaponObject.LockAim = false;
                }

                GameObject.Destroy(this.Character.Weapon.gameObject);
                this.Character.Weapon = null;
            }

            this.cachedWeaponObject = null;

            AWorkerTask.LogProviderThrottled(
                $"{this.Character.name}|SiegeOut", 2f,
                $"[EnemyDiag] {this.Character.name} ← Siege",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 找最近已建成房间墙格（W3 夜袭民宅）：遍历房间 Points 取距离最近且建筑在位
        /// （PosMap 有条目且 IsComplete——啃建造中的空格无伤害）的格。
        /// OnEnter 事件点（夜间 8s 失联才进本状态），遍历可承受。
        /// </summary>
        private static Vector3Int? FindNearestWallCell(Vector3Int fromMap)
        {
            if (!Core.ServiceLocator.TryGet(out Item.RoomManager roomManager))
            {
                return null;
            }

            LAB2D.Map.BuildMap buildMap = Core.ServiceLocator.TryGet(out LAB2D.Map.BuildMap bm)
                ? bm
                : null;
            Vector3Int best = default;
            int bestDist = int.MaxValue;
            foreach (KeyValuePair<string, Item.RoomInfo> kv in roomManager.GetAllRooms())
            {
                Item.RoomInfo room = kv.Value;
                if (room.Points == null)
                {
                    continue;
                }

                foreach (Vector3Int p in room.Points)
                {
                    int dx = p.x - fromMap.x;
                    int dy = p.y - fromMap.y;
                    int dist = dx * dx + dy * dy;
                    if (dist >= bestDist)
                    {
                        continue;
                    }

                    LAB2D.Map.BuildMap.BuildTileData tile = buildMap?.GetBuildTileData(p);
                    if (tile == null || !tile.IsComplete)
                    {
                        continue; // 墙已被拆/尚未建成：不是有效目标
                    }

                    bestDist = dist;
                    best = p;
                }
            }

            return bestDist == int.MaxValue ? null : best;
        }
    }
}
