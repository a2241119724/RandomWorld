using UnityEngine;
using Photon.Pun;
using System;

namespace LAB2D {
    public abstract class Character : MonoBehaviourPun
    {
        public CharacterData CharacterDataLAB = new CharacterData();
        public float moveSpeed = 2.5f; // 角色速度

        protected SpriteRenderer _renderer; // 闪烁 
        protected CheckBug checkBug;

        private GameObject damageUI; // 掉血面板
        private Color originalColor; // 原来的自身颜色

        protected virtual void Awake()
        {
            damageUI = ResourcesManager.Instance.getPrefab("Damage");
            transform.SetParent(GameObject.FindGameObjectWithTag("CharacterRoot").transform);
            checkBug = new CheckBug();
        }

        protected virtual void Start() { 
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null) {
                LogManager.Instance.log("renderer Not Found!!!", LogManager.LogLevel.Error);
                return;
            }
            originalColor = _renderer.color;
        }

        public virtual void reduceHp(float Hp)
        {
            if (Hp <= 0) return;
            GameObject g = Instantiate(damageUI, transform.position, Quaternion.identity); // 创建物体(预设,位置,角度)
            if (g == null) {
                LogManager.Instance.log("damageUI Instantiate Error!!!", LogManager.LogLevel.Error);
                return;
            }
            g.name = damageUI.name;
            if(this is Enemy)
            {
                // 暴击时显示不同的框
                g.GetComponent<DamageUI>().setDamage(Hp, Convert.ToInt32(PlayerManager.Instance.Select.weaponData.isCRT));
            }
            else
            {
                g.GetComponent<DamageUI>().setDamage(Hp);
            }
            g.transform.SetParent(transform);

            // 变红
            _renderer.color = Color.red;
            Invoke(nameof(ResetColor), 0.2f); // 一段时间后调用

            //if (!photonView.IsMine) return;
            CharacterDataLAB.Hp -= Hp;  // 更新敌人生命值
            if (CharacterDataLAB.Hp <= 0)
            {
                CharacterDataLAB.Hp = 0;
                death();
                return;
            }
        }

        /// <summary>
        /// 恢复颜色
        /// </summary>
        private void ResetColor() {
            _renderer.color = originalColor;
        }

        protected abstract void death();

        public override string ToString()
        {
            return $"{this.GetType().Name}:{name}\n" +
                $"Speed:{moveSpeed}\n";
        }

        protected class CheckBug
        {
            public long lastTime;
            public int colliderCount;

            public bool isBug(string name, int threshold=50)
            {
                bool bug = colliderCount > threshold;
                if (bug)
                {
                    LogManager.Instance.log(name + "碰撞次数:" + colliderCount, LogManager.LogLevel.Info);
                }
                return bug;
            }

            public void addColliderCount(long time)
            {
                if (time - lastTime < 3e5)
                {
                    colliderCount++;
                }
                else
                {
                    colliderCount = 1;
                }
                lastTime = time;
            }
        }

        [Serializable]
        public class CharacterData {
            public float Hp = 1; // 血量
            public float MaxHp = 1; // 最大血量
            public float ATN = 0.0f; // 物理攻击力
            public float INT = 0.0f; // 魔法攻击力
            public float CRI = 0.0f; // 暴击率
            public float ATK = 0.0f; // 攻击力
            public float DEF = 0.0f; // 防御力
            public float SPD = 0.0f; // 速度，回避物理攻击之类的
            public float HIT = 0.0f; // 命中率或者连击之类的
            public float RES = 0.0f; // 魔法防御力
            public Vector3LAB pos;
        }
    }

    /// <summary>
    /// 为了可以序列化
    /// </summary>
    [Serializable]
    public struct Vector3LAB
    {
        public float x;
        public float y;
        public float z;

        public Vector3LAB(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3 toVector3(Vector3LAB vector3LAB)
        {
            return new Vector3(vector3LAB.x, vector3LAB.y, vector3LAB.z);
        }

        public static Vector3LAB toVector3LAB(Vector3 vector3)
        {
            return new Vector3LAB(vector3.x, vector3.y, vector3.z);
        }
    }
}
