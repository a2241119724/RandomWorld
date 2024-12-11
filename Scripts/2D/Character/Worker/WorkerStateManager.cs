using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class WorkerStateManager<CS, CST, C> : CharacterStateManager<CS, CST, C> where CS : ICharacterState where CST : Enum where C : Worker
    {
        public WorkerTask Task { get; set; }

        public WorkerStateManager(C character) : base(character) { 
        }

        public override void changeState(CST type)
        {
            // ��ִ��,������Enter�и���,��Ȼ�ᱻ����
            Character.WorkerState.text = CurrentStateType.ToString();
            base.changeState(type);
        }
    }
}
