using System.Collections;
using System.Collections.Generic;

namespace LAB2D
{
    public class BuildModel : MVCModel
    {
        public BuildModel() : base(ItemType.Room, ItemType.BuildOther)
        {
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        /// <returns>是否有数据</returns>
        public override void LoadData()
        {
            Dictionary<ItemType, ArrayList> data = Tool.LoadDataByBinary<Dictionary<ItemType, ArrayList>>(GlobalData.ConfigFile.GetPath(GetType().Name));
            //Dictionary<BuildType, ArrayList> data = Tool.loadDataByJson<Dictionary<BuildType, ArrayList>>(GlobalData.ConfigFile.BuildDataFilePath);
            if (data == null) return;
            itemDict = data;
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        public override void SaveData()
        {
            Tool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(GetType().Name), itemDict);
            //Tool.saveDataByJson(GlobalData.ConfigFile.BuildDataFilePath, itemDict);
        }
    }
}
