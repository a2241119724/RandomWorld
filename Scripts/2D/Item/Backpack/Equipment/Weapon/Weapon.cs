using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LAB2D {
    [Serializable]
    public abstract class Weapon : Equipment {
        public float ATN; // 物理攻击力
        public float INT; // 魔法攻击力
        public float CRT; // 暴击率
        public float CSD; // 暴击伤害
        public float ATK; // 攻击力
        //public float DEF; // 防御力
        //public float SPD; // 速度，回避物理攻击之类的
        public float HIT; // 命中率或者连击之类的
        public float RES; // 魔法防御力
        public bool isCRT = false; // 本次共计是否暴击

        public Weapon() {
            ATN = rankRandom(0.0f, 0.0f);
            ATK = rankRandom(0.0f, 0.0f);
            INT = rankRandom(0.0f, 0.0f);
            CRT = rankRandom(0.0f, 0.0f);
            CSD = rankRandom(0.0f, 0.0f);
            HIT = rankRandom(0.0f, 0.0f);
            RES = rankRandom(0.0f, 0.0f);
        }

        /// <summary>
        /// 返回武器伤害值
        /// </summary>
        /// <returns></returns>
        public float getDamage()
        {
            isCRT = UnityEngine.Random.Range(0.0f, 1.0f) < CRT;
            float damage = Convert.ToInt32(isCRT) * ATK * (1 + CSD) + Convert.ToInt32(!isCRT) * ATK;
            return damage;
        }

        /// <summary>
        /// 按照越大几率越小生成随机数
        /// </summary>
        /// <returns></returns>
        protected float rankRandom(float down, float up)
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

        public override string ToString()
        {
            return base.ToString() +
                "攻击力: " + Math.Round(ATK, 2) + "\n" +
                "暴击率: " + Math.Round(CRT * 100, 2) + "%\n" +
                "暴击伤害: " + Math.Round(CSD * 100, 2) + "%\n";
        }
    }

    public abstract class WeaponObject : BackpackItemObject, IPunObservable
    {
        protected float attackInterval = 1.0f; // 攻击时间
        protected Transform minDistanceEnemy; // 最近的敌人
        protected float raduis = 0.0f; // 武器追踪敌人范围
        protected GameObject player; // 手持该武器的玩家

        private float recordTime = float.MaxValue;
        private CircleCollider2D _collider;
        private ContactFilter2D contactFilter2D; // 结构体可以不new
        /// <summary>
        /// 存储圈内的所有碰撞体
        /// </summary>
        private readonly Collider2D[] retCollider2Ds = new Collider2D[100];

        protected override void Awake()
        {
            base.Awake();
            contactFilter2D.useTriggers = true;
            name = this.GetType().Name;
        }

        protected override void Start()
        {
            base.Start();
            transform.localPosition = Vector3.zero; // 初始位置与玩家一致
            _collider = transform.Find("Head").GetComponent<CircleCollider2D>();
            if (_collider == null) {
                Debug.LogError("collider Not Found!!!");
                return;
            }
            // 设置武器追踪范围
            CircleCollider2D collider2D = transform.Find("Head").GetComponent<CircleCollider2D>();
            if (collider2D != null)
            {
                collider2D.radius = raduis;
            }
        }

        protected override void Update()
        {
            base.Update();
            // 控制武器攻击的事件间隔
            if (recordTime < attackInterval)
            {
                recordTime += Time.deltaTime;
            }
            float minDistance = 9999.0f, tempDistance;
            // 通过检测碰撞器内部的碰撞体{Overlap:重叠}
            int length = _collider.OverlapCollider(contactFilter2D, retCollider2Ds);
            for (int i = 0; i < length; i++)
            {
                if (retCollider2Ds[i].CompareTag("Enemy"))
                {
                    tempDistance = (retCollider2Ds[i].transform.position - transform.position).magnitude;
                    if (tempDistance < minDistance) {
                        minDistance = tempDistance;
                        minDistanceEnemy = retCollider2Ds[i].transform;
                    }
                }
            }
            if (minDistanceEnemy != null)
            {
                //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.FromToRotation(Vector3.up, minDistanceEnemy.position - transform.position), Time.deltaTime * 100);
                transform.rotation = Quaternion.FromToRotation(Vector3.up, minDistanceEnemy.position - transform.position);
                minDistanceEnemy = null;
            }else if(Joystick.Instance && Joystick.Instance.Direction.magnitude > 1.0f) {
                transform.rotation = Quaternion.FromToRotation(Vector3.up, Joystick.Instance.Direction);
            }else{
                transform.rotation = Quaternion.FromToRotation(Vector3.up, Input.mousePosition-Camera.main.WorldToScreenPoint(PlayerManager.Instance.Mine.transform.position));
            }
            // 鼠标左键点击攻击
            if (Input.GetMouseButtonDown(0))
            {
                attack();
            }
        }

        /// <summary>
        /// 设置玩家
        /// </summary>
        /// <param name="player">玩家</param>
        public void SetPlayer(Player player)
        {
            if (player == null)
            {
                Debug.LogError("collider is null!!!");
                return;
            }
            this.player = player.gameObject;
            enabled = true; // 启动角色控制武器脚本
            CircleCollider2D c = PlayerManager.Instance.Select.weapon.transform.Find("Head").gameObject.AddComponent<CircleCollider2D>(); // 敌人检测
            c.isTrigger = true;
        }

        /// <summary>
        /// 攻击
        /// </summary>
        [PunRPC]
        public virtual void attack()
        {
            //if (!photonView.IsMine && PhotonNetwork.IsConnected) return;
            if (recordTime >= attackInterval)
            {
                _attack();
                recordTime = 0.0f;
            }
        }
        /// <summary>
        /// 间隔攻击
        /// </summary>
        protected abstract void _attack();

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting) {
                stream.SendNext(player.GetPhotonView().ViewID);
            } else {
                player = PhotonView.Find((int)stream.ReceiveNext()).gameObject;
                transform.SetParent(player.transform);
            }
        }

        //protected override void OnTriggerEnter2D(Collider2D collision)
        //{
        //    if (transform.parent && transform.parent.CompareTag("Player")) return;
        //    base.OnTriggerEnter2D(collision);
        //}
    }
}
