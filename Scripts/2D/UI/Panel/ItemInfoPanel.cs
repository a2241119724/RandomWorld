using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class ItemInfoPanel : BasePanel<ItemInfoPanel>
    {
        private Text textUI;

        public ItemInfoPanel()
        {
            Name = "ItemInfo";
            setPanel();
            textUI = Tool.GetComponentInChildren<Text>(panel, "Text");
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void OnPause()
        {
            base.OnPause();
            Time.timeScale = 0; // ÔÝÍ£
        }

        public override void OnRun()
        {
            base.OnRun();
            Time.timeScale = ForegroundPanel.Instance.TimeScale;
        }

        public void setItemInfo(string text) {
            textUI.text = text;
        }
    }
}
