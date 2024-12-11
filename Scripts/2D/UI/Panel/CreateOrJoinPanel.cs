using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class CreateOrJoinPanel : BasePanel<CreateOrJoinPanel>
    {
        private TypedLobby typedLobby = null;

        public CreateOrJoinPanel()
        {
            Name = "CreateOrJoin";
            setPanel();
            Tool.GetComponentInChildren<Button>(panel, "CreateRoom").onClick.AddListener(OnClick_CreateRoom);
            Tool.GetComponentInChildren<Button>(panel, "JoinRoom").onClick.AddListener(OnClick_JoinRoom);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            typedLobby = new TypedLobby("myLobby", LobbyType.SqlLobby);
            PhotonNetwork.JoinLobby(typedLobby);
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        private void OnClick_CreateRoom()
        {
            // 进入创建房间面板
            controller.close();
            controller.show(CreateMenuPanel.Instance);
        }

        private void OnClick_JoinRoom()
        {
            if (PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer ||
                PhotonNetwork.NetworkClientState == ClientState.JoinedLobby){
                // 进入加入房间面板
                controller.close();
                controller.show(JoinMenuPanel.Instance);
            }
            else {
                GlobalInit.Instance.showTip("请稍后再试");
            }
        }
    }
}