using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class JoinMenuPanel : BasePanel<JoinMenuPanel>
    {
        private string selectRoomName; // 当前选择的房间名称

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
            // 回调OnRoomListUpdate
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
                GlobalInit.Instance.showTip("房间名不能为空");
                return;
            }
            // 创建房间,(房间名字,房子选项{最大连接人数(最大20)},大厅基本属性)
            bool success = PhotonNetwork.JoinRoom(selectRoomName);
            if (!success) {
                GlobalInit.Instance.showTip("房间名字不存在");
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
            Tool.GetComponentInChildren<Text>(panel, "SelectRoomName").text = "选择的房间\n[" + str + "]";
        }
    }
}