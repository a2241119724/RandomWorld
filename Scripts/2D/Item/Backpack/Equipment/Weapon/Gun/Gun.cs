using System;
using UnityEngine;

namespace LAB2D
{
	[Serializable]
	public abstract class Gun : Weapon
    {

    }


	public abstract class GunObject : WeaponObject
	{
		protected GameObject gunHead; // 枪的头部
		protected float bulletSpeed = 30.0f; // 子弹速度

		protected override void Awake() {
			base.Awake();
			raduis = 5.0f;
		}
		protected override void Start() // 防止不执行父类的函数,被子类覆盖
		{
			base.Start();
			gunHead = transform.Find("Head").gameObject;
			if (gunHead == null)
			{
				LogManager.Instance.log("gunHead Not Found!!!", LogManager.LogLevel.Error);
                return;
			}
		}

		protected override void Update()
        {
			base.Update();
			//// 滑屏控制枪的方向
			//if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
			//{
			//	if (Input.GetTouch(0).position.x > Screen.width/2 && Input.GetTouch(0).deltaPosition.sqrMagnitude > 1.0f) // 滑动距离超过多少
			//	{
			//		transform.rotation *= Quaternion.Euler(0,0, -Input.GetTouch(0).deltaPosition.x * rotationSpeed);
			//	}
			//}
		}

		/// <summary>
		/// 发射子弹
		/// </summary>
		/// <param name="bulletName"></param>
		/// <returns></returns>
		protected GameObject fireBullet(string bulletName)
		{
			GameObject g = Tool.Instantiate(ResourcesManager.Instance.getPrefab(bulletName), gunHead.transform.position, Quaternion.identity);
			if (g == null)
			{
				LogManager.Instance.log("bullet Instantiate Error!!!", LogManager.LogLevel.Error);
                return null;
			}
			g.GetComponent<Bullet>().Direction = gunHead.transform.position - transform.position;
			g.GetComponent<Bullet>().BulletSpeed = bulletSpeed;
			g.GetComponent<Bullet>().Damage = ((Weapon)Item).getDamage();
			g.GetComponent<Bullet>().Origin = player.GetComponent<Character>();
			g.transform.SetParent(transform.parent.parent, false);
			// go.GetComponent<Rigidbody2D>().velocity = go.transform.TransformDirection(gunForward.normalized * bulletSpeed); // (子弹)刚体的速度
			// Destroy(go, 1.0f);
			return g;
		}
	}
}
