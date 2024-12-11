using UnityEngine;

namespace LAB2D
{
    public static class GlobalData
    {
        /// <summary>
        /// 是否是2D游戏(未用)
        /// </summary>
        public static bool is2D = true;

        /// <summary>
        /// 打包类型(未用)
        /// </summary>
        public static readonly PackageType packaageType = PackageType.PC;

        /// <summary>
        /// 寻路时是否可以斜着走
        /// </summary>
        public static bool isTilt = false; // 是否可以斜着走

        public static class Lock {
            public static class SeekLock
            {
                public static bool seekLock = false;
                /// <summary>
                /// 拥有者
                /// </summary>
                public static Worker owner;
            }
        }

        public static class ConfigFile
        {
            // C:\Users\*\AppData\LocalLow\*\First_Version
            public static string UserDataFilePath = Application.persistentDataPath + "/user.json";
            public static string BackpackDataFilePath = Application.persistentDataPath + "/backpack.lab";
            public static string BuildDataFilePath = Application.persistentDataPath + "/build.lab";
        }
    }

    public enum PackageType { 
        Android,
        PC
    }
}