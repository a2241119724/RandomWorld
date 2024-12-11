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
        public List<Vector3> EnemyPositions { set; get; }

        public EnemyManager() {
            EnemyPositions = new List<Vector3>();
        }

        public int totalCount()
        {
            return EnemyPositions.Count + Characters.Count;
        }
    }
}