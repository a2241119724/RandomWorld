namespace LAB2D.Data
{
    using LAB2D;
    using LAB2D.Enum;
    using UnityEngine;

    /// <summary>
    /// 全局数据
    /// </summary>
    public static class GlobalData
    {
        /// <summary>
        /// 打包类型(未用)
        /// </summary>
        public static readonly PackageType CurrentPackageType = PackageType.PC;

        /// <summary>
        /// 是否是2D游戏(未用)
        /// </summary>
        public static bool Is2D = true;

        /// <summary>
        /// 是否是新游戏
        /// </summary>
        public static bool IsNew = true;

        /// <summary>
        /// 一天的实际时间（秒）。波次挂日节奏：一天 600s = 白天约 360s 经营 + 夜晚约 240s 防守；
        /// Worker 任务时长（WorkerTaskTimeConfig）按此百分比自动等比缩放。
        /// </summary>
        public static float GameDayTime = 10 * 60.0f;

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
                return Core.ServiceLocator.Get<ArchiveManager>().GetArchivePath(name);
            }
        }
    }
}
