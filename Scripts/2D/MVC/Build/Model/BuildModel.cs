using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

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
        public override void loadData()
        {
            Dictionary<ItemType, ArrayList> data = Tool.loadDataByBinary<Dictionary<ItemType, ArrayList>>(GlobalData.ConfigFile.getPath(this.GetType().Name));
            //Dictionary<BuildType, ArrayList> data = Tool.loadDataByJson<Dictionary<BuildType, ArrayList>>(GlobalData.ConfigFile.BuildDataFilePath);
            if (data == null) return;
            itemDict = data;
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        public override void saveData()
        {
            Tool.saveDataByBinary(GlobalData.ConfigFile.getPath(this.GetType().Name), itemDict);
            //Tool.saveDataByJson(GlobalData.ConfigFile.BuildDataFilePath, itemDict);
        }
    }
}
