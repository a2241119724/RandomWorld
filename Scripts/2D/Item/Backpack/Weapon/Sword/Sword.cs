using System;
using UnityEngine;

namespace LAB2D
{
    [Serializable]
    public abstract class Sword : Weapon { 
    }

    // 该脚本被玩家装备才会激活
    public abstract class SwordObject : WeaponObject
    {
        protected GameObject blood; // 掉血特效

        protected override void Awake()
        {
            base.Awake();
            raduis = 8.0f;
            blood = ResourcesManager.Instance.getPrefab("Blood");
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();
        }
    }
}