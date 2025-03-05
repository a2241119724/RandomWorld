using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LAB2D
{
    public class MenuUI : MonoBehaviour
    {
        private Toggle toggle; // 条款勾选框
        private GameObject note; // 通知

        private void Awake()
        {
            GlobalInit.Instance.showTip("登录成功!!!");
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
        /// 开始游戏
        /// </summary>
        public void OnClick_Start()
        {
            if (toggle.isOn)
            {
                string name = Tool.GetComponentInChildren<Text>(gameObject, "PlayerName").text;
                if (name.Length <= 0) {
                    GlobalInit.Instance.showTip("名字不能为空!!!");
                    return;
                }
                PhotonNetwork.NickName = name;
                SceneManager.LoadScene("Game"); // 加载场景
            }
            else
            {
                GlobalInit.Instance.showTip("未勾选条款!!!");
            }
        }

        /// <summary>
        /// 关闭通告
        /// </summary>
        public void OnClick_NoteClose()
        {
            note.SetActive(false);
        }
    }
}