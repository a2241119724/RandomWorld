namespace LAB2D.MVC.Build.Model
{
    using LAB2D;
    using System.Collections.Generic;

    /// <summary>
    /// 建造数据模型
    /// </summary>
    public class BuildModel : MVCModel
    {
        public BuildModel()
            : base(AItem.ItemTypeEnum.Room, AItem.ItemTypeEnum.BuildOther)
        {
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            AsyncProgressUI.Instance.SetTip("加载建造数据...");
            Dictionary<AItem.ItemTypeEnum, List<AItem>> data = DataTool.LoadDataByBinary<Dictionary<AItem.ItemTypeEnum, List<AItem>>>(GlobalData.ConfigFile.GetPath(this.GetType().Name));

            // Dictionary<BuildType, ArrayList> data = LAB2D.Tool.Tool.loadDataByJson<Dictionary<BuildType, ArrayList>>(GlobalData.ConfigFile.BuildDataFilePath);
            if (data == null)
            {
                return;
            }

            this.ItemDict = data;
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        public override void SaveData()
        {
            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), this.ItemDict);

            // LAB2D.Tool.Tool.saveDataByJson(GlobalData.ConfigFile.BuildDataFilePath, itemDict);
        }
    }
}
