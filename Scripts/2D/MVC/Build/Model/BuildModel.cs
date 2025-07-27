namespace LAB2D
{
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// 建造数据模型
    /// </summary>
    public class BuildModel : MVCModel
    {
        public BuildModel()
            : base(ItemType.Room, ItemType.BuildOther)
        {
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            Dictionary<ItemType, ArrayList> data = Tool.LoadDataByBinary<Dictionary<ItemType, ArrayList>>(GlobalData.ConfigFile.GetPath(this.GetType().Name));

            // Dictionary<BuildType, ArrayList> data = Tool.loadDataByJson<Dictionary<BuildType, ArrayList>>(GlobalData.ConfigFile.BuildDataFilePath);
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
            Tool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), this.ItemDict);

            // Tool.saveDataByJson(GlobalData.ConfigFile.BuildDataFilePath, itemDict);
        }
    }
}
