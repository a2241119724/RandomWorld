using Photon.Pun;
using System;
using UnityEngine;

namespace LAB2D
{
    [Serializable]
    public class SingleGun : Gun
    {
        public SingleGun()
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
            fireBullet("PlayerBullet");
        }
    }
}