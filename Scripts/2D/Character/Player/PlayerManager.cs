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
            /// ��ǰװ��������id
            /// </summary>
            public int currentId = -1;

            /// <summary>
            /// ��ǰװ������������
            /// </summary>
            public GameObject weapon = null;

            /// <summary>
            /// ��ǰװ������������
            /// </summary>
            public Weapon weaponData = null;
        }
    }
}