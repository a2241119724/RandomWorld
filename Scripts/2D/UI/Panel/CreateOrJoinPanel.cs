namespace LAB2D
{
    using Photon.Pun;
    using Photon.Realtime;
    using UnityEngine.UI;

    /// <summary>
    /// 创建或加入面板
    /// </summary>
    public class CreateOrJoinPanel : BasePanel<CreateOrJoinPanel>
    {
        private TypedLobby typedLobby = null;

        public CreateOrJoinPanel()
        {
            this.Name = "CreateOrJoin";
            this.Open();
            Tool.GetComponentInChildren<Button>(this.Panel, "CreateRoom").onClick.AddListener(this.OnClick_CreateRoom);
            Tool.GetComponentInChildren<Button>(this.Panel, "JoinRoom").onClick.AddListener(this.OnClick_JoinRoom);
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            this.typedLobby = new TypedLobby("myLobby", LobbyType.SqlLobby);
            PhotonNetwork.JoinLobby(this.typedLobby);
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }

        private void OnClick_CreateRoom()
        {
            // 进入创建房间面板
            this.Controller.Close();
            this.Controller.Show(CreateMenuPanel.Instance);
        }

        private void OnClick_JoinRoom()
        {
            if (PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer ||
                PhotonNetwork.NetworkClientState == ClientState.JoinedLobby)
            {
                // 进入加入房间面板
                this.Controller.Close();
                this.Controller.Show(JoinMenuPanel.Instance);
            }
            else
            {
                GlobalInit.Instance.ShowTip("请稍后再试");
            }
        }
    }
}