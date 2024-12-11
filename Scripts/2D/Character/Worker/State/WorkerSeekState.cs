using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class WorkerSeekState : CharacterState<Worker>
    {
        private Vector3Int targetMap;
        private string preString = "";
        private bool isOne = true;

        public WorkerSeekState(Worker character) : base(character)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            isOne = true;
            // û������
            Vector3Int posMap = new Vector3Int(Mathf.RoundToInt(Character.transform.position.y), Mathf.RoundToInt(Character.transform.position.x), 0);
            targetMap = TileMap.Instance.genAvailablePosMap(posMap);
            preString = "";
            if (Character.Manager.Task != null)
            {
                // ������
                targetMap = Character.Manager.Task.TargetMap;
                // ���Աߵ�λ�ý��н���
                bool isReach = false;
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        if (i == 0 && j == 0) continue;
                        targetMap = new Vector3Int(targetMap.x + i, targetMap.y + j, 0);
                        if (Character.isCanReach(targetMap))
                        {
                            isReach = true;
                            break;
                        }
                    }
                    if (isReach) break;
                }
                preString = "<color=red>Worker</color>\n";
            }
            Character.initSeek(targetMap);
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            Character.WorkerState.text = preString + $"<color=yellow>Seeking:{Mathf.RoundToInt(Character.SeekProgress * 100)}%</color>\n"+
                $"Target: {targetMap.y},{targetMap.x}";
            // û�����������Լ��ϵ���
            if (!GlobalData.Lock.SeekLock.seekLock ||
                (GlobalData.Lock.SeekLock.seekLock && GlobalData.Lock.SeekLock.owner == Character)) {
                // ֻ����һ����Ѱ·(����),���������������ӵ���߲����Լ���������������
                if (isOne) {
                    GlobalData.Lock.SeekLock.seekLock = true;
                    GlobalData.Lock.SeekLock.owner = Character;
                    isOne = false;
                    Character.toTarget();
                }
            }
            if (!Character.IsSeeking) {
                GlobalData.Lock.SeekLock.seekLock = false;
                // Ѱ·����
                Character.Manager.changeState(WorkerStateType.Move);
            }
        }
    }
}

