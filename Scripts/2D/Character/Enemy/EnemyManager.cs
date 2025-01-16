using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class EnemyManager : CharacterManager<EnemyManager,Enemy,EnemyCreator>
    {
        /// <summary>
        /// ����������
        /// </summary>
        public int MaxEnemyCount { set; get; }
    }
}