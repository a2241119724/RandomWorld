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
            Core.GameServices.AsyncProgressSetTipProvider("加载玩家管理信息...");
            Player.PlayerData data = DataTool.LoadDataByBinary<Player.PlayerData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            if (data == null)
            {
                // 降级方案：存档无玩家数据时，进度完成后在随机位置创建玩家
                AWorkerTask.LogProvider("Player data not found in archive, will create player at random position after loading", LogManager.LogLevelEnum.Warning);
                Core.GameServices.AsyncProgressCompleteProvider(() =>
                {
                    GameObject g = this.Create();
                    if (g == null)
                    {
                        AWorkerTask.LogProvider("Failed to create player at random position", LogManager.LogLevelEnum.Error);
                        return;
                    }

                    this.Mine = g.GetComponent<Player>();
                });
                return;
            }

            Core.GameServices.AsyncProgressCompleteProvider(() =>
            {
                GameObject g = this.Create(Vector3LAB.ToVector3(data.Pos));
                if (g == null)
                {
                    AWorkerTask.LogProvider("Failed to create player from saved data", LogManager.LogLevelEnum.Error);
                    return;
                }

                this.Mine = g.GetComponent<Player>();
                if (this.Mine == null)
                {
                    AWorkerTask.LogProvider("Player prefab is missing Player component", LogManager.LogLevelEnum.Error);
                    return;
                }

                this.Mine.CharacterDataLAB = data;
                this.Mine.CharacterDataLAB.Character = this.Mine;
            });
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