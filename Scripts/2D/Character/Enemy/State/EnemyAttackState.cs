using Photon.Pun;
using UnityEngine;

namespace LAB2D
{
    public class EnemyAttackState : CharacterState<Enemy>
    {
        public float AttackTime { get; private set; } = 0.0f;

        private float _recordTime = 0.0f;
        private static readonly float attackInterval = 1.0f;

        public EnemyAttackState(Enemy character) : base(character)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            AttackTime = 0.0f;
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void OnUpdate()
        {
            AttackTime += Time.deltaTime;
            // 如果玩家与敌人的距离大于敌人的攻击距离，那么进入追踪状态
            if (Vector3.Distance(Character.Target.transform.position, Character.transform.position) > Character.attackRange)
            {
                Character.Manager.changeState(EnemyStateType.Chase);
                return;
            }
            if (Character.SenseNearby(Character.Target.transform))
            {
                //animator.SetBool("isAttack", true);
                _recordTime += Time.deltaTime;
                if (_recordTime > attackInterval) // 攻击间隔时间
                {
                    _recordTime = 0.0f;
                    //if (zombieAttackAudio != null)
                    //AudioSource.PlayClipAtPoint(zombieAttackAudio, transform.position);
                    // 攻击
                    //character.attack();
                    if (NetworkConnect.Instance.IsOnline)
                    {
                        Character.photonView.RPC("attack",RpcTarget.All);
                    }
                    else
                    {
                        Character.attack();
                    }
                    //g.GetComponent<Rigidbody2D>().velocity = g.transform.TransformDirection(character.characterForward.normalized * character.characterBulletSpeed); // 刚体的速度
                    //Object.Destroy(g, 1.0f);
                }
                Vector3 direction = Character.Target.transform.position - Character.transform.position;
                Character.rotateTo(direction); // 旋转
                if (direction.magnitude > 3.0f) // 2米之外向玩家移动
                {
                    Character.moveToForward();
                }
                // animator.SetBool("isAttack", false);
                return;
            }
            // 如果敌人感知范围内没有玩家，进入搜寻状态
            Character.Manager.changeState(EnemyStateType.Seek);
            // animator.SetBool("isAttack", false);
        }
    }
}