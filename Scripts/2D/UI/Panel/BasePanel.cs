using Photon.Pun;
using UnityEngine;

namespace LAB2D
{
    public abstract class BasePanel<BP> : Singleton<BP>, IBasePanel where BP : new()
    {
        public GameObject panel; // 面板物体
        /// <summary>
        /// 通过Name获取对应的GameObject Panel
        /// </summary>
        public string Name { get; set; } = "";
        protected PanelController controller; // 切换和关闭面板

        public BasePanel()
        {
            controller = PanelController.Instance;
        }

        public void setPanel(string root = ResourceConstant.UI_TAG_ROOT)
        {
            Transform t = GameObject.FindGameObjectWithTag(root).transform.Find(Name);
            if (t == null)
            {
                panel = Object.Instantiate(ResourcesManager.Instance.getPrefab(Name), PanelController.Instance.Parent, false);
            }
            else
            {
                panel = t.gameObject;
            }
            panel.name = Name;
            panel.SetActive(false);
        }

        public virtual void OnEnter() {
            Debug.Log(PhotonNetwork.NetworkClientState);
            if (panel == null) return;
            panel.SetActive(true);
        }
        public virtual void OnPause() { }
        public virtual void OnRun() { }
        public virtual void OnExit() {
            panel.SetActive(false);
        }
    }

    public interface IBasePanel {
        void OnEnter();
        void OnPause();
        void OnRun(); // 暂停后开启为运行
        void OnExit();
    }
}