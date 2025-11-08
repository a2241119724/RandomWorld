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
            : base(AItem.ItemType.Weapon, AItem.ItemType.BackpackOther)
        {
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        public override void LoadData()
        {
            AsyncProgressUI.Instance.SetTip("加载背包数据...");
            Dictionary<AItem.ItemType, ArrayList> data = DataTool.LoadDataByBinary<Dictionary<AItem.ItemType, ArrayList>>(GlobalData.ConfigFile.GetPath(this.GetType().Name));

            // Dictionary<Item.ItemType, ArrayList> data = Tool.loadDataByJson<Dictionary<Item.ItemType, ArrayList>>(GlobalData.ConfigFile.BackpackDataFilePath);
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

            // Tool.saveDataByJson<object>(GlobalData.ConfigFile.BackpackDataFilePath, itemDict);
        }
    }
}
