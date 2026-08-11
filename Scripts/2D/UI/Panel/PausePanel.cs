namespace LAB2D.UI.Panel
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 暂停面板
    /// </summary>
    public class PausePanel : ABasePanel<PausePanel>
    {
        private readonly AudioSource audioSource; // 被控制
        private bool isExitRequested;

        public PausePanel()
        {
            this.Name = "PausePanel";
            this.Init();

            // 查找 AudioSource — 如果不存在则记录错误但不阻断按钮注册
            this.audioSource = GameObject.FindGameObjectWithTag(TagConstant.UI_TAG)?.GetComponent<AudioSource>();
            if (this.audioSource == null)
            {
                AWorkerTask.LogProvider("audioSource Not Found on UIRoot, audio slider will be disabled.", LogManager.LogLevelEnum.Warning);
            }

            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.Panel, "Exit")?.onClick.AddListener(this.OnClick_Exit);
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.Panel, "BackMenu")?.onClick.AddListener(this.OnClick_BackMenu);
            LAB2D.Tool.Tool.GetComponentInChildren<Slider>(this.Panel, "Audio")?.onValueChanged.AddListener(this.OnClick_Audio);
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.Panel, "BackGame")?.onClick.AddListener(this.OnClick_Back);
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
            if (this.isExitRequested)
            {
                return;
            }

            this.isExitRequested = true;
            try
            {
                LAB2D.Core.Seek.ASeek.Shutdown();

                if (PhotonNetwork.IsConnected)
                {
                    if (PhotonNetwork.InRoom)
                    {
                        PhotonNetwork.LeaveRoom(false);
                    }
                    else if (PhotonNetwork.InLobby)
                    {
                        PhotonNetwork.LeaveLobby();
                    }

                    PhotonNetwork.Disconnect();
                }

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
            catch (System.Exception ex)
            {
                this.isExitRequested = false;
                Debug.LogError($"[ExitFlow] 异常: {ex}");
            }
        }

        /// <summary>
        /// 返回菜单 - 清空整个面板栈后回到主菜单
        /// </summary>
        private void OnClick_BackMenu()
        {
            // 逐层关闭所有面板，确保每层的 OnExit 被正确调用
            PanelController controller = ServiceLocator.Get<PanelController>();
            while (controller.Panels.Count > 0)
            {
                controller.Close();
            }

            controller.Show(CreateOrJoinPanel.Instance);
        }

        /// <summary>
        /// 调节音量
        /// </summary>
        private void OnClick_Audio(float value)
        {
            if (this.audioSource != null)
            {
                this.audioSource.volume = value;
            }
        }
    }
}
