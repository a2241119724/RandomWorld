using Photon.Pun;
using UnityEngine;

namespace LAB2D
{
    public class EnemySeekState : CharacterState<Enemy>
    {
        private float _recordTime = 0.0f;
        private const float seekTime = 3.0f; // 敌人被攻击搜索时间

        public EnemySeekState(Enemy character) : base(character)
        {
        }

        public override void OnEnter()
        {
            //LogManager.Instance.log("SeekState", LogManager.LogLevel.Info);
        }

        public override void OnExit()
        {
        }

        public override void OnUpdate()
        {
            // 如果一段时间后没有找到搜索目标,那么回到游荡状态
            _recordTime += Time.deltaTime;
            if (_recordTime > seekTime)
            {
                Character.Manager.changeState(EnemyStateType.Wander); // 进入游荡状态
                return;
            }
            // 感知人物是否在范围内，进入追踪状态
            if (Character.SenseNearby(Character.Target.transform))
            {
                Character.Manager.changeState(EnemyStateType.Chase);
                return;
            }
            // 如果受到攻击,那么向着玩家方向进行搜索
            Character.rotateTo(Character.Target.transform.position - Character.transform.position); 
            Character.moveToForward();
            // TODO可以奔跑搜索，以后实现
        }
    }
}