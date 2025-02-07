using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class CreateMenuPanel : BasePanel<CreateMenuPanel>
    {
        public CreateMenuPanel()
        {
            Name = "CreateMenu";
            setPanel();
            Tool.GetComponentInChildren<Button>(panel, "StartCreate").onClick.AddListener(OnClick_StartCreate);
            Tool.GetComponentInChildren<Button>(panel, "Back").onClick.AddListener(OnClick_Back);
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

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
                GlobalInit.Instance.showTip("���Ժ�����");
                return;
            }
            string roomName = Tool.GetComponentInChildren<Text>(panel, "RoomName").text;
            if (string.IsNullOrEmpty(roomName))
            {
                GlobalInit.Instance.showTip("����������Ϊ��");
                return;
            }
            if (roomName.Length > 8) {
                GlobalInit.Instance.showTip("���������Ȳ��ܳ���8λ");
                return;
            }
            if (NetworkConnect.Instance.IsOnline)
            {
                // ��������,(��������,����ѡ��{�����������(���4)},������������)
                RoomOptions roomOptions = new RoomOptions();
                roomOptions.IsOpen = true;
                roomOptions.IsVisible = true;
                roomOptions.MaxPlayers = 4;
                // ��ϷģʽΪ1
                roomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "C0", 1 } };
                roomOptions.CustomRoomPropertiesForLobby = new string[] { "C0" };
                //bool success = PhotonNetwork.CreateRoom(roomName, roomOptions, typedLobby);
                bool success = PhotonNetwork.CreateRoom(roomName, roomOptions);
                if (!success)
                {
                    GlobalInit.Instance.showTip("���䴴��ʧ��");
                    return;
                }
            }
            controller.close();
            controller.show(NewOrContinuePanel.Instance);
        }

        private void OnClick_Back()
        {
            controller.close();
            controller.show(CreateOrJoinPanel.Instance);
        }
    }
}