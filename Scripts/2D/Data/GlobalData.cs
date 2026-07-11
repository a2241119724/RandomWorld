namespace LAB2D.Data
{
    using LAB2D;
    using UnityEngine;

    /// <summary>
    /// 全局数据
    /// </summary>
    public static class GlobalData
    {
        /// <summary>
        /// 打包类型(未用)
        /// </summary>
        public static readonly PackageTypeEnum PackageType = PackageTypeEnum.PC;

        /// <summary>
        /// 是否是2D游戏(未用)
        /// </summary>
        public static bool Is2D = true;

        /// <summary>
        /// 是否是新游戏
        /// </summary>
        public static bool IsNew = true;

        /// <summary>
        /// 一天的实际时间
        /// </summary>
        public static float GameDayTime = 30 * 60.0f;

        /// <summary>
        /// 最大帧率
        /// </summary>
        public static int MaxFrame = 300;

        /// <summary>
        /// 用户数据文件路径
        /// </summary>
        public static class ConfigFile
        {
            /// <summary>
            /// 用户数据文件路径C:\Users\*\AppData\LocalLow\*\First_Version
            /// </summary>
            public static string UserDataFilePath = Application.persistentDataPath + "/user.json";

            /// <summary>
            /// 获取文件路径
            /// </summary>
            /// <param name="name">类名</param>
            /// <returns>文件路径</returns>
            public static string GetPath(string name)
            {
                return ArchiveManager.Instance.GetArchivePath(name);
            }
        }
    }
}
