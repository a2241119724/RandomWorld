using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    [Serializable]
    public abstract class BackpackItem : Item
    {
        public BackpackItemQuality quality; // 品质

        protected BackpackItem()
        {
            quality = BackpackItemQuality.Gray;
        }

        [Serializable]
        public enum BackpackItemQuality
        {
            Gray,   //灰色
            White,  //白色
            Green,  //绿色
            Blue,   //蓝色
            Purple, //紫色
            Orange, //橙色
            Yellow, //黄色
            Red,    //红色
            Black   //黑色
        }

        public override string ToString()
        {
            return base.ToString() +
                $"品质: {quality.ToString()}\n";
        }
    }

    public abstract class BackpackItemObject : ItemObject
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();
        }

        /// <summary>
        /// 即使挂在物体上的脚本没有开启,该方法也会执行
        /// 该道具碰到玩家,加到背包里面
        /// </summary>
        /// <param name="collision"></param>
        protected virtual void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                BackpackController.Instance.addItem(ItemFactory.Instance.getBackpackItemByName(name.Split("Object")[0]));
                Destroy(gameObject);
                //gameObject.SetActive(false); // 减小开销
            }
        }
    }
}
