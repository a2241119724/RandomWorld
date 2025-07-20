using System.Collections;
using System.Collections.Generic;

namespace LAB2D
{
    public class BackpackModel : MVCModel
    {
        public BackpackModel() : base(ItemType.Weapon, ItemType.BackpackOther)
        {
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        /// <returns>是否有数据</returns>
        public override void LoadData()
        {
            Dictionary<ItemType, ArrayList> data = Tool.LoadDataByBinary<Dictionary<ItemType, ArrayList>>(GlobalData.ConfigFile.GetPath(GetType().Name));
            //Dictionary<ItemType, ArrayList> data = Tool.loadDataByJson<Dictionary<ItemType, ArrayList>>(GlobalData.ConfigFile.BackpackDataFilePath);
            if (data == null) return;
            itemDict = data;
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        public override void SaveData()
        {
            Tool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(GetType().Name), itemDict);
            //Tool.saveDataByJson<object>(GlobalData.ConfigFile.BackpackDataFilePath, itemDict);
        }
    }
}
