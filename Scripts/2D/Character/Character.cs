using UnityEngine;
using Photon.Pun;
using System;

namespace LAB2D {
    [Serializable]
    public abstract class Character : MonoBehaviourPun
    {
        protected PlayerData playerData;
        protected float Hp { get; set; } = 1; // 血量
        protected float MaxHp { get; set; } = 1; // 最大血量

        private GameObject damageUI; // 掉血面板
        protected SpriteRenderer _renderer; // 闪烁 
        private Color originalColor; // 原来的自身颜色

        protected virtual void Awake()
        {
            damageUI = ResourcesManager.Instance.getPrefab("Damage");
            transform.SetParent(GameObject.FindGameObjectWithTag("CharacterRoot").transform);
        }

        protected virtual void Start() { 
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null) {
                Debug.LogError("renderer Not Found!!!");
                return;
            }
            originalColor = _renderer.color;
        }

        public virtual void reduceHp(float Hp)
        {
            if (Hp <= 0) return;
            GameObject g = Instantiate(damageUI, transform.position, Quaternion.identity); // 创建物体(预设,位置,角度)
            if (g == null) {
                Debug.LogError("damageUI Instantiate Error!!!");
                return;
            }
            g.name = damageUI.name;
            if(this is Player)
            {
                g.GetComponent<DamageUI>().setDamage(Hp);
            }
            else
            {
                // 暴击时显示不同的框
                g.GetComponent<DamageUI>().setDamage(Hp, Convert.ToInt32(PlayerManager.Instance.Select.weaponData.isCRT));
                //g.GetComponent<DamageUI>().setDamage(Hp, 0);
            }
            g.transform.SetParent(transform);

            // 变红
            _renderer.color = Color.red;
            Invoke(nameof(ResetColor), 0.2f); // 一段时间后调用

            //if (!photonView.IsMine) return;
            this.Hp -= Hp;  // 更新敌人生命值
            if (this.Hp <= 0)
            {
                this.Hp = 0;
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
    }
}

