using Photon.Pun;
using UnityEngine;

namespace LAB2D {
    public class Enemy_Lv1 : Enemy
    {
        public float enemyBulletSpeed = 50.0f; // ���˷����ӵ����ٶ�
    
        protected override void Awake()
        {
            base.Awake();
            name = "Enemy_Lv1";
        }

        protected override void Start()
        {
            // ���Ӿ�,����,������Χ
            //Tool.DrawSectorSolid(10, attackRange, new Color32(255, 0, 0, 50), transform);
            //Tool.DrawSectorSolid(SightAngle, SightRange, new Color32(0, 255, 0, 50), transform);
            //Tool.DrawSectorSolid(360, SoundRange, new Color32(0, 0, 255, 50), transform);
            base.Start();
            // ���״̬
            Manager.addStates(EnemyStateType.Wander, new EnemyWanderState(this));
            Manager.addStates(EnemyStateType.Chase, new EnemyChaseState(this));
            Manager.addStates(EnemyStateType.Dead, new EnemyDeadState(this));
            Manager.addStates(EnemyStateType.Seek, new EnemySeekState(this));
            Manager.addStates(EnemyStateType.Attack, new EnemyAttackState(this));
            // ��ʼ��״̬
            Manager.changeState(EnemyStateType.Wander);
            target = null;
        }

        [PunRPC]
        public override void attack()
        {
            // �����ӵ�
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
