using Photon.Pun;
using System;
using UnityEngine;

namespace LAB2D {
    public class Enemy_Lv1 : Enemy
    {
        protected override void Awake()
        {
            base.Awake();
            name = "Enemy_Lv1";
        }

        protected override void Start()
        {
            // »­ÊÓ¾õ,Ìý¾õ,¹¥»÷·¶Î§
            Tool.DrawSectorSolid(10, attackRange, new Color32(255, 0, 0, 50), transform);
            Tool.DrawSectorSolid(SightAngle, SightRange, new Color32(0, 255, 0, 50), transform);
            Tool.DrawSectorSolid(360, SoundRange, new Color32(0, 0, 255, 50), transform);
            base.Start();
            // Ìí¼Ó×´Ì¬
            Manager.addStates(EnemyStateType.Wander, new EnemyWanderState(this));
            Manager.addStates(EnemyStateType.Chase, new EnemyChaseState(this));
            Manager.addStates(EnemyStateType.Dead, new EnemyDeadState(this));
            Manager.addStates(EnemyStateType.Seek, new EnemySeekState(this));
            Manager.addStates(EnemyStateType.Attack, new EnemyAttackState(this));
            // ³õÊ¼»¯×´Ì¬
            Manager.changeState(EnemyStateType.Wander);
            Target = null;
        }

        [PunRPC]
        public override void attack()
        {
            // ·¢Éä×Óµ¯
            GameObject g = Tool.Instantiate(ResourcesManager.Instance.getPrefab("EnemyBullet"), EnemyHead.position, Quaternion.identity);
            //GameObject g = Instantiate(enemyBullet, enemyHead.position, Quaternion.identity);
            g.GetComponent<EnemyBullet>().Direction = EnemyHead.position-transform.position;
            g.GetComponent<EnemyBullet>().BulletSpeed = bulletSpeed;
            damage = UnityEngine.Random.Range(1,10);
            g.GetComponent<EnemyBullet>().Damage = damage;
            g.transform.SetParent(transform.parent,false);
        }
    }
}
