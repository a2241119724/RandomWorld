using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class WorkerSeekState : WorkerState
    {
        private Vector3Int targetMap;
        private bool isOne = true;

        public WorkerSeekState(Worker character) : base(character)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            // 如果饥饿并且没有吃饭任务就进入饥饿状态,做完任务再吃饭
            if (Character.CurHungry < Worker.ThresholdHungry && Character.Manager.Task == null) {
                Character.Manager.changeState(WorkerStateType.Hungry);
                return;
            }
            isOne = true;
            // 没有任务
            Vector3Int posMap = TileMap.Instance.worldPosToMapPos(Character.transform.position);
            targetMap = TileMap.Instance.genAvailablePosMap(posMap);
            if (Character.Manager.Task != null)
            {
                // 有任务
                targetMap = Character.Manager.Task.TargetMap;
                // 找旁边的位置进行建造
                float minDistance = 99999.0f;
                Vector3Int closedPos = default(Vector3Int);
                foreach (Vector3Int pos in Character.Manager.Task.AvailableNeighborPos)
                {
                    // 由于是斜对称
                    Vector3Int temp = new Vector3Int(targetMap.x + pos.y, targetMap.y + pos.x, 0);
                    if (Character.isCanReach(temp))
                    {
                        Vector3 worldPos = TileMap.Instance.mapPosToWorldPos(temp);
                        float distance = Mathf.Pow(worldPos.x-Character.transform.position.x,2) +
                            Mathf.Pow(worldPos.y - Character.transform.position.y, 2);
                        if(distance < minDistance)
                        {
                            minDistance = distance;
                            closedPos = temp;
                        }
                    }
                }
                if(closedPos == default(Vector3Int))
                {
                    Debug.Log("没有邻居位置!!!");
                }
                targetMap = closedPos;
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
                $"Target: {targetMap.x},{targetMap.y}";
            if (Worker.seekLock.getLock(Character))
            {
                // 只能有一个在寻路(加锁),如果被锁了且锁的拥有者不是自己则阻塞，可重入
                if (isOne)
                {
                    isOne = false;
                    Character.toTarget();
                }
            }
            if (!Character.IsSeeking) {
                Worker.seekLock.releaseLock(Character);
                // 寻路结束
                Character.Manager.changeState(WorkerStateType.Move);
            }
        }
    }
}

