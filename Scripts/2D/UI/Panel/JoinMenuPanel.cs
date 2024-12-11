using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class JoinMenuPanel : BasePanel<JoinMenuPanel>
    {
        private string selectRoomName; // ��ǰѡ��ķ�������

        public JoinMenuPanel()
        {
            Name = "JoinMenu";
            setPanel();
            Tool.GetComponentInChildren<Button>(panel, "StartJoin").onClick.AddListener(OnClick_StartJoin);
            Tool.GetComponentInChildren<Button>(panel, "Back").onClick.AddListener(OnClick_Back);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            RoomUI.Instance.ClickAndShow += show;
            // �ص�OnRoomListUpdate
            PhotonNetwork.GetCustomRoomList(PhotonNetwork.CurrentLobby, "C0 = 1");
        }

        public override void OnExit()
        {
            base.OnExit();
            RoomUI.Instance.ClickAndShow -= show;
        }

        private void OnClick_StartJoin()
        {
            if (string.IsNullOrEmpty(selectRoomName)) {
                GlobalInit.Instance.showTip("����������Ϊ��");
                return;
            }
            // ��������,(��������,����ѡ��{�����������(���20)},������������)
            bool success = PhotonNetwork.JoinRoom(selectRoomName);
            if (!success) {
                GlobalInit.Instance.showTip("�������ֲ�����");
                return;
            }
            controller.close();
            controller.show(ForegroundPanel.Instance);
        }

        private void OnClick_Back()
        {
            controller.close();
            controller.show(CreateOrJoinPanel.Instance);
        }

        private void show(string str) {
            selectRoomName = str;
            Tool.GetComponentInChildren<Text>(panel, "SelectRoomName").text = "ѡ��ķ���\n[" + str + "]";
        }
    }
}