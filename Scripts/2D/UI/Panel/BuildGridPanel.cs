using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class BuildGridPanel : BasePanel<BuildGridPanel>
    {
        public BuildGridPanel()
        {
            Name = "BuildGrid";
            setPanel();
        }

        public override void OnEnter()
        {
            base.OnEnter();
            // 触发鼠标事件
            //panel.transform.SetParent(PanelController.Instance.Parent);
        }

        public override void OnExit()
        {
            base.OnExit();
            controller.show(ForegroundPanel.Instance);
            // 视角变换会导致建筑方格大小不正确
            //panel.transform.SetParent(GameObject.FindGameObjectWithTag(ResourceConstant.CHARACTER_TAG_ROOT).transform);
        }
    }
}
