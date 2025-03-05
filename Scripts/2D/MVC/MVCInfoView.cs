using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    /// <summary>
    /// 点击道具显示道具信息
    /// </summary>
    public abstract class MVCInfoView : MonoBehaviour
    {
        private Text info; // 道具信息
        //private SelectAndShowEventSO selectAndShow;

        private void OnEnable()
        {
            //selectAndShow = Resources.Load<SelectAndShowEventSO>("SO/SelectAndShowEvent");
            ////订阅事件
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
        //    // 删除订阅(必要的)
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
            // 记录点击的是哪个道具
        }
    }
}
