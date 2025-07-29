namespace LAB2D
{
    using System;
    using Photon.Pun;
    using UnityEngine;

    /// <summary>
    /// 角色基类
    /// </summary>
    public abstract class Character : MonoBehaviourPun
    {
        /// <summary>
        /// 角色数据
        /// </summary>
        public CharacterData CharacterDataLAB = new ();

        /// <summary>
        /// 移动速度
        /// </summary>
        public float MoveSpeed = 2.5f;

        /// <summary>
        /// 闪烁
        /// </summary>
        protected new SpriteRenderer renderer;

        /// <summary>
        /// 检测bug
        /// </summary>
        protected CheckBug checkBug;

        private GameObject damageUI; // 掉血面板
        private Color originalColor; // 原来的自身颜色

        /// <summary>
        /// 角色扣血
        /// </summary>
        /// <param name="hp">血量</param>
        public virtual void ReduceHp(float hp)
        {
            if (hp <= 0)
            {
                return;
            }

            GameObject g = Instantiate(this.damageUI, this.transform.position, Quaternion.identity); // 创建物体(预设,位置,角度)
            if (g == null)
            {
                LogManager.Instance.Log("damageUI Instantiate Error!!!", LogManager.LogLevel.Error);
                return;
            }

            g.name = this.damageUI.name;
            if (this is Enemy)
            {
                // 暴击时显示不同的框
                g.GetComponent<DamageUI>().SetDamage(hp, Convert.ToInt32(PlayerManager.Instance.Select.WeaponData.IsCRT));
            }
            else
            {
                g.GetComponent<DamageUI>().SetDamage(hp);
            }

            g.transform.SetParent(this.transform);

            // 变红
            this.renderer.color = Color.red;
            this.Invoke(nameof(this.ResetColor), 0.2f); // 一段时间后调用

            // if (!photonView.IsMine) return;
            this.CharacterDataLAB.Hp -= hp;  // 更新敌人生命值
            if (this.CharacterDataLAB.Hp <= 0)
            {
                this.CharacterDataLAB.Hp = 0;
                this.Death();
                return;
            }
        }

        /// <summary>
        /// 角色信息
        /// </summary>
        /// <returns>信息</returns>
        public override string ToString()
        {
            return $"{this.GetType().Name}:{this.name}\n" +
                $"Speed:{this.MoveSpeed}\n";
        }

        protected virtual void Awake()
        {
            this.damageUI = ResourceManager.Instance.GetPrefab("Damage");
            this.transform.SetParent(GameObject.FindGameObjectWithTag("CharacterRoot").transform);
            this.checkBug = new CheckBug();
        }

        protected virtual void Start()
        {
            this.renderer = this.GetComponent<SpriteRenderer>();
            if (this.renderer == null)
            {
                LogManager.Instance.Log("renderer Not Found!!!", LogManager.LogLevel.Error);
                return;
            }

            this.originalColor = this.renderer.color;
        }

        /// <summary>
        /// 敌人死亡
        /// </summary>
        protected abstract void Death();

        /// <summary>
        /// 恢复颜色
        /// </summary>
        private void ResetColor()
        {
            this.renderer.color = this.originalColor;
        }

        /// <summary>
        /// 角色数据
        /// </summary>
        [Serializable]
        public class CharacterData
        {
            /// <summary>
            /// 血量
            /// </summary>
            public float Hp = 1;

            /// <summary>
            /// 最大血量
            /// </summary>
            public float MaxHp = 1;

            /// <summary>
            /// 物理攻击力
            /// </summary>
            public float ATN = 0.0f;

            /// <summary>
            /// 魔法攻击力
            /// </summary>
            public float INT = 0.0f;

            /// <summary>
            /// 暴击率
            /// </summary>
            public float CRI = 0.0f;

            /// <summary>
            /// 攻击力
            /// </summary>
            public float ATK = 0.0f;

            /// <summary>
            /// 防御力
            /// </summary>
            public float DEF = 0.0f;

            /// <summary>
            /// 速度，回避物理攻击之类的
            /// </summary>
            public float SPD = 0.0f;

            /// <summary>
            /// 命中率或者连击之类的
            /// </summary>
            public float HIT = 0.0f;

            /// <summary>
            /// 魔法防御力
            /// </summary>
            public float RES = 0.0f;

            /// <summary>
            /// 位置
            /// </summary>
            public Vector3LAB Pos;
        }

        /// <summary>
        /// 检查bug
        /// </summary>
        protected class CheckBug
        {
            /// <summary>
            /// 上一次碰撞时间
            /// </summary>
            public long LastTime;

            /// <summary>
            /// 连续碰撞次数
            /// </summary>
            public int ColliderCount;

            private const double Interval = 3e5;
            private const int Threshold = 100;

            /// <summary>
            /// 是否有bug
            /// </summary>
            /// <param name="name">出bug的角色名称</param>
            /// <param name="threshold">检测bug的碰撞阈值</param>
            /// <returns>是否有bug.</returns>
            public bool IsBug(string name, int threshold = Threshold)
            {
                bool bug = this.ColliderCount > threshold;
                if (bug)
                {
                    // LogManager.Instance.log(name + "碰撞次数:" + colliderCount, LogManager.LogLevel.Info);
                }

                return bug;
            }

            /// <summary>
            /// 添加碰撞次数
            /// </summary>
            /// <param name="time">当前时间</param>
            public void AddColliderCount(long time)
            {
                if (time - this.LastTime < Interval)
                {
                    this.ColliderCount++;
                }
                else
                {
                    this.ColliderCount = 1;
                }

                this.LastTime = time;
            }
        }
    }

    /// <summary>
    /// 为了可以序列化
    /// </summary>
    [Serializable]
    public class Vector3LAB
    {
        /// <summary>
        /// X 坐标
        /// </summary>
        public float X;

        /// <summary>
        /// Y 坐标
        /// </summary>
        public float Y;

        /// <summary>
        /// Z 坐标
        /// </summary>
        public float Z;

        public Vector3LAB(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        /// <summary>
        /// 将Vector3LAB转换为Vector3
        /// </summary>
        /// <param name="vector3LAB">Vector3LAB</param>
        /// <returns>Vector3</returns>
        public static Vector3 ToVector3(Vector3LAB vector3LAB)
        {
            return new Vector3(vector3LAB.X, vector3LAB.Y, vector3LAB.Z);
        }

        /// <summary>
        /// 将Vector3转换为Vector3LAB
        /// </summary>
        /// <param name="vector3">Vector3</param>
        /// <returns>Vector3LAB</returns>
        public static Vector3LAB ToVector3LAB(Vector3 vector3)
        {
            return new Vector3LAB(vector3.x, vector3.y, vector3.z);
        }
    }
}
