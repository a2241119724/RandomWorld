namespace LAB2D.Item.Backpack.Equipment.Weapon
{
    using LAB2D;
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
            // if (!photonView.IsMine && PhotonNetwork.IsConnected) return;
            if (this.recordTime >= this.attackInterval)
            {
                // 所有武器攻击效果
                ParticleSystem particleSystem = AttackEffectManager.Instance.GetEffect(this.attackEffect, (this.transform.rotation.eulerAngles.z + 90) * Mathf.Deg2Rad);
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
                LogManager.Instance.Log("collider Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            // 设置武器追踪范围
            this.circleCollider2D.radius = this.raduis;
        }

        /// <inheritdoc/>
        protected override void Update()
        {
            base.Update();

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

            if (this.minDistanceCharacter != null)
            {
                // 如果范围内有敌人, 跟踪敌人
                // transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.FromToRotation(Vector3.up, minDistanceEnemy.position - transform.position), Time.deltaTime * 100);
                this.transform.rotation = Quaternion.FromToRotation(Vector3.up, this.minDistanceCharacter.position - this.transform.position);
                this.minDistanceCharacter = null;
            }
            else if (Joystick.Instance && Joystick.Instance.Direction.magnitude > 1.0f)
            {
                // 跟随摇杆
                this.transform.rotation = Quaternion.FromToRotation(Vector3.up, Joystick.Instance.Direction);
            }
            else if (this.character is Player)
            {
                // 玩家跟随鼠标
                this.transform.rotation = Quaternion.FromToRotation(Vector3.up, Input.mousePosition - Camera.main.WorldToScreenPoint(PlayerManager.Instance.Mine.transform.position));
            }
            else
            {
                // Worker暂时仅用上面的跟踪
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
