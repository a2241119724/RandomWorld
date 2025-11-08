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
            AsyncProgressUI.Instance.SetTip("加载玩家管理信息...");
            Player.PlayerData data = DataTool.LoadDataByBinary<Player.PlayerData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            AsyncProgressUI.Instance.Complete += () =>
            {
                GameObject g = this.Create(Vector3LAB.ToVector3(data.Pos));
                this.Mine = g.GetComponent<Player>();
                this.Mine.CharacterDataLAB = data;
            };
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            // 仅保存玩家自己的信息
            this.mine.CharacterDataLAB.Pos = Vector3LAB.ToVector3LAB(this.mine.transform.position);
            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), this.mine.CharacterDataLAB);
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
            public AWeapon WeaponData = null;
        }
    }
}