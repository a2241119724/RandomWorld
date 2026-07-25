namespace LAB2D.UI.Panel
{
    using LAB2D;
    using LAB2D.Core;
    using Photon.Pun;
    using UnityEngine.UI;

    /// <summary>
    /// 加入菜单面板
    /// </summary>
    public class JoinMenuPanel : ABasePanel<JoinMenuPanel>
    {
        private string selectRoomName; // 当前选择的房间名称

        public JoinMenuPanel()
        {
            this.Name = "JoinMenu";
            this.Init();
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.Panel, "StartJoin").onClick.AddListener(this.OnClick_StartJoin);
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.Panel, "Back").onClick.AddListener(this.OnClick_Back);
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            ServiceLocator.Get<JoinMenuUI>().ClickAndShow += this.Show;

            // 回调OnRoomListUpdate
            PhotonNetwork.GetCustomRoomList(PhotonNetwork.CurrentLobby, "C0 = 1");
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
            ServiceLocator.Get<JoinMenuUI>().ClickAndShow -= this.Show;
        }

        public override void OnClick_Back()
        {
            this.Controller.Close();
            this.Controller.Show(ServiceLocator.Get<CreateOrJoinPanel>());
        }

        private void OnClick_StartJoin()
        {
            if (string.IsNullOrEmpty(this.selectRoomName))
            {
                ServiceLocator.Get<GlobalInit>().ShowTip("房间名不能为空");
                return;
            }

            // 创建房间,(房间名字,房子选项{最大连接人数(最大20)},大厅基本属性)
            bool success = PhotonNetwork.JoinRoom(this.selectRoomName);
            if (!success)
            {
                ServiceLocator.Get<GlobalInit>().ShowTip("房间名字不存在");
                return;
            }

            this.Controller.Close();
            this.Controller.Show(ServiceLocator.Get<AsyncProgressPanel>());
            ServiceLocator.Get<AsyncProgressUI>().SetTip("正在同步数据...");
        }

        private void Show(string str)
        {
            this.selectRoomName = str;
            LAB2D.Tool.Tool.GetComponentInChildren<Text>(this.Panel, "SelectRoomName").text = "选择的房间\n[" + str + "]";
        }
    }
}