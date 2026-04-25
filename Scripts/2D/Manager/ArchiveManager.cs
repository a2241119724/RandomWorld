namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using UnityEngine;

    /// <summary>
    /// 存档管理.
    /// 当前仅实现一个存档槽位, 预留多个存档的切换能力.
    /// </summary>
    public class ArchiveManager : Singleton<ArchiveManager>
    {
        private const string ArchiveRootFolderName = "Archive";
        private const string ArchivePrefix = "Archive_";
        private const int ArchiveSlotCount = 10;
        private readonly int archiveCount = ArchiveSlotCount;
        private bool isLegacyArchiveChecked;

        /// <summary>
        /// 当前存档索引.
        /// </summary>
        public int CurrentArchiveIndex { get; private set; }

        /// <summary>
        /// 当前存档名称.
        /// </summary>
        public string CurrentArchiveName => this.GetArchiveName(this.CurrentArchiveIndex);

        /// <summary>
        /// 当前存档目录.
        /// </summary>
        public string CurrentArchiveDirectory => this.GetArchiveDirectory(this.CurrentArchiveIndex);

        /// <summary>
        /// 存档槽位数量.
        /// </summary>
        public int ArchiveCount => this.archiveCount;

        /// <summary>
        /// 获取当前已实现的存档槽位名称.
        /// </summary>
        /// <returns>存档名称列表.</returns>
        public List<string> GetArchives()
        {
            List<string> archiveNames = new ();
            for (int i = 0; i < this.archiveCount; i++)
            {
                archiveNames.Add(this.GetArchiveName(i));
            }

            return archiveNames;
        }

        /// <summary>
        /// 切换当前存档.
        /// </summary>
        /// <param name="archiveIndex">存档索引.</param>
        public void SetCurrentArchive(int archiveIndex)
        {
            if (archiveIndex < 0 || archiveIndex >= this.archiveCount)
            {
                LogManager.Instance.Log($"archive index {archiveIndex} out of range", LogManager.LogLevelEnum.Error);
                return;
            }

            this.CurrentArchiveIndex = archiveIndex;
        }

        /// <summary>
        /// 获取当前存档下的数据文件路径.
        /// </summary>
        /// <param name="name">数据名称.</param>
        /// <returns>文件路径.</returns>
        public string GetArchivePath(string name)
        {
            return Path.Combine(this.CurrentArchiveDirectory, name + ".lab");
        }

        /// <summary>
        /// 当前存档是否存在.
        /// </summary>
        /// <returns>是否存在.</returns>
        public bool HasCurrentArchive()
        {
            if (this.CurrentArchiveIndex == 0)
            {
                this.TryMigrateLegacyArchive();
            }

            return File.Exists(this.GetArchivePath(nameof(TileMap)));
        }

        /// <summary>
        /// 指定存档槽是否存在.
        /// </summary>
        /// <param name="archiveIndex">存档索引.</param>
        /// <returns>是否存在.</returns>
        public bool HasArchive(int archiveIndex)
        {
            if (archiveIndex < 0 || archiveIndex >= this.archiveCount)
            {
                LogManager.Instance.Log($"archive index {archiveIndex} out of range", LogManager.LogLevelEnum.Error);
                return false;
            }

            if (archiveIndex == 0)
            {
                this.TryMigrateLegacyArchive();
            }

            return File.Exists(this.GetArchivePath(archiveIndex, nameof(TileMap)));
        }

        /// <summary>
        /// 保存当前存档.
        /// </summary>
        public void SaveCurrentArchive()
        {
            this.InvokeSaveData(Tool.GetChildByParent<ASaveData>());
            this.InvokeSaveData(Tool.GetChildByParent<AMonoSaveData>());
        }

        /// <summary>
        /// 加载当前存档.
        /// </summary>
        public void LoadCurrentArchive()
        {
            if (this.CurrentArchiveIndex == 0)
            {
                this.TryMigrateLegacyArchive();
            }

            List<Type> saveDatas = Tool.GetChildByParent<ASaveData>();
            List<Type> monoSaveDatas = Tool.GetChildByParent<AMonoSaveData>();

            Lock.IsCompleteTileMap = true;
            AsyncProgressUI.Instance.SetTip("...");
            AsyncProgressUI.Instance.AddTotal(saveDatas.Count + monoSaveDatas.Count);

            this.InvokeLoadData(monoSaveDatas);
            this.InvokeLoadData(saveDatas);
        }

        private string GetArchiveName(int archiveIndex)
        {
            return ArchivePrefix + (archiveIndex + 1);
        }

        private string GetArchiveDirectory(int archiveIndex)
        {
            return Path.Combine(Application.persistentDataPath, ArchiveRootFolderName, this.GetArchiveName(archiveIndex));
        }

        private string GetArchivePath(int archiveIndex, string name)
        {
            return Path.Combine(this.GetArchiveDirectory(archiveIndex), name + ".lab");
        }

        private void InvokeSaveData(List<Type> types)
        {
            foreach (Type type in types)
            {
                if (!this.TryGetInstance(type, out object obj))
                {
                    continue;
                }

                Tool.GetMethodByType(type, nameof(ASaveData.SaveData))?.Invoke(obj, null);
            }
        }

        private void InvokeLoadData(List<Type> types)
        {
            foreach (Type type in types)
            {
                if (!this.TryGetInstance(type, out object obj))
                {
                    AsyncProgressUI.Instance.AddOneProcess();
                    continue;
                }

                Tool.GetMethodByType(type, nameof(ASaveData.LoadData))?.Invoke(obj, null);
                AsyncProgressUI.Instance.AddOneProcess();
            }
        }

        private bool TryGetInstance(Type type, out object obj)
        {
            obj = null;
            PropertyInfo propertyInfo = Tool.GetStaticPropertyByType(type, "Instance");
            if (propertyInfo == null)
            {
                return false;
            }

            obj = propertyInfo.GetValue(null, null);
            return obj != null;
        }

        private void TryMigrateLegacyArchive()
        {
            if (this.isLegacyArchiveChecked)
            {
                return;
            }

            this.isLegacyArchiveChecked = true;
            const int legacyArchiveIndex = 0;
            if (this.HasArchiveFiles(legacyArchiveIndex))
            {
                return;
            }

            string legacyTileMapPath = Path.Combine(Application.persistentDataPath, nameof(TileMap) + ".lab");
            if (!File.Exists(legacyTileMapPath))
            {
                return;
            }

            string targetDirectory = this.GetArchiveDirectory(legacyArchiveIndex);
            Directory.CreateDirectory(targetDirectory);
            string[] legacyFiles = Directory.GetFiles(Application.persistentDataPath, "*.lab");
            foreach (string legacyFile in legacyFiles)
            {
                string targetFile = Path.Combine(targetDirectory, Path.GetFileName(legacyFile));
                if (File.Exists(targetFile))
                {
                    continue;
                }

                File.Copy(legacyFile, targetFile);
            }
        }

        private bool HasArchiveFiles(int archiveIndex)
        {
            string archiveDirectory = this.GetArchiveDirectory(archiveIndex);
            if (!Directory.Exists(archiveDirectory))
            {
                return false;
            }

            return File.Exists(this.GetArchivePath(archiveIndex, nameof(TileMap)));
        }
    }
}
