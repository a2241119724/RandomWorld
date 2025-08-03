namespace LAB2D
{
    using Photon.Pun;
    using Photon.Realtime;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 创建地图面板
    /// </summary>
    public class CreateDataPanel : BasePanel<CreateDataPanel>
    {
        private int height = 548; // 地图纵向长度
        private int width = 548; // 地图横向长度
        private int maxEnemyCount = 548; // 敌人数量

        public CreateDataPanel()
        {
            this.Name = "CreateData";
            this.Init();
            Transform g1 = Tool.GetComponentInChildren<Transform>(this.Panel, "MapHeight");
            Slider s1 = g1.Find("Bar").GetComponent<Slider>();
            this.height = (int)s1.value;
            g1.Find("Bar").GetComponent<Slider>().onValueChanged.AddListener(delegate(float value)
            {
                this.height = (int)Mathf.Floor(g1.Find("Bar").GetComponent<Slider>().value);
                g1.Find("Count").GetComponent<Text>().text = this.height.ToString();
            });
            Transform g2 = Tool.GetComponentInChildren<Transform>(this.Panel, "MapWidth");
            Slider s2 = g2.Find("Bar").GetComponent<Slider>();
            this.width = (int)s2.value;
            g2.Find("Bar").GetComponent<Slider>().onValueChanged.AddListener((value) =>
            {
                this.width = (int)Mathf.Floor(g2.Find("Bar").GetComponent<Slider>().value);
                g2.Find("Count").GetComponent<Text>().text = this.width.ToString();
            });
            Transform g3 = Tool.GetComponentInChildren<Transform>(this.Panel, "EnemyCount");
            Slider s3 = g3.Find("Bar").GetComponent<Slider>();
            this.maxEnemyCount = (int)s3.value;
            g3.Find("Bar").GetComponent<Slider>().onValueChanged.AddListener((value) =>
            {
                this.maxEnemyCount = (int)Mathf.Floor(g3.Find("Bar").GetComponent<Slider>().value);
                g3.Find("Count").GetComponent<Text>().text = this.maxEnemyCount.ToString();
            });
            Tool.GetComponentInChildren<Button>(this.Panel, "StartCreate").onClick.AddListener(this.Onclick_StartCreate);
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
            this.Controller.Show(AsyncProgressPanel.Instance);
        }

        /// <summary>
        /// 创建地图,敌人,玩家,道具等物体
        /// </summary>
        private void Onclick_StartCreate()
        {
            if (PhotonNetwork.NetworkClientState != ClientState.Joined
                && NetworkConnect.Instance.IsOnline)
            {
                GlobalInit.Instance.ShowTip("请稍后再试");
                return;
            }

            this.Controller.Close();

            // TileMap
            TileMap.Instance.SetProgress(this.height, this.width);
            Coroutine coroutine = TileMap.Instance.StartCoroutine(TileMap.Instance.Create());

            // ResourceMap
            ResourceMap.Instance.SetProgress();
            ResourceMap.Instance.StartCoroutine(ResourceMap.Instance.GenResource(coroutine));

            // EnemyManager
            EnemyManager.Instance.MaxEnemyCount = this.maxEnemyCount;
            AsyncProgressUI.Instance.Complete += () =>
            {
                PlayerManager.Instance.Create();
            };
        }
    }
}
