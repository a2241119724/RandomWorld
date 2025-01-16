using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class EnemyManager : CharacterManager<EnemyManager,Enemy,EnemyCreator>
    {
        /// <summary>
        /// 最大敌人数量
        /// </summary>
        public int MaxEnemyCount { set; get; }
    }
}