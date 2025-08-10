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
            AsyncProgressUI.Instance.SetTip("...");
            AsyncProgressUI.Instance.AddTotal(ASaveData.Instances.Count + AMonoSaveData.Instances.Count);

            // 注: 加载数据之前, 必须先实例化
            ResourceConstant.IsCompleteTileMap = true;
            List<Type> types = Tool.GetChildByParent<ASaveData>();
            foreach (Type type in types)
            {
                PropertyInfo propertyInfo = Tool.GetStaticPropertyByType(type, "Instance");
                if (propertyInfo == null)
                {
                    continue;
                }

                // 实例化
                object obj = propertyInfo.GetValue(null, null);

                // AsyncProgressUI.Instance.SetTip(saveData.ToString());
                Tool.GetMethodByType(type, "LoadData")?.Invoke(obj, null);
                AsyncProgressUI.Instance.AddOneProcess();
            }

            types = Tool.GetChildByParent<AMonoSaveData>();
            foreach (Type type in types)
            {
                PropertyInfo propertyInfo = Tool.GetStaticPropertyByType(type, "Instance");
                if (propertyInfo == null)
                {
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
