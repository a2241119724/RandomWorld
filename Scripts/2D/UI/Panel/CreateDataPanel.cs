using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class CreateDataPanel : BasePanel<CreateDataPanel>
    {
        private int height = 548; // 地图纵向长度
        private int width = 548; // 地图横向长度
        private int maxEnemyCount = 548; // 敌人数量

        public CreateDataPanel()
        {
            Name = "CreateData";
            setPanel();
            Transform g1 = Tool.GetComponentInChildren<Transform>(panel, "MapHeight");
            Slider s1 = g1.Find("Bar").GetComponent<Slider>();
            height = (int)s1.value;
            g1.Find("Bar").GetComponent<Slider>().onValueChanged.AddListener(delegate (float value)
            {
                height = (int)Mathf.Floor(g1.Find("Bar").GetComponent<Slider>().value);
                g1.Find("Count").GetComponent<Text>().text = height.ToString();
            });
            Transform g2 = Tool.GetComponentInChildren<Transform>(panel, "MapWidth");
            Slider s2 = g2.Find("Bar").GetComponent<Slider>();
            width = (int)s2.value;
            g2.Find("Bar").GetComponent<Slider>().onValueChanged.AddListener((value) =>
            {
                width = (int)Mathf.Floor(g2.Find("Bar").GetComponent<Slider>().value);
                g2.Find("Count").GetComponent<Text>().text = width.ToString();
            });
            Transform g3 = Tool.GetComponentInChildren<Transform>(panel, "EnemyCount");
            Slider s3 = g3.Find("Bar").GetComponent<Slider>();
            maxEnemyCount = (int)s3.value;
            g3.Find("Bar").GetComponent<Slider>().onValueChanged.AddListener((value) =>
            {
                maxEnemyCount = (int)Mathf.Floor(g3.Find("Bar").GetComponent<Slider>().value);
                g3.Find("Count").GetComponent<Text>().text = maxEnemyCount.ToString();
            });
            Tool.GetComponentInChildren<Button>(panel, "StartCreate").onClick.AddListener(Onclick_StartCreate);
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
            controller.show(AsyncProgressPanel.Instance);
        }

        /// <summary>
        /// 创建地图,敌人,玩家,道具等物体
        /// </summary>
        private void Onclick_StartCreate() {
            if (PhotonNetwork.NetworkClientState != ClientState.Joined 
                && NetworkConnect.Instance.IsOnline)
            {
                GlobalInit.Instance.showTip("请稍后再试");
                return;
            }
            controller.close();
            // TileMap
            TileMap.Instance.setProgress(height, width);
            Coroutine coroutine = TileMap.Instance.StartCoroutine(TileMap.Instance.create());
            // ResourceMap
            ResourceMap.Instance.setProgress();
            ResourceMap.Instance.StartCoroutine(ResourceMap.Instance.genResource(coroutine));
            // EnemyManager
            EnemyManager.Instance.MaxEnemyCount = maxEnemyCount;
            AsyncProgressUI.Instance.complete += () => {
                PlayerManager.Instance.create();
            };
        }
    }
}
