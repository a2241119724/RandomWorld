using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static LAB2D.TileMap;

namespace LAB2D
{
    public class NewOrContinuePanel : BasePanel<NewOrContinuePanel>
    {
        public NewOrContinuePanel()
        {
            Name = "NewOrContinue";
            setPanel();
            Tool.GetComponentInChildren<Button>(panel, "NewGame").onClick.AddListener(OnClick_NewGame);
            Tool.GetComponentInChildren<Button>(panel, "ContinueGame").onClick.AddListener(OnClick_ContinueGame);
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        private void OnClick_NewGame()
        {
            controller.close();
            GlobalData.isNew = true;
            controller.show(CreateDataPanel.Instance);
        }

        private void OnClick_ContinueGame()
        {
            TileMapData data = Tool.loadDataByBinary<TileMapData>(GlobalData.ConfigFile.getPath("TileMap"));
            if(data == null)
            {
                GlobalInit.Instance.showTip("û�д浵!!!");
                return;
            }
            controller.close();
            GlobalData.isNew = false;
            controller.show(AsyncProgressPanel.Instance);
            AsyncProgressUI.Instance.addTotal(ASaveData.Instances.Count + AMonoSaveData.Instances.Count);
            // ��������֮ǰ,��ʵ����
            PlayerManager.Instance.init();
            foreach (ASaveData saveData in ASaveData.Instances)
            {
                if (saveData == null) continue;
                AsyncProgressUI.Instance.setTip(saveData.ToString());
                saveData.loadData();
                AsyncProgressUI.Instance.addOneProcess();
            }
            foreach (AMonoSaveData saveData in AMonoSaveData.Instances)
            {
                if (saveData == null) continue;
                AsyncProgressUI.Instance.setTip(saveData.ToString());
                saveData.loadData();
                AsyncProgressUI.Instance.addOneProcess();
            }
        }
    }
}
