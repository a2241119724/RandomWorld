namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 玩家管理器
    /// </summary>
    public class PlayerManager : CharacterManager<PlayerManager, Player, PlayerCreator>
    {
        private Player mine;

        public PlayerManager()
            : base()
        {
            this.Select = new TakedWeapon();
        }

        /// <summary>
        /// 当前选中的武器
        /// </summary>
        public TakedWeapon Select { get; set; }

        /// <summary>
        /// 本地玩家
        /// </summary>
        public Player Mine
        {
            get
            {
                return this.mine;
            }

            set
            {
                this.mine = value;
                this.Add(value);
            }
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            Character.CharacterData data = Tool.LoadDataByBinary<Character.CharacterData>(GlobalData.ConfigFile.getPath(this.GetType().Name));
            AsyncProgressUI.Instance.complete += () =>
            {
                GameObject g = this.create(Vector3LAB.ToVector3(data.Pos));
                this.Mine = g.GetComponent<Player>();
                this.Mine.CharacterDataLAB = data;
            };
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            this.mine.CharacterDataLAB.Pos = Vector3LAB.ToVector3LAB(this.mine.transform.position);
            Tool.SaveDataByBinary(GlobalData.ConfigFile.getPath(this.GetType().Name), this.mine.CharacterDataLAB);
        }

        /// <summary>
        /// 持有的武器
        /// </summary>
        public class TakedWeapon
        {
            /// <summary>
            /// 当前装备武器的id
            /// </summary>
            public int Id = -1;

            /// <summary>
            /// 当前装备武器的物体
            /// </summary>
            public GameObject Weapon = null;

            /// <summary>
            /// 当前装备武器的数据
            /// </summary>
            public Weapon WeaponData = null;
        }
    }
}