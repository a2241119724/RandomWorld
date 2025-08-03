namespace LAB2D
{
    using Photon.Pun;
    using Photon.Realtime;
    using UnityEngine.UI;

    /// <summary>
    /// 创建面板
    /// </summary>
    public class CreateMenuPanel : BasePanel<CreateMenuPanel>
    {
        public CreateMenuPanel()
        {
            this.Name = "CreateMenu";
            this.Init();
            Tool.GetComponentInChildren<Button>(this.Panel, "StartCreate").onClick.AddListener(this.OnClick_StartCreate);
            Tool.GetComponentInChildren<Button>(this.Panel, "Back").onClick.AddListener(this.OnClick_Back);
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }

        private void OnClick_StartCreate()
        {
            if (PhotonNetwork.NetworkClientState != ClientState.ConnectedToMasterServer
                && PhotonNetwork.NetworkClientState != ClientState.JoinedLobby
                && NetworkConnect.Instance.IsOnline)
            {
                GlobalInit.Instance.ShowTip("请稍后再试");
                return;
            }

            string roomName = Tool.GetComponentInChildren<Text>(this.Panel, "RoomName").text;
            if (string.IsNullOrEmpty(roomName))
            {
                GlobalInit.Instance.ShowTip("房间名不能为空");
                return;
            }

            if (roomName.Length > 8)
            {
                GlobalInit.Instance.ShowTip("房间名长度不能超过8位");
                return;
            }

            if (NetworkConnect.Instance.IsOnline)
            {
                // 创建房间,(房间名字,房子选项{最大连接人数(最大4)},大厅基本属性)
                RoomOptions roomOptions = new ();
                roomOptions.IsOpen = true;
                roomOptions.IsVisible = true;
                roomOptions.MaxPlayers = 4;

                // 游戏模式为1
                roomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "C0", 1 } };
                roomOptions.CustomRoomPropertiesForLobby = new string[] { "C0" };

                // bool success = PhotonNetwork.CreateRoom(roomName, roomOptions, typedLobby);
                bool success = PhotonNetwork.CreateRoom(roomName, roomOptions);
                if (!success)
                {
                    GlobalInit.Instance.ShowTip("房间创建失败");
                    return;
                }
            }

            this.Controller.Close();
            this.Controller.Show(NewOrContinuePanel.Instance);
        }

        private void OnClick_Back()
        {
            this.Controller.Close();
            this.Controller.Show(CreateOrJoinPanel.Instance);
        }
    }
}