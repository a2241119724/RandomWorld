using UnityEngine;

namespace LAB2D
{
    public static class GlobalData
    {
        /// <summary>
        /// �Ƿ���2D��Ϸ(δ��)
        /// </summary>
        public static bool is2D = true;

        /// <summary>
        /// �������(δ��)
        /// </summary>
        public static readonly PackageType packaageType = PackageType.PC;

        /// <summary>
        /// �Ƿ�������Ϸ
        /// </summary>
        public static bool isNew = true;

        /// <summary>
        /// Ѱ·ʱ�Ƿ����б����
        /// </summary>
        public static bool isTilt = false; // �Ƿ����б����

        public static class Lock {
            public static class SeekLock
            {
                public static bool seekLock = false;
                /// <summary>
                /// ӵ����
                /// </summary>
                public static Worker owner;
            }
        }

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