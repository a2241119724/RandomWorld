using System.Collections.Generic;
using UnityEngine;

namespace LAB2D {
    public class PlayerManager : CharacterManager<PlayerManager,Player,PlayerCreator>
    {
        public SelectWeapon Select { set; get; }
        public Player Mine { set { mine = value; add(value); } get { return mine; } }
        private Player mine;

        public PlayerManager() : base() {
            Select = new SelectWeapon();
        }

        public class SelectWeapon
        {
            /// <summary>
            /// 当前装备武器的id
            /// </summary>
            public int currentId = -1;

            /// <summary>
            /// 当前装备武器的物体
            /// </summary>
            public GameObject weapon = null;

            /// <summary>
            /// 当前装备武器的数据
            /// </summary>
            public Weapon weaponData = null;
        }
    }
}