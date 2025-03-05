using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;

namespace LAB2D
{
    [Serializable]
    public class CustomSword : Sword { 
    }

    public class CustomSwordObject : SwordObject
    {
        protected override void Awake()
        {
            base.Awake();
            name = "CustomSword";
            attackInterval = 0.2f;
        }

        protected override void _attack()
        {
            // ʹ�ô������ص�ʽ�ٴδ���OnTriggerEnter2D
            GetComponent<Collider2D>().enabled = false;
            GetComponent<Collider2D>().enabled = true;
            StartCoroutine(Rotate());
        }

        private IEnumerator Rotate(){
            Quaternion q = transform.rotation;
            for (int i = 0; i < 20; i++)
            {
                transform.rotation = Quaternion.Lerp(q * Quaternion.Euler(0, 0, 60), q * Quaternion.Euler(0, 0, -60), 0.05f * i); // (��ʼ������ֹ������ת�ٶ�)������
                yield return null;
            }
            // �ص�ԭ����
            transform.rotation = q;
        }

        //protected override void OnTriggerEnter2D(Collider2D collision)
        //{
        //    base.OnTriggerEnter2D(collision);
        //    if (!photonView.IsMine && PhotonNetwork.IsConnected) return;
        //    if (collision.transform.CompareTag("Enemy"))
        //    {
        //        // ��ֹ��������ʱ,ͬʱ����
        //        if (PlayerManager.Instance.Select.weaponData == null) return;
        //        collision.GetComponent<Enemy>().setPlayer(player.GetComponent<Player>());
        //        //collision.GetComponent<PhotonView>().RPC("reduceHp", RpcTarget.All, ((WeaponData)PlayerManager.Instance.Select.weaponData).getDamage());
        //        collision.transform.GetComponent<Enemy>().reduceHp((PlayerManager.Instance.Select.weaponData).getDamage());
        //        GameObject go = Instantiate(blood, collision.transform.position, Quaternion.identity);
        //        if (go == null)
        //        {
        //LogManager.Instance.log("blood Instantiate Error!!!", LogManager.LogLevel.Error);
        //            return;
        //        }
        //        go.name = blood.name;
        //        go.transform.SetParent(collision.transform);
        //    }
        //}
    }
}