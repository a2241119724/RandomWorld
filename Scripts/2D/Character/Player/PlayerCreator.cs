using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class PlayerCreator : CharacterCreator<PlayerCreator>
    {
        /// <summary>
        /// ÊµÀý»¯Íæ¼Ò
        /// </summary>
        protected override GameObject _create(Vector3 worldPos, string name, string layer)
        {
            return base._create(worldPos, "Player", "Player");
        }
    }
}