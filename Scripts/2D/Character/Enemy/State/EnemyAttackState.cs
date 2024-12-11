using Photon.Pun;
using UnityEngine;

namespace LAB2D
{
    public class EnemyAttackState : CharacterState<Enemy>
    {
        private float recordTime = 0.0f;

        public EnemyAttackState(Enemy character) : base(character)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Character.continueTiming = 0.0f;
        }

        public override void OnExit()
        {
            base.OnExit();
            Character.continueTiming = 0.0f;
        }

        public override void OnUpdate()
        {
            Character.continueTiming += Time.deltaTime;
            // 如果玩家与敌人的距离大于敌人的攻击距离，那么进入追踪状态
            //int count = PlayerManager.Instance.count();
            //for (int i = 0; i < count; i++)
            //{
            //    if (Vector3.Distance(PlayerManager.Instance.get(i).transform.position, character.transform.position) > character.attackRange)
            //    {
            //        character.manager.changeState(EnemyStateType.Chase);
            //        return;
            //    }
            //}
            if (Vector3.Distance(Character.target.transform.position, Character.transform.position) > Character.attackRange)
            {
                Character.attackFlag = false;
                Character.Manager.changeState(EnemyStateType.Chase);
                return;
            }
            //for (int i = 0; i < count; i++)
            //{
            //    if (character.SenseNearby(PlayerManager.Instance.get(i).transform))
            //    {
            //        Vector3 direction = PlayerManager.Instance.get(i).transform.position - character.transform.position;
            //        //animator.SetBool("isAttack", true);
            //        recordTime += Time.deltaTime;
            //        if (recordTime > 1.0f) // 攻击间隔时间
            //        {
            //            recordTime = 0.0f;
            //            //if (zombieAttackAudio != null)
            //            //AudioSource.PlayClipAtPoint(zombieAttackAudio, transform.position);
            //            // 攻击
            //            character.attack();
            //            //g.GetComponent<Rigidbody2D>().velocity = g.transform.TransformDirection(character.characterForward.normalized * character.characterBulletSpeed); // 刚体的速度
            //            //Object.Destroy(g, 1.0f);
            //        }
            //        character.RotateTo(direction); // 旋转
            //        if (direction.magnitude > 3.0f) // 2米之外向玩家移动
            //        {
            //            character.MoveToForward();
            //        }
            //        // animator.SetBool("isAttack", false);
            //        return;
            //    }
            //}
            if (Character.SenseNearby(Character.target.transform))
            {
                //animator.SetBool("isAttack", true);
                recordTime += Time.deltaTime;
                if (recordTime > 1.0f) // 攻击间隔时间
                {
                    recordTime = 0.0f;
                    //if (zombieAttackAudio != null)
                    //AudioSource.PlayClipAtPoint(zombieAttackAudio, transform.position);
                    // 攻击
                    //character.attack();
                    Character.photonView.RPC("attack",RpcTarget.All);
                    //g.GetComponent<Rigidbody2D>().velocity = g.transform.TransformDirection(character.characterForward.normalized * character.characterBulletSpeed); // 刚体的速度
                    //Object.Destroy(g, 1.0f);
                }
                Vector3 direction = Character.target.transform.position - Character.transform.position;
                //character.GetComponent<PhotonView>().RPC("RotateTo", RpcTarget.All, direction);
                Character.RotateTo(direction); // 旋转
                if (direction.magnitude > 3.0f) // 2米之外向玩家移动
                {
                    //character.GetComponent<PhotonView>().RPC("MoveToForward", RpcTarget.All);
                    Character.MoveToForward();
                }
                // animator.SetBool("isAttack", false);
                return;
            }
            // 如果敌人感知范围内没有玩家，进入搜寻状态
            Character.target = null;
            Character.attackFlag = false;
            Character.Manager.changeState(EnemyStateType.Seek);
            // animator.SetBool("isAttack", false);
        }
    }
}