namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 枪
    /// </summary>
    [Serializable]
    public abstract class Gun : Weapon
    {
    }

    /// <summary>
    /// 枪对象
    /// </summary>
    public abstract class GunObject : WeaponObject
    {
        /// <summary>
        /// 枪的头部
        /// </summary>
        protected GameObject gunHead;

        /// <summary>
        /// 子弹速度
        /// </summary>
        protected float bulletSpeed = 30.0f;

        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            this.raduis = 5.0f;
        }

        /// <inheritdoc/>
        protected override void Start()
        {
            base.Start();
            this.gunHead = this.transform.Find("Head").gameObject;
            if (this.gunHead == null)
            {
                LogManager.Instance.Log("gunHead Not Found!!!", LogManager.LogLevel.Error);
                return;
            }
        }

        /// <inheritdoc/>
        protected override void Update()
        {
            base.Update();

            // // 滑屏控制枪的方向
            // if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
            // {
            //  	if (Input.GetTouch(0).position.x > Screen.width/2 && Input.GetTouch(0).deltaPosition.sqrMagnitude > 1.0f) // 滑动距离超过多少
            //  	{
            //  		transform.rotation *= Quaternion.Euler(0,0, -Input.GetTouch(0).deltaPosition.x * rotationSpeed);
            //  	}
            // }
        }

        /// <summary>
        /// 发射子弹
        /// </summary>
        /// <param name="bulletName">子弹名字</param>
        /// <returns>子弹</returns>
        protected GameObject FireBullet(string bulletName)
        {
            GameObject g = Tool.Instantiate(ResourcesManager.Instance.GetPrefab(bulletName), this.gunHead.transform.position, Quaternion.identity);
            if (g == null)
            {
                LogManager.Instance.Log("bullet Instantiate Error!!!", LogManager.LogLevel.Error);
                return null;
            }

            g.GetComponent<Bullet>().Direction = this.gunHead.transform.position - this.transform.position;
            g.GetComponent<Bullet>().BulletSpeed = this.bulletSpeed;
            g.GetComponent<Bullet>().Damage = ((Weapon)this.Item).GetDamage();
            g.GetComponent<Bullet>().Origin = this.player.GetComponent<Character>();
            g.transform.SetParent(this.transform.parent.parent, false);

            // go.GetComponent<Rigidbody2D>().velocity = go.transform.TransformDirection(gunForward.normalized * bulletSpeed); // (子弹)刚体的速度
            // Destroy(go, 1.0f);
            return g;
        }
    }
}
