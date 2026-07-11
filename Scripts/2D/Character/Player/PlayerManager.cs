namespace LAB2D.Character.Player
{
    using LAB2D;
    using LAB2D.Serializable;
    using UnityEngine;

    /// <summary>
    /// 玩家管理器
    /// </summary>
    public class PlayerManager : CharacterManager<PlayerManager, Player, PlayerCreator>
    {
        private Player mine;

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
                this.Mine.CharacterDataLAB.Character = this.Mine;
            };
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            // 仅保存玩家自己的信息
            this.mine.CharacterDataLAB.Pos = Vector3LAB.ToVector3LAB(this.mine.transform.position);
            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), this.mine.CharacterDataLAB);
        }
    }
}