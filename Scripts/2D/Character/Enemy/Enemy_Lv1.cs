using Photon.Pun;
using UnityEngine;

namespace LAB2D {
    public class Enemy_Lv1 : Enemy
    {
        public float enemyBulletSpeed = 50.0f; // 敌人发射子弹的速度
    
        protected override void Awake()
        {
            base.Awake();
            name = "Enemy_Lv1";
        }

        protected override void Start()
        {
            // 画视觉,听觉,攻击范围
            //Tool.DrawSectorSolid(10, attackRange, new Color32(255, 0, 0, 50), transform);
            //Tool.DrawSectorSolid(SightAngle, SightRange, new Color32(0, 255, 0, 50), transform);
            //Tool.DrawSectorSolid(360, SoundRange, new Color32(0, 0, 255, 50), transform);
            base.Start();
            // 添加状态
            Manager.addStates(EnemyStateType.Wander, new EnemyWanderState(this));
            Manager.addStates(EnemyStateType.Chase, new EnemyChaseState(this));
            Manager.addStates(EnemyStateType.Dead, new EnemyDeadState(this));
            Manager.addStates(EnemyStateType.Seek, new EnemySeekState(this));
            Manager.addStates(EnemyStateType.Attack, new EnemyAttackState(this));
            // 初始化状态
            Manager.changeState(EnemyStateType.Wander);
            target = null;
        }

        [PunRPC]
        public override void attack()
        {
            // 发射子弹
            GameObject g = PhotonNetwork.Instantiate(ResourcesManager.Instance.getPath("EnemyBullet.prefab"), enemyHead.position, Quaternion.identity);
            //GameObject g = Instantiate(enemyBullet, enemyHead.position, Quaternion.identity);
            g.GetComponent<EnemyBullet>().Direction = enemyForward;
            g.GetComponent<EnemyBullet>().BulletSpeed = enemyBulletSpeed;
            damage = Random.Range(1,10);
            g.GetComponent<EnemyBullet>().Damage = damage;
            g.transform.SetParent(transform.parent,false);
        }
    }
}
