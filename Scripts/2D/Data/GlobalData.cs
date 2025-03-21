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
        public static readonly PackageType packageType = PackageType.PC;

        /// <summary>
        /// 是否是新游戏
        /// </summary>
        public static bool isNew = true;

        /// <summary>
        /// 一天的实际时间
        /// </summary>
        public static float dayTime = 30 * 60.0f;

        public static class ConfigFile
        {
            // C:\Users\*\AppData\LocalLow\*\First_Version
            public static string UserDataFilePath = Application.persistentDataPath + "/user.json";

            public static string getPath(string name) {
                return Application.persistentDataPath + "/" + name + ".lab";
            }
        }
    }

    public enum PackageType { 
        Android,
        PC
    }
}