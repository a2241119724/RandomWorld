using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class BuildMenuPanel : BasePanel<BuildMenuPanel>
    {
        public SelectItemData Select { set; get; }

        public BuildMenuPanel()
        {
            Name = "BuildMenu";
            Select = new SelectItemData();
            setPanel();
            Tool.GetComponentInChildren<Button>(panel, "BackGame").onClick.AddListener(OnClick_BackGame);
            Tool.GetComponentInChildren<Button>(panel, "StartBuild").onClick.AddListener(OnClick_StartBuild);
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
            // 进入游戏主界面
            controller.show(ForegroundPanel.Instance);
        }

        public void OnClick_BackGame()
        {
            controller.close();
        }

        public void OnClick_StartBuild()
        {
            controller.close();
            // 关闭ForegroundPanel
            controller.close();
            // GameObject g = PrefabManager.Instance.getByAll(Select.itemData.itemName);
            controller.show(BuildGridPanel.Instance);
        }
    }
}

