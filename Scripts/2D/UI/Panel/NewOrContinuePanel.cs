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
            GlobalData.IsNew = true;
            controller.show(CreateDataPanel.Instance);
        }

        private void OnClick_ContinueGame()
        {
            TileMapData data = Tool.LoadDataByBinary<TileMapData>(GlobalData.ConfigFile.GetPath("TileMap"));
            if (data == null)
            {
                GlobalInit.Instance.ShowTip("没有存档!!!");
                return;
            }
            controller.close();
            GlobalData.IsNew = false;
            controller.show(AsyncProgressPanel.Instance);
            AsyncProgressUI.Instance.addTotal(ASaveData.Instances.Count + AMonoSaveData.Instances.Count);
            // 加载数据之前,线实例化
            PlayerManager.Instance.Init();
            foreach (ASaveData saveData in ASaveData.Instances)
            {
                if (saveData == null) continue;
                AsyncProgressUI.Instance.setTip(saveData.ToString());
                saveData.LoadData();
                AsyncProgressUI.Instance.addOneProcess();
            }
            foreach (AMonoSaveData saveData in AMonoSaveData.Instances)
            {
                if (saveData == null) continue;
                AsyncProgressUI.Instance.setTip(saveData.ToString());
                saveData.LoadData();
                AsyncProgressUI.Instance.addOneProcess();
            }
        }
    }
}
