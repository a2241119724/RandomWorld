using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class Worker_Lv1 : Worker
    {
        protected override void Awake()
        {
            base.Awake();
            // ���״̬
            Manager.addStates(WorkerStateType.Move, new WorkerMoveState(this));
            Manager.addStates(WorkerStateType.Work, new WorkerWorkState(this));
            Manager.addStates(WorkerStateType.Seek, new WorkerSeekState(this));
            // ��ʼ��״̬
            Manager.changeState(WorkerStateType.Seek);
        }
    }
}

