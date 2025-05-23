using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    [Serializable]
    public class TraceGun : Gun
    {
        public TraceGun()
        {
            ATN = rankRandom(5.0f, 10.0f);
            ATK = rankRandom(5.0f, 10.0f);
            INT = rankRandom(5.0f, 10.0f);
            CRT = rankRandom(5.0f, 10.0f);
            CSD = rankRandom(5.0f, 10.0f);
            HIT = rankRandom(5.0f, 10.0f);
            RES = rankRandom(5.0f, 10.0f);
        }
    }

    public class TraceGunObject : GunObject
    {
        protected override void Awake()
        {
            base.Awake();
            attackInterval = 0.5f;
        }

        protected override void _attack()
        {
            GameObject g = fireBullet("TraceBullet");
            if (g != null && EnemyManager.Instance.count() > 0)
            {
                g.GetComponent<TraceBullet>().Target = EnemyManager.Instance.get(0);
            }
        }
    }
}

