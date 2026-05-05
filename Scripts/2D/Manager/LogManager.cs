namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;

    /// <summary>
    /// 日志管理.
    /// </summary>
    public class LogManager : Singleton<LogManager>
    {
        private readonly object syncRoot = new object();
        private readonly string logPath = Path.Combine(Application.persistentDataPath, "game.log");
        private readonly List<string> logs = new List<string>();
        private readonly int maxLogCount = 200;
        private LogLevelEnum minLogLevel = LogLevelEnum.Info; // 最小的日志级别.
        private bool isSave = true;
        private bool fileAvailable = true;
        private long index = 0;

        public LogManager()
        {
            this.PrepareLogFile(true);
            Application.quitting += this.Flush;
        }

        /// <summary>
        /// 日志级别.
        /// </summary>
        public enum LogLevelEnum
        {
            /// <summary>
            /// Trace.
            /// </summary>
            Trace = 0,

            /// <summary>
            /// Debug.
            /// </summary>
            Debug = 1,

            /// <summary>
            /// Info.
            /// </summary>
            Info = 2,

            /// <summary>
            /// Warning.
            /// </summary>
            Warning = 3,

            /// <summary>
            /// Error.
            /// </summary>
            Error = 4,

            /// <summary>
            /// Fatal.
            /// </summary>
            Fatal = 5,

            /// <summary>
            /// Off.
            /// </summary>
            Off = 6,
        }

        /// <summary>
        /// 记录日志.
        /// </summary>
        /// <param name="message">日志内容.</param>
        /// <param name="level">日志级别.</param>
        public void Log(string message, LogLevelEnum level = LogLevelEnum.Trace)
        {
            if (level == LogLevelEnum.Off || level < this.minLogLevel)
            {
                return;
            }

            string logMessage;
            List<string> pendingLogs = null;
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            lock (this.syncRoot)
            {
                logMessage = $"{timestamp} [{level}] {this.index++} {message ?? string.Empty}";
                this.logs.Add(logMessage);
                if (this.logs.Count >= this.maxLogCount)
                {
                    pendingLogs = this.TakeLogsLocked();
                }
            }

            this.WriteToUnityConsole(logMessage, level);
            this.SaveLog(this.logPath, pendingLogs);
        }

        /// <summary>
        /// 设置最小日志级别.
        /// </summary>
        /// <param name="level">最小日志级别.</param>
        public void SetMinLogLevel(LogLevelEnum level)
        {
            this.minLogLevel = level;
        }

        /// <summary>
        /// 设置是否写入文件.
        /// </summary>
        /// <param name="enabled">是否写入文件.</param>
        public void SetFileSaveEnabled(bool enabled)
        {
            if (this.isSave == enabled)
            {
                return;
            }

            if (!enabled)
            {
                this.Flush();
            }

            this.isSave = enabled;
            if (this.isSave)
            {
                this.PrepareLogFile(false);
            }
        }

        /// <summary>
        /// 将缓存中的日志立即写入文件.
        /// </summary>
        public void Flush()
        {
            List<string> pendingLogs;
            lock (this.syncRoot)
            {
                pendingLogs = this.TakeLogsLocked();
            }

            this.SaveLog(this.logPath, pendingLogs);
        }

        private void SaveLog(string path, List<string> logs)
        {
            if (!this.isSave || !this.fileAvailable || logs == null || logs.Count == 0)
            {
                return;
            }

            try
            {
                File.AppendAllText(path, string.Join(Environment.NewLine, logs) + Environment.NewLine);
            }
            catch (Exception exception)
            {
                this.fileAvailable = false;
                Debug.LogWarning($"[LogManager] 写入日志文件失败，已暂停文件日志: {exception.Message}");
            }
        }

        private List<string> TakeLogsLocked()
        {
            if (this.logs.Count == 0)
            {
                return null;
            }

            List<string> pendingLogs = new List<string>(this.logs);
            this.logs.Clear();
            return pendingLogs;
        }

        private void PrepareLogFile(bool clear)
        {
            if (!this.isSave)
            {
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(this.logPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (clear)
                {
                    File.WriteAllText(this.logPath, string.Empty);
                }
                else if (!File.Exists(this.logPath))
                {
                    File.WriteAllText(this.logPath, string.Empty);
                }

                this.fileAvailable = true;
            }
            catch (Exception exception)
            {
                this.fileAvailable = false;
                Debug.LogWarning($"[LogManager] 初始化日志文件失败，已暂停文件日志: {exception.Message}");
            }
        }

        private void WriteToUnityConsole(string logMessage, LogLevelEnum level)
        {
            switch (level)
            {
                case LogLevelEnum.Warning:
                    Debug.LogWarning(logMessage);
                    break;
                case LogLevelEnum.Error:
                case LogLevelEnum.Fatal:
                    Debug.LogError(logMessage);
                    break;
            }
        }
    }
}
