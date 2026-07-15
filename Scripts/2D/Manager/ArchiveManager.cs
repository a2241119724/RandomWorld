namespace LAB2D.Manager
{
    using LAB2D;
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
        private const string ArchiveMetaFileName = "ArchiveMeta.json";
        private const int ArchiveSlotCount = 10;

        /// <summary>
        /// 存档展示名称最大长度.
        /// </summary>
        public const int ArchiveDisplayNameMaxLength = 16;
        private readonly int archiveCount = ArchiveSlotCount;
        private bool isLegacyArchiveChecked;

        [Serializable]
        private class ArchiveMetaData
        {
            public string DisplayName;
        }

        /// <summary>
        /// 当前存档索引.
        /// </summary>
        public int CurrentArchiveIndex { get; private set; }

        /// <summary>
        /// 当前存档目录名称.
        /// </summary>
        public string CurrentArchiveName => this.GetArchiveName(this.CurrentArchiveIndex);

        /// <summary>
        /// 当前存档展示名称.
        /// </summary>
        public string CurrentArchiveDisplayName => this.GetArchiveDisplayName(this.CurrentArchiveIndex);

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
                archiveNames.Add(this.GetArchiveDisplayName(i));
            }

            return archiveNames;
        }

        /// <summary>
        /// 切换当前存档.
        /// </summary>
        /// <param name="archiveIndex">存档索引.</param>
        public void SetCurrentArchive(int archiveIndex)
        {
            if (!this.IsArchiveIndexValid(archiveIndex))
            {
                return;
            }

            this.CurrentArchiveIndex = archiveIndex;
        }

        /// <summary>
        /// 获取指定存档槽展示名称.
        /// </summary>
        /// <param name="archiveIndex">存档索引.</param>
        /// <returns>存档展示名称.</returns>
        public string GetArchiveDisplayName(int archiveIndex)
        {
            if (!this.IsArchiveIndexValid(archiveIndex))
            {
                return string.Empty;
            }

            ArchiveMetaData metaData = this.LoadArchiveMetaData(archiveIndex);
            if (metaData == null || string.IsNullOrWhiteSpace(metaData.DisplayName))
            {
                return this.GetDefaultArchiveDisplayName(archiveIndex);
            }

            return metaData.DisplayName;
        }

        /// <summary>
        /// 修改指定存档槽展示名称.
        /// </summary>
        /// <param name="archiveIndex">存档索引.</param>
        /// <param name="displayName">新的展示名称.</param>
        /// <returns>是否修改成功.</returns>
        public bool SetArchiveDisplayName(int archiveIndex, string displayName)
        {
            if (!this.IsArchiveIndexValid(archiveIndex))
            {
                return false;
            }

            string normalizedDisplayName = this.NormalizeArchiveDisplayName(displayName);
            if (string.IsNullOrEmpty(normalizedDisplayName))
            {
                return false;
            }

            ArchiveMetaData metaData = new ()
            {
                DisplayName = normalizedDisplayName,
            };
            return this.SaveArchiveMetaData(archiveIndex, metaData);
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
            if (!this.IsArchiveIndexValid(archiveIndex))
            {
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
            this.InvokeSaveData(LAB2D.Tool.Tool.GetChildByParent<ASaveData>());
            this.InvokeSaveData(LAB2D.Tool.Tool.GetChildByParent<AMonoSaveData>());
            this.EnsureArchiveMetaData(this.CurrentArchiveIndex);
        }

        /// <summary>
        /// 删除指定存档槽.
        /// </summary>
        /// <param name="archiveIndex">存档索引.</param>
        /// <returns>是否删除成功.</returns>
        public bool DeleteArchive(int archiveIndex)
        {
            if (!this.IsArchiveIndexValid(archiveIndex))
            {
                return false;
            }

            string archiveDirectory = this.GetArchiveDirectory(archiveIndex);
            if (!Directory.Exists(archiveDirectory))
            {
                return false;
            }

            try
            {
                Directory.Delete(archiveDirectory, true);
                return true;
            }
            catch (Exception exception)
            {
                LogManager.Instance.Log(
                    $"delete archive failed: {archiveDirectory}\n{exception}",
                    LogManager.LogLevelEnum.Error);
                return false;
            }
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

            List<Type> saveDatas = LAB2D.Tool.Tool.GetChildByParent<ASaveData>();
            List<Type> monoSaveDatas = LAB2D.Tool.Tool.GetChildByParent<AMonoSaveData>();

            Core.ServiceLocator.Get<Core.MapInitCoordinator>().IsComplete = true;
            AsyncProgressUI.Instance.SetTip("...");
            AsyncProgressUI.Instance.AddTotal(saveDatas.Count + monoSaveDatas.Count);

            this.InvokeLoadData(monoSaveDatas);
            this.InvokeLoadData(saveDatas);
        }

        private string GetArchiveName(int archiveIndex)
        {
            return ArchivePrefix + (archiveIndex + 1);
        }

        private string GetDefaultArchiveDisplayName(int archiveIndex)
        {
            return $"存档 {archiveIndex + 1}";
        }

        private string GetArchiveDirectory(int archiveIndex)
        {
            return Path.Combine(Application.persistentDataPath, ArchiveRootFolderName, this.GetArchiveName(archiveIndex));
        }

        private string GetArchivePath(int archiveIndex, string name)
        {
            return Path.Combine(this.GetArchiveDirectory(archiveIndex), name + ".lab");
        }

        private string GetArchiveMetaPath(int archiveIndex)
        {
            return Path.Combine(this.GetArchiveDirectory(archiveIndex), ArchiveMetaFileName);
        }

        private bool IsArchiveIndexValid(int archiveIndex)
        {
            if (archiveIndex >= 0 && archiveIndex < this.archiveCount)
            {
                return true;
            }

            LogManager.Instance.Log($"archive index {archiveIndex} out of range", LogManager.LogLevelEnum.Error);
            return false;
        }

        private string NormalizeArchiveDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return string.Empty;
            }

            string normalizedDisplayName = displayName.Trim();
            if (normalizedDisplayName.Length > ArchiveDisplayNameMaxLength)
            {
                normalizedDisplayName = normalizedDisplayName.Substring(0, ArchiveDisplayNameMaxLength);
            }

            return normalizedDisplayName;
        }

        private ArchiveMetaData LoadArchiveMetaData(int archiveIndex)
        {
            string archiveMetaPath = this.GetArchiveMetaPath(archiveIndex);
            if (!File.Exists(archiveMetaPath))
            {
                return null;
            }

            try
            {
                return DataTool.LoadDataByJson<ArchiveMetaData>(archiveMetaPath);
            }
            catch (Exception exception)
            {
                LogManager.Instance.Log(
                    $"load archive meta failed: {archiveMetaPath}\n{exception}",
                    LogManager.LogLevelEnum.Error);
                return null;
            }
        }

        private bool SaveArchiveMetaData(int archiveIndex, ArchiveMetaData metaData)
        {
            string archiveMetaPath = this.GetArchiveMetaPath(archiveIndex);
            try
            {
                DataTool.SaveDataByJson(archiveMetaPath, metaData);
                return true;
            }
            catch (Exception exception)
            {
                LogManager.Instance.Log(
                    $"save archive meta failed: {archiveMetaPath}\n{exception}",
                    LogManager.LogLevelEnum.Error);
                return false;
            }
        }

        private void EnsureArchiveMetaData(int archiveIndex)
        {
            if (File.Exists(this.GetArchiveMetaPath(archiveIndex)))
            {
                return;
            }

            ArchiveMetaData metaData = new ()
            {
                DisplayName = this.GetDefaultArchiveDisplayName(archiveIndex),
            };
            this.SaveArchiveMetaData(archiveIndex, metaData);
        }

        private void InvokeSaveData(List<Type> types)
        {
            foreach (Type type in types)
            {
                if (!this.TryGetInstance(type, out object obj))
                {
                    continue;
                }

                LAB2D.Tool.Tool.GetMethodByType(type, nameof(ASaveData.SaveData))?.Invoke(obj, null);
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

                LAB2D.Tool.Tool.GetMethodByType(type, nameof(ASaveData.LoadData))?.Invoke(obj, null);
                AsyncProgressUI.Instance.AddOneProcess();
            }
        }

        private bool TryGetInstance(Type type, out object obj)
        {
            obj = null;
            PropertyInfo propertyInfo = LAB2D.Tool.Tool.GetStaticPropertyByType(type, "Instance");
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

            // 先验证旧数据能否被当前版本正确反序列化
            try
            {
                var data = DataTool.LoadDataByBinary<LAB2D.Map.TileMap.TileMapData>(legacyTileMapPath);
                if (data == null)
                {
                    Debug.LogWarning(
                        $"[ArchiveManager] Legacy archive data at '{legacyTileMapPath}' is incompatible " +
                        "with the current version. Migration skipped — the slot will be treated as empty. " +
                        "Delete the old .lab files manually if you want to reclaim disk space.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[ArchiveManager] Failed to read legacy archive data: {ex.Message}. " +
                    "Migration skipped.");
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

            Debug.Log($"[ArchiveManager] Migrated {legacyFiles.Length} legacy archive files to {targetDirectory}");
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
