namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine.UI;
    using static LAB2D.TileMap;

    /// <summary>
    /// 新游戏或者继续游戏面板
    /// </summary>
    public class NewOrContinuePanel : ABasePanel<NewOrContinuePanel>
    {
        public NewOrContinuePanel()
        {
            this.Name = "NewOrContinue";
            this.Init();
            Tool.GetComponentInChildren<Button>(this.Panel, "NewGame").onClick.AddListener(this.OnClick_NewGame);
            Tool.GetComponentInChildren<Button>(this.Panel, "ContinueGame").onClick.AddListener(this.OnClick_ContinueGame);
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
        }

        private void OnClick_NewGame()
        {
            this.Controller.Close();
            GlobalData.IsNew = true;
            this.Controller.Show(CreateDataPanel.Instance);
        }

        private void OnClick_ContinueGame()
        {
            TileMapData data = DataTool.LoadDataByBinary<TileMapData>(GlobalData.ConfigFile.GetPath(nameof(TileMap)));
            if (data == null)
            {
                GlobalInit.Instance.ShowTip("没有存档!!!");
                return;
            }

            this.Controller.Close();
            GlobalData.IsNew = false;
            this.Controller.Show(AsyncProgressPanel.Instance);

            // 注: 加载数据之前, 必须先实例化
            Lock.IsCompleteTileMap = true;
            List<Type> saveDatas = Tool.GetChildByParent<ASaveData>();
            List<Type> monoSaveDatas = Tool.GetChildByParent<AMonoSaveData>();
            AsyncProgressUI.Instance.SetTip("...");
            AsyncProgressUI.Instance.AddTotal(saveDatas.Count + monoSaveDatas.Count);
            foreach (Type type in saveDatas)
            {
                PropertyInfo propertyInfo = Tool.GetStaticPropertyByType(type, "Instance");
                if (propertyInfo == null)
                {
                    AsyncProgressUI.Instance.AddOneProcess();
                    continue;
                }

                // 实例化
                object obj = propertyInfo.GetValue(null, null);

                // AsyncProgressUI.Instance.SetTip(saveData.ToString());
                Tool.GetMethodByType(type, "LoadData")?.Invoke(obj, null);
                AsyncProgressUI.Instance.AddOneProcess();
            }

            foreach (Type type in monoSaveDatas)
            {
                PropertyInfo propertyInfo = Tool.GetStaticPropertyByType(type, "Instance");
                if (propertyInfo == null)
                {
                    AsyncProgressUI.Instance.AddOneProcess();
                    continue;
                }

                // 实例化
                object obj = propertyInfo.GetValue(null, null);

                // AsyncProgressUI.Instance.SetTip(saveData.ToString());
                Tool.GetMethodByType(type, "LoadData")?.Invoke(obj, null);
                AsyncProgressUI.Instance.AddOneProcess();
            }
        }
    }
}
