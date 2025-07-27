namespace LAB2D
{
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// 背包数据模型
    /// </summary>
    public class BackpackModel : MVCModel
    {
        public BackpackModel()
            : base(ItemType.Weapon, ItemType.BackpackOther)
        {
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        public override void LoadData()
        {
            Dictionary<ItemType, ArrayList> data = Tool.LoadDataByBinary<Dictionary<ItemType, ArrayList>>(GlobalData.ConfigFile.GetPath(this.GetType().Name));

            // Dictionary<ItemType, ArrayList> data = Tool.loadDataByJson<Dictionary<ItemType, ArrayList>>(GlobalData.ConfigFile.BackpackDataFilePath);
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

            // Tool.saveDataByJson<object>(GlobalData.ConfigFile.BackpackDataFilePath, itemDict);
        }
    }
}
