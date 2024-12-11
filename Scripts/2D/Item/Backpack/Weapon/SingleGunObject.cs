using Photon.Pun;
using System;
using UnityEngine;

namespace LAB2D
{
    [Serializable]
    public class SingleGun : Gun { 
    }

    public class SingleGunObject : GunObject
    {
        protected override void Awake()
        {
            base.Awake();
            attackInterval = 0.5f;
            name = "SingleGun";
        }

        protected override void _attack()
        {
            GameObject g = PhotonNetwork.Instantiate(ResourcesManager.Instance.getPath("PlayerBullet.prefab"), gunHead.transform.position, Quaternion.identity);
            if (g == null) {
                Debug.LogError("bullet Instantiate Error!!!");
                return;
            }
            g.GetComponent<Bullet>().Direction = gunHead.transform.position - transform.position;
            g.GetComponent<Bullet>().BulletSpeed = bulletSpeed;
            g.GetComponent<Bullet>().Damage = damage;
            g.transform.SetParent(transform.parent.parent,false);
            // go.GetComponent<Rigidbody2D>().velocity = go.transform.TransformDirection(gunForward.normalized * bulletSpeed); // (子弹)刚体的速度
            // Destroy(go, 1.0f);
        }
    }
}