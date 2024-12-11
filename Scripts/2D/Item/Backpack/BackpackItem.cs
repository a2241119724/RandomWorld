using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    [Serializable]
    public abstract class BackpackItem : Item
    {
        public BackpackItemQuality quality; // Ʒ��


        [Serializable]
        public enum BackpackItemQuality
        {
            Gray,   //��ɫ
            White,  //��ɫ
            Green,  //��ɫ
            Blue,   //��ɫ
            Purple, //��ɫ
            Orange, //��ɫ
            Yellow, //��ɫ
            Red,    //��ɫ
            Black   //��ɫ
        }
    }

    public abstract class BackpackItemObject : ItemObject
    {
        protected override void Awake()
        {
            base.Awake();
            // ���ڷŵ�MapTile
            //transform.SetParent(GameObject.FindGameObjectWithTag("MapRoot").transform);
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
        /// ��ʹ���������ϵĽű�û�п���,�÷���Ҳ��ִ��
        /// �õ����������,�ӵ���������
        /// </summary>
        /// <param name="collision"></param>
        //protected virtual void OnTriggerEnter2D(Collider2D collision)
        //{
        //    if (collision.gameObject.CompareTag("Player"))
        //    {
        //        BackpackController.Instance.addItem(ItemDataFactory.Instance.get(name));
        //        Destroy(gameObject);
        //        //gameObject.SetActive(false); // ��С����
        //    }
        //}
    }
}
