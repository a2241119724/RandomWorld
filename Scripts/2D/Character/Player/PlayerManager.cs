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

        public override void loadData()
        {
            Character.CharacterData data = Tool.loadDataByBinary<Character.CharacterData>(GlobalData.ConfigFile.getPath(this.GetType().Name));
            AsyncProgressUI.Instance.complete += () =>
            {
                GameObject g = create(Vector3LAB.toVector3(data.pos));
                Mine = g.GetComponent<Player>();
                Mine.CharacterDataLAB = data;
            };
        }

        public override void saveData()
        {
            mine.CharacterDataLAB.pos = Vector3LAB.toVector3LAB(mine.transform.position);
            Tool.saveDataByBinary(GlobalData.ConfigFile.getPath(this.GetType().Name), mine.CharacterDataLAB);
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