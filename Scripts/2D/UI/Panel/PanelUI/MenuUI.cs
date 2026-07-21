namespace LAB2D.UI.Panel.PanelUI
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 菜单UI
    /// </summary>
    public class MenuUI : MonoBehaviour
    {
        private Toggle toggle; // 条款勾选框
        private GameObject note; // 通知

        public void Awake()
        {
            GlobalInit.Instance.ShowTip("登录成功!!!");
        }

        public void Start()
        {
            PhotonNetwork.NickName = "aaa";
            this.toggle = LAB2D.Tool.Tool.GetComponentInChildren<Toggle>(this.gameObject, "Clause").GetComponent<Toggle>();
            this.note = this.transform.Find("Center/Note").gameObject;
            if (this.note == null)
            {
                AWorkerTask.LogProvider("note Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.gameObject, "Start").onClick.AddListener(this.OnClick_Start);
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.gameObject, "NoteClose").onClick.AddListener(this.OnClick_NoteClose);
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        private void OnClick_Start()
        {
            if (this.toggle.isOn)
            {
                string name = LAB2D.Tool.Tool.GetComponentInChildren<Text>(this.gameObject, "PlayerName").text;
                if (name.Length <= 0)
                {
                    GlobalInit.Instance.ShowTip("名字不能为空!!!");
                    return;
                }

                PhotonNetwork.NickName = name;
                LAB2D.Tool.Tool.LoadScene("Game");
            }
            else
            {
                GlobalInit.Instance.ShowTip("未勾选条款!!!");
            }
        }

        /// <summary>
        /// 关闭通告
        /// </summary>
        private void OnClick_NoteClose()
        {
            this.note.SetActive(false);
        }
    }
}