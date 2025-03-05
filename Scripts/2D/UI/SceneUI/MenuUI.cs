using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LAB2D
{
    public class MenuUI : MonoBehaviour
    {
        private Toggle toggle; // ���ѡ��
        private GameObject note; // ֪ͨ

        private void Awake()
        {
            GlobalInit.Instance.showTip("��¼�ɹ�!!!");
        }

        void Start()
        {
            PhotonNetwork.NickName = "aaa";
            toggle = Tool.GetComponentInChildren<Toggle>(gameObject, "Clause").GetComponent<Toggle>();
            note = transform.Find("Center/Note").gameObject;
            if (note == null)
            {
                LogManager.Instance.log("note Not Found!!!", LogManager.LogLevel.Error);
                return;
            }
            Tool.GetComponentInChildren<Button>(gameObject,"Start").onClick.AddListener(OnClick_Start);
            Tool.GetComponentInChildren<Button>(gameObject, "NoteClose").onClick.AddListener(OnClick_NoteClose);
        }

        /// <summary>
        /// ��ʼ��Ϸ
        /// </summary>
        public void OnClick_Start()
        {
            if (toggle.isOn)
            {
                string name = Tool.GetComponentInChildren<Text>(gameObject, "PlayerName").text;
                if (name.Length <= 0) {
                    GlobalInit.Instance.showTip("���ֲ���Ϊ��!!!");
                    return;
                }
                PhotonNetwork.NickName = name;
                SceneManager.LoadScene("Game"); // ���س���
            }
            else
            {
                GlobalInit.Instance.showTip("δ��ѡ����!!!");
            }
        }

        /// <summary>
        /// �ر�ͨ��
        /// </summary>
        public void OnClick_NoteClose()
        {
            note.SetActive(false);
        }
    }
}