namespace LAB2D.MVC.Backpack.Model
{
    using LAB2D;
    using LAB2D.Item;
    using System.Collections.Generic;

    /// <summary>
    /// 背包数据模型
    /// </summary>
    public class BackpackModel : MVCModel
    {
        public BackpackModel()
            : base(AItem.ItemTypeEnum.Weapon, AItem.ItemTypeEnum.BackpackOther)
        {
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        public override void LoadData()
        {
            AsyncProgressUI.Instance.SetTip("加载背包数据...");
            Dictionary<AItem.ItemTypeEnum, List<AItem>> data = DataTool.LoadDataByBinary<Dictionary<AItem.ItemTypeEnum, List<AItem>>>(GlobalData.ConfigFile.GetPath(this.GetType().Name));

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

            // LAB2D.Tool.Tool.saveDataByJson<object>(GlobalData.ConfigFile.BackpackDataFilePath, itemDict);
        }
    }
}
