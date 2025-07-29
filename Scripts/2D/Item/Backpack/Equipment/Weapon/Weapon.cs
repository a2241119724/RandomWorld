namespace LAB2D
{
    using System;
    using Photon.Pun;
    using UnityEngine;

    /// <summary>
    /// 剑
    /// </summary>
    [Serializable]
    public abstract class Weapon : Equipment
    {
        /// <summary>
        /// 物理攻击力
        /// </summary>
        public float ATN;

        /// <summary>
        /// 魔法攻击力
        /// </summary>
        public float INT;

        /// <summary>
        /// 暴击率
        /// </summary>
        public float CRT;

        /// <summary>
        /// 暴击伤害
        /// </summary>
        public float CSD;

        /// <summary>
        /// 攻击力
        /// </summary>
        public float ATK;

        // public float DEF; // 防御力
        // public float SPD; // 速度，回避物理攻击之类的

        /// <summary>
        /// 命中率或者连击之类的
        /// </summary>
        public float HIT;

        /// <summary>
        /// 魔法防御力
        /// </summary>
        public float RES;

        /// <summary>
        /// 本次共计是否暴击
        /// </summary>
        public bool IsCRT = false;

        public Weapon()
        {
            this.ATN = this.RankRandom(0.0f, 0.0f);
            this.ATK = this.RankRandom(0.0f, 0.0f);
            this.INT = this.RankRandom(0.0f, 0.0f);
            this.CRT = this.RankRandom(0.0f, 0.0f);
            this.CSD = this.RankRandom(0.0f, 0.0f);
            this.HIT = this.RankRandom(0.0f, 0.0f);
            this.RES = this.RankRandom(0.0f, 0.0f);
            this.EquipTypeValue = EquipType.Weapon;
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
    public abstract class WeaponObject : BackpackItemObject, IPunObservable
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
        protected GameObject player;

        private readonly Collider2D[] retCollider2Ds = new Collider2D[100]; // 存储圈内的所有碰撞体
        private float recordTime = float.MaxValue;
        private new CircleCollider2D collider;
        private ContactFilter2D contactFilter2D; // 结构体可以不new

        /// <summary>
        /// 设置玩家
        /// </summary>
        /// <param name="player">玩家</param>
        public void SetPlayer(Player player)
        {
            if (player == null)
            {
                LogManager.Instance.Log("collider is null!!!", LogManager.LogLevel.Error);
                return;
            }

            this.player = player.gameObject;
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
                this.Attack1();
                this.recordTime = 0.0f;
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(this.player.GetPhotonView().ViewID);
            }
            else
            {
                this.player = PhotonView.Find((int)stream.ReceiveNext()).gameObject;
                this.transform.SetParent(this.player.transform);
            }
        }

        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            this.contactFilter2D.useTriggers = true;
            this.name = this.GetType().Name;
        }

        /// <inheritdoc/>
        protected override void Start()
        {
            base.Start();
            this.transform.localPosition = Vector3.zero; // 初始位置与玩家一致
            this.collider = this.transform.Find("Head").GetComponent<CircleCollider2D>();
            if (this.collider == null)
            {
                LogManager.Instance.Log("collider Not Found!!!", LogManager.LogLevel.Error);
                return;
            }

            // 设置武器追踪范围
            CircleCollider2D collider2D = this.transform.Find("Head").GetComponent<CircleCollider2D>();
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
            int length = this.collider.OverlapCollider(this.contactFilter2D, this.retCollider2Ds);
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

            // 鼠标左键点击攻击
            if (Input.GetMouseButtonDown(0))
            {
                this.Attack();
            }
        }

        /// <summary>
        /// 间隔攻击
        /// </summary>
        protected abstract void Attack1();

        // protected override void OnTriggerEnter2D(Collider2D collision)
        // {
        //     if (transform.parent && transform.parent.CompareTag("Player")) return;
        //     base.OnTriggerEnter2D(collision);
        // }
    }
}
