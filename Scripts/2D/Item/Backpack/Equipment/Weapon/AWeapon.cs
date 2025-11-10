namespace LAB2D
{
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
        /// <summary>
        /// 本次共计是否暴击
        /// </summary>
        public bool IsCRT = false;

        public AWeapon()
        {
            this.ATN = this.RankRandom(0.0f, 0.0f);
            this.ATK = this.RankRandom(0.0f, 0.0f);
            this.INT = this.RankRandom(0.0f, 0.0f);
            this.CRT = this.RankRandom(0.0f, 0.0f);
            this.CSD = this.RankRandom(0.0f, 0.0f);
            this.HIT = this.RankRandom(0.0f, 0.0f);
            this.RES = this.RankRandom(0.0f, 0.0f);
            this.EquipType = EquipTypeEnum.Weapon;
        }

        /// <summary>
        /// 返回武器伤害值
        /// </summary>
        /// <returns>伤害值</returns>
        public float GetDamage()
        {
            this.IsCRT = UnityEngine.Random.Range(0.0f, 1.0f) < this.CRT;
            float damage = (Convert.ToInt32(this.IsCRT) * this.ATK * (1 + this.CSD)) + (Convert.ToInt32(!this.IsCRT) * this.ATK);
            return damage;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return base.ToString() +
                "攻击力: " + Math.Round(this.ATK, 2) + "\n" +
                "暴击率: " + Math.Round(this.CRT * 100, 2) + "%\n" +
                "暴击伤害: " + Math.Round(this.CSD * 100, 2) + "%\n";
        }

        /// <summary>
        /// 生成数越大生成的随机数几率越小
        /// </summary>
        /// <param name="down">下限</param>
        /// <param name="up">上限</param>
        /// <returns>随机数</returns>
        protected float RankRandom(float down, float up)
        {
            if (down > up)
            {
                float t = down;
                down = up;
                up = t;
            }

            float intervalValue = (up - down) / 20;
            float r; // 每次生成随机数进行判断
            for (float t = down + intervalValue; t < up; t += intervalValue)
            {
                r = UnityEngine.Random.Range(down, up);
                if (r < t)
                {
                    return r;
                }
            }

            return 0.0f;
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
        protected Transform minDistanceEnemy;

        /// <summary>
        /// 武器追踪敌人范围
        /// </summary>
        protected float raduis = 0.0f;

        /// <summary>
        /// 手持该武器的玩家
        /// </summary>
        protected Character character;

        /// <summary>
        /// 武器头部
        /// </summary>
        protected Transform head;

        /// <summary>
        /// 攻击效果
        /// </summary>
        protected AttackEffectManager.EffectTypeEnum attackEffect = AttackEffectManager.EffectTypeEnum.KnifeLight;

        private readonly Collider2D[] retCollider2Ds = new Collider2D[100]; // 存储圈内的所有碰撞体
        private float recordTime = float.MaxValue;
        private CircleCollider2D circleCollider2D;
        private ContactFilter2D contactFilter2D; // 结构体可以不new

        /// <summary>
        /// 攻击的层级
        /// </summary>
        public LayerMask AttackLayers { get; set; }

        /// <summary>
        /// 攻击的标签
        /// </summary>
        public List<string> AttackTags { get; set; }

        /// <summary>
        /// 设置玩家
        /// </summary>
        /// <param name="character">持有武器的角色</param>
        public void SetCharacter(Character character)
        {
            this.character = character;
            this.enabled = true; // 启动角色控制武器脚本
            CircleCollider2D c = PlayerManager.Instance.Select.Weapon.transform.Find("Head").gameObject.AddComponent<CircleCollider2D>(); // 敌人检测
            c.isTrigger = true;
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
                particleSystem.transform.position = this.head.position;
                particleSystem.Play();
                AttackEffect attackEffect = particleSystem.GetComponent<AttackEffect>();
                attackEffect.AttackLayers = this.AttackLayers;
                attackEffect.AttackTags = this.AttackTags;
                attackEffect.Onwer = this.character;
                attackEffect.Damage = ((AWeapon)this.Item).GetDamage();
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
            this.head = this.transform.Find("Head");
        }

        /// <inheritdoc/>
        protected override void Start()
        {
            base.Start();
            this.transform.localPosition = Vector3.zero; // 初始位置与玩家一致
            this.circleCollider2D = this.head.GetComponent<CircleCollider2D>();
            if (this.circleCollider2D == null)
            {
                LogManager.Instance.Log("collider Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            // 设置武器追踪范围
            CircleCollider2D collider2D = this.head.GetComponent<CircleCollider2D>();
            if (collider2D != null)
            {
                collider2D.radius = this.raduis;
            }
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
                if (this.retCollider2Ds[i].CompareTag("Enemy"))
                {
                    tempDistance = (this.retCollider2Ds[i].transform.position - this.transform.position).magnitude;
                    if (tempDistance < minDistance)
                    {
                        minDistance = tempDistance;
                        this.minDistanceEnemy = this.retCollider2Ds[i].transform;
                    }
                }
            }

            if (this.minDistanceEnemy != null)
            {
                // transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.FromToRotation(Vector3.up, minDistanceEnemy.position - transform.position), Time.deltaTime * 100);
                this.transform.rotation = Quaternion.FromToRotation(Vector3.up, this.minDistanceEnemy.position - this.transform.position);
                this.minDistanceEnemy = null;
            }
            else if (Joystick.Instance && Joystick.Instance.Direction.magnitude > 1.0f)
            {
                this.transform.rotation = Quaternion.FromToRotation(Vector3.up, Joystick.Instance.Direction);
            }
            else
            {
                this.transform.rotation = Quaternion.FromToRotation(Vector3.up, Input.mousePosition - Camera.main.WorldToScreenPoint(PlayerManager.Instance.Mine.transform.position));
            }
        }

        /// <summary>
        /// 间隔攻击
        /// </summary>
        /// <param name="attackEffect">攻击特效</param>
        protected abstract void DoAttack(AttackEffect attackEffect);
    }
}
