using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class CreateDataPanel : BasePanel<CreateDataPanel>
    {
        private int length = 548; // ��ͼ���򳤶�
        private int width = 548; // ��ͼ���򳤶�
        private int maxEnemyCount = 548; // ��������

        public CreateDataPanel()
        {
            Name = "CreateData";
            setPanel();
            Transform g1 = Tool.GetComponentInChildren<Transform>(panel, "MapLength");
            Slider s1 = g1.Find("Bar").GetComponent<Slider>();
            length = (int)s1.value;
            g1.Find("Bar").GetComponent<Slider>().onValueChanged.AddListener(delegate (float value)
            {
                length = (int)Mathf.Floor(g1.Find("Bar").GetComponent<Slider>().value);
                g1.Find("Count").GetComponent<Text>().text = length.ToString();
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
            Tool.GetComponentInChildren<Button>(panel, "StartCreate").onClick.AddListener(StartCreate);
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
            // ������������ؽ���
            controller.show(AsyncProgressPanel.Instance);
        }

        /// <summary>
        /// ������ͼ,����,���,���ߵ�����
        /// </summary>
        private void StartCreate() {
            if (PhotonNetwork.NetworkClientState != ClientState.Joined)
            {
                GlobalInit.Instance.showTip("���Ժ�����");
                return;
            }
            GameObject.FindGameObjectWithTag("MapRoot").GetComponent<TileMap>().enabled = true;
            TileMap.Instance.Height = length;
            TileMap.Instance.Width = width;
            // map
            EnemyManager.Instance.MaxEnemyCount = maxEnemyCount;
            controller.close();
        }
    }
}
