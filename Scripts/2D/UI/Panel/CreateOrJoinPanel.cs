namespace LAB2D.UI.Panel
{
    using LAB2D;
    using LAB2D.Core;
    using Photon.Pun;
    using Photon.Realtime;
    using UnityEngine.UI;

    /// <summary>
    /// 创建或加入面板
    /// </summary>
    public class CreateOrJoinPanel : ABasePanel<CreateOrJoinPanel>
    {
        private TypedLobby typedLobby = null;

        public CreateOrJoinPanel()
        {
            this.Name = "CreateOrJoinPanel";
            this.Init();
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.Panel, "CreateRoom").onClick.AddListener(this.OnClick_CreateRoom);
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.Panel, "JoinRoom").onClick.AddListener(this.OnClick_JoinRoom);
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
            this.Controller.Show(CreatePanel.Instance);
        }

        private void OnClick_JoinRoom()
        {
            if (PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer ||
                PhotonNetwork.NetworkClientState == ClientState.JoinedLobby)
            {
                // 进入加入房间面板
                this.Controller.Close();
                this.Controller.Show(JoinPanel.Instance);
            }
            else
            {
                ServiceLocator.Get<GlobalInit>().ShowTip("请稍后再试");
            }
        }
    }
}