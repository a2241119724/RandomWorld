namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 枪
    /// </summary>
    [Serializable]
    public abstract class AGun : AWeapon
    {
    }

    /// <summary>
    /// 枪对象
    /// </summary>
    public abstract class AGunObject : AWeaponObject
    {
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
    }
}
