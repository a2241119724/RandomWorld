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
            // ��������¼�
            //panel.transform.SetParent(PanelController.Instance.Parent);
        }

        public override void OnExit()
        {
            base.OnExit();
            controller.show(ForegroundPanel.Instance);
            // �ӽǱ任�ᵼ�½��������С����ȷ
            //panel.transform.SetParent(GameObject.FindGameObjectWithTag(ResourceConstant.CHARACTER_TAG_ROOT).transform);
        }
    }
}
