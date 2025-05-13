using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LAB2D
{
    public class PauseMenuPanel : BasePanel<PauseMenuPanel>
    {
        private AudioSource audioSource; // ������

        public PauseMenuPanel()
        {
            Name = "PauseMenu";
            setPanel();
            audioSource = GameObject.FindGameObjectWithTag(ResourceConstant.UI_TAG_ROOT).GetComponent<AudioSource>();
            if (audioSource == null)
            {
                LogManager.Instance.log("audioSource Not Found!!!", LogManager.LogLevel.Error);
                return;
            }
            Tool.GetComponentInChildren<Button>(panel, "Exit").onClick.AddListener(OnClick_Exit);
            Tool.GetComponentInChildren<Button>(panel, "BackMenu").onClick.AddListener(OnClick_BackMenu);
            Tool.GetComponentInChildren<Slider>(panel, "Audio").onValueChanged.AddListener(OnClick_Audio);
            Tool.GetComponentInChildren<Button>(panel, "BackGame").onClick.AddListener(OnClick_BackGame);
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        /// <summary>
        /// �˳���Ϸ
        /// </summary>
        public void OnClick_Exit()
        {
            // ��Ҫ�ر�����
            PhotonNetwork.LeaveLobby();
            PhotonNetwork.Disconnect();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        }

        /// <summary>
        /// ���ز˵�
        /// </summary>
        public void OnClick_BackMenu()
        {
            Tool.loadScene("Menu");
        }

        /// <summary>
        /// ��������
        /// </summary>
        public void OnClick_Audio(float value)
        {
            audioSource.volume = value;
        }

        /// <summary>
        /// ������Ϸ
        /// </summary>
        public void OnClick_BackGame()
        {
            controller.close();
        }
    }
}