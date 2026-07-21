namespace LAB2D.UI.Panel
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 暂停菜单面板
    /// </summary>
    public class PauseMenuPanel : ABasePanel<PauseMenuPanel>
    {
        private readonly AudioSource audioSource; // 被控制

        public PauseMenuPanel()
        {
            this.Name = "PauseMenu";
            this.Init();
            this.audioSource = GameObject.FindGameObjectWithTag(TagConstant.UI_TAG).GetComponent<AudioSource>();
            if (this.audioSource == null)
            {
                AWorkerTask.LogProvider("audioSource Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.Panel, "Exit").onClick.AddListener(this.OnClick_Exit);
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.Panel, "BackMenu").onClick.AddListener(this.OnClick_BackMenu);
            LAB2D.Tool.Tool.GetComponentInChildren<Slider>(this.Panel, "Audio").onValueChanged.AddListener(this.OnClick_Audio);
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.Panel, "BackGame").onClick.AddListener(this.OnClick_Back);
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

        public override void OnClick_Back()
        {
            this.Controller.Close();
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        private void OnClick_Exit()
        {
            // 需要关闭连接
            PhotonNetwork.LeaveLobby();
            PhotonNetwork.Disconnect();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        }

        /// <summary>
        /// 返回菜单
        /// </summary>
        private void OnClick_BackMenu()
        {
            PanelController.Instance.Close();
            PanelController.Instance.Show(CreateOrJoinPanel.Instance);
        }

        /// <summary>
        /// 调节音量
        /// </summary>
        private void OnClick_Audio(float value)
        {
            this.audioSource.volume = value;
        }
    }
}