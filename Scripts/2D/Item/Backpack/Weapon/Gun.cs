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
		protected float bulletSpeed = 20.0f; // 子弹速度

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
				Debug.LogError("gunHead Not Found!!!");
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
	}
}
