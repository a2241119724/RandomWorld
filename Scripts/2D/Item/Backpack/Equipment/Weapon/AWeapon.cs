namespace LAB2D.Item.Backpack.Equipment.Weapon
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using LAB2D.UnityAdapter;
    using Character = LAB2D.Character.Character;
    using System;
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;

    /// <summary>
    /// 剑
    /// </summary>
    [Serializable]
    public abstract class AWeapon : AEquipment
    {
        public AWeapon()
        {
            this.Type = EquipTypeEnum.Weapon;
        }
    }

    /// <summary>
    /// 武器对象
    /// </summary>
    public abstract class AWeaponObject : ABackpackItemObject, IPunObservable
    {
        /// <summary>
        /// 攻击时间
        /// </summary>
        protected float attackInterval = 1.0f;

        /// <summary>
        /// 最近的敌人
        /// </summary>
        protected Transform minDistanceCharacter;

        /// <summary>
        /// 当前攻击目标（攻击状态指定）。非空时武器优先跟踪它，而不是范围内最近目标
        /// ——否则攻击目标与武器方向不一致（目标 player 在 173.8°、武器却朝最近的韩东瑜 108.9°），
        /// 即用户看到的"拐到其他方向攻击"（见 bug-fixes.md 2026-08-16）。
        /// </summary>
        public Transform AimTarget { get; set; }

        /// <summary>
        /// 武器追踪敌人范围
        /// </summary>
        protected float raduis = 0.0f;

        /// <summary>
        /// 手持该武器的玩家
        /// </summary>
        protected Character character;

        /// <summary>
        /// 攻击效果
        /// </summary>
        protected AttackEffectManager.EffectTypeEnum attackEffect = AttackEffectManager.EffectTypeEnum.KnifeLight;

        private readonly Collider2D[] retCollider2Ds = new Collider2D[100]; // 存储圈内的所有碰撞体
        private float recordTime = float.MaxValue;
        private bool aimInitialized; // 武器朝向是否已矫正（首次 Update 后才允许攻击）

        /// <summary>
        /// 持有者标识（诊断用）：名字 + 坐标，区分同名角色实例（敌人均叫类型名，见 Character.Awake）
        /// </summary>
        private string OwnerLabel => this.character != null
            ? $"{this.character.name}@({this.character.transform.position.x:F0},{this.character.transform.position.y:F0})"
            : "?";
        private CircleCollider2D circleCollider2D;
        private ContactFilter2D contactFilter2D; // 结构体可以不new
        private LayerMask attackLayers; // 攻击的层级
        private List<string> attackTags; // 攻击的标签

        /// <summary>
        /// 是否暴击
        /// </summary>
        public bool IsCRT { get; set; } = false;

        /// <summary>
        /// 武器头部
        /// </summary>
        public Transform Head { get; set; }

        /// <summary>
        /// 设置玩家
        /// </summary>
        /// <param name="character">持有武器的角色</param>
        public void SetCharacter(Character character)
        {
            this.character = character;
            this.attackLayers = character.AttackLayers;
            this.attackTags = character.AttackTags;
        }

        /// <summary>
        /// 攻击
        /// </summary>
        [PunRPC]
        public virtual void Attack()
        {
            // 武器刚实例化时朝向尚未矫正（AWeaponObject.Update 还没跑过），跳过本次攻击：
            // 否则特效会按 prefab 默认方向打出，视觉上"突然拐到一边再拐回来"。
            // 武器方向完全由 Update() 动态跟踪（范围内最近目标/摇杆/鼠标），这里只做时序门控（见 bug-fixes.md 2026-08-16）。
            if (!this.aimInitialized)
            {
                return;
            }

            if (this.recordTime >= this.attackInterval)
            {
                // 视线检测：如果目标在墙后，跳过攻击避免剑光出生即碰墙消失。
                // 检测对象取攻击目标（AimTarget）优先，回退到范围内最近目标。
                Transform losTarget = this.AimTarget != null ? this.AimTarget : this.minDistanceCharacter;
                if (losTarget != null)
                {
                    Vector3 direction = losTarget.position - this.Head.position;
                    float distance = direction.magnitude;
                    RaycastHit2D hit = Physics2D.Raycast(this.Head.position, direction.normalized, distance,
                        LayerMask.GetMask("Tile", "BuildTile"));
                    if (hit.collider != null)
                    {
                        // 目标被墙壁遮挡，跳过本次攻击
                        return;
                    }
                }

                // 所有武器攻击效果
                ParticleSystem particleSystem = ServiceLocator.Get<AttackEffectManager>().GetEffect(this.attackEffect, (this.transform.rotation.eulerAngles.z + 90) * MathHelper.Deg2Rad);
                particleSystem.transform.parent = this.transform.parent.parent;
                particleSystem.transform.position = this.Head.position;
                particleSystem.Play();
                AttackEffect attackEffect = particleSystem.GetComponent<AttackEffect>();
                attackEffect.IsCRT = this.IsCRT;
                attackEffect.AttackLayers = this.attackLayers;
                attackEffect.AttackTags = this.attackTags;
                attackEffect.Onwer = this.character;
                attackEffect.Damage = this.character.CharacterDataLAB.GetDamage(this.IsCRT);
                this.recordTime = 0.0f;
                this.DoAttack(attackEffect);
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(this.character.gameObject.GetPhotonView().ViewID);
            }
            else
            {
                this.character = PhotonView.Find((int)stream.ReceiveNext()).GetComponent<Character>();
                this.transform.SetParent(this.character.transform);
            }
        }

        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            this.contactFilter2D.useTriggers = true;
            this.name = this.GetType().Name;
            this.Head = this.transform.Find("Head");
        }

        /// <inheritdoc/>
        protected override void Start()
        {
            base.Start();
            this.transform.localPosition = Vector3.zero; // 初始位置与玩家一致
            this.circleCollider2D = this.Head.GetComponent<CircleCollider2D>();
            if (this.circleCollider2D == null)
            {
                AWorkerTask.LogProvider("collider Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            // 设置武器追踪范围
            this.circleCollider2D.radius = this.raduis;

            // 武器实例化诊断（事件点）：初始朝向 + 追踪范围 + 持有者，确认拿起武器时 prefab 默认朝向
            AWorkerTask.LogProvider(
                $"[WeaponDiag] {this.OwnerLabel} {this.name} 实例化 初始z={this.transform.rotation.eulerAngles.z:0.0} raduis={this.raduis}",
                LogManager.LogLevelEnum.Debug);
        }

        /// <inheritdoc/>
        protected override void Update()
        {
            base.Update();

            // 首次执行到此处说明朝向矫正逻辑已就绪，允许攻击
            this.aimInitialized = true;

            // 控制武器攻击的事件间隔
            if (this.recordTime < this.attackInterval)
            {
                this.recordTime += Time.deltaTime;
            }

            float minDistance = 9999.0f, tempDistance;

            // 通过检测碰撞器内部的碰撞体{Overlap:重叠}
            int length = this.circleCollider2D.OverlapCollider(this.contactFilter2D, this.retCollider2Ds);
            for (int i = 0; i < length; i++)
            {
                if (this.attackTags.Contains(this.retCollider2Ds[i].tag))
                {
                    tempDistance = (this.retCollider2Ds[i].transform.position - this.transform.position).magnitude;
                    if (tempDistance < minDistance)
                    {
                        minDistance = tempDistance;
                        this.minDistanceCharacter = this.retCollider2Ds[i].transform;
                    }
                }
            }

            // 攻击目标（AimTarget，攻击状态指定）优先于范围内最近目标：
            // 否则攻击状态锁定的 Target/LastAttacker 与武器实际方向不一致，
            // 武器在最近几个目标间切换 = "拐到其他方向攻击"（见 bug-fixes.md 2026-08-16）。
            Transform trackTarget = this.AimTarget != null ? this.AimTarget : this.minDistanceCharacter;
            if (trackTarget != null)
            {
                // 如果范围内有目标, 跟踪目标
                // transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.FromToRotation(Vector3.up, minDistanceEnemy.position - transform.position), Time.deltaTime * 100);
                this.transform.rotation = Quaternion.FromToRotation(Vector3.up, trackTarget.position - this.transform.position);
                this.minDistanceCharacter = null;
            }
            else if (this.character is Player)
            {
                // 玩家：摇杆优先（移动端），否则跟随鼠标。
                // 注意摇杆是全局单例（Joystick.Instance），必须放在 is Player 守卫内——
                // 否则玩家推摇杆时 Worker/Enemy 武器也会被摇杆方向劫持，指向空方向攻击（见 bug-fixes.md 2026-08-16）。
                if (ServiceLocator.TryGet<Joystick>(out Joystick joystick) && joystick.Direction.magnitude > 1.0f)
                {
                    // 跟随摇杆
                    this.transform.rotation = Quaternion.FromToRotation(Vector3.up, joystick.Direction);
                }
                else
                {
                    // 玩家跟随鼠标
                    this.transform.rotation = Quaternion.FromToRotation(Vector3.up, UnityGlobalInputAdapter.GetMouseScreenPosition() - Camera.main.WorldToScreenPoint(ServiceLocator.Get<PlayerManager>().Mine.transform.position));
                }
            }
            else
            {
                // Worker/敌人：范围内无目标，保持原朝向
            }
        }

        /// <summary>
        /// 获取武器的方向
        /// </summary>
        /// <returns>方向</returns>
        protected Vector3 GetDirection()
        {
            return (this.Head.position - this.transform.position).normalized;
        }

        /// <summary>
        /// 间隔攻击
        /// </summary>
        /// <param name="attackEffect">攻击特效</param>
        protected abstract void DoAttack(AttackEffect attackEffect);
    }
}
