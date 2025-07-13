using UnityEngine;

namespace LAB2D
{
    public class PlayerManager : CharacterManager<PlayerManager, Player, PlayerCreator>
    {
        public TakedWeapon Select { set; get; }
        public Player Mine { set { mine = value; add(value); } get { return mine; } }
        private Player mine;

        public PlayerManager() : base()
        {
            Select = new TakedWeapon();
        }

        public override void LoadData()
        {
            Character.CharacterData data = Tool.LoadDataByBinary<Character.CharacterData>(GlobalData.ConfigFile.getPath(GetType().Name));
            AsyncProgressUI.Instance.complete += () =>
            {
                GameObject g = create(Vector3LAB.toVector3(data.pos));
                Mine = g.GetComponent<Player>();
                Mine.CharacterDataLAB = data;
            };
        }

        public override void SaveData()
        {
            mine.CharacterDataLAB.pos = Vector3LAB.toVector3LAB(mine.transform.position);
            Tool.SaveDataByBinary(GlobalData.ConfigFile.getPath(GetType().Name), mine.CharacterDataLAB);
        }

        public class TakedWeapon
        {
            /// <summary>
            /// 当前装备武器的id
            /// </summary>
            public int id = -1;

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