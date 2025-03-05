using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    /// <summary>
    /// ���������ʾ������Ϣ
    /// </summary>
    public abstract class MVCInfoView : MonoBehaviour
    {
        private Text info; // ������Ϣ
        //private SelectAndShowEventSO selectAndShow;

        private void OnEnable()
        {
            //selectAndShow = Resources.Load<SelectAndShowEventSO>("SO/SelectAndShowEvent");
            ////�����¼�
            //selectAndShow.OnSelectAndRun += showInfo;
        }

        void Start()
        {
            //info = transform.Find("Background/Message").GetComponent<Text>();
            info = Tool.GetComponentInChildren<Text>(gameObject, "Message");
            info.text = "";
        }

        //private void OnDisable()
        //{
        //    // ɾ������(��Ҫ��)
        //    selectAndShow.OnSelectAndRun -= showInfo;
        //}

        public void showInfo(Item item)
        {
            if (item == null)
            {
                LogManager.Instance.log("item is null!!!", LogManager.LogLevel.Error);
                return;
            }
            info.text = item.ToString();
            // ��¼��������ĸ�����
        }
    }
}
