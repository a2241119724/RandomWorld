using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D {
    public class EnemyStateManager<CS, CST, C> : CharacterStateManager<CS, CST, C> where CS : ICharacterState where CST : Enum where C : Enemy
    {
        public EnemyStateManager(C character) : base(character)
        {
        }
    }
}

