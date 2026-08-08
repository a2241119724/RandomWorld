namespace LAB2D.Manager
{
    using LAB2D;
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
        private readonly string errorLogPath = Path.Combine(Application.persistentDataPath, "error.log");
        private readonly List<string> logs = new List<string>();
        private readonly int maxLogCount = 100; // 每条日志立即落盘，方便运行时查看game.log.
        private LogLevelEnum minLogLevel = LogLevelEnum.Trace; // 最小的日志级别.
        private bool isSave = true;
        private bool logFileAvailable = true;
        private bool errorFileAvailable = true;
        private int suppressUnityErrorCapture = 0;
        private long index = 0;

        public LogManager()
        {
            this.PrepareLogFile(this.logPath, true, ref this.logFileAvailable, "game.log");
            this.PrepareLogFile(this.errorLogPath, true, ref this.errorFileAvailable, "error.log");
            Application.logMessageReceived += this.HandleUnityLogMessageReceived;
            Application.quitting += this.Flush;
        }

        /// <summary>
        /// 日志级别.
        /// </summary>
        public enum LogLevelEnum
        {
            /// <summary>
            /// 跟踪级别。
            /// </summary>
            Trace = 0,

            /// <summary>
            /// 调试级别。
            /// </summary>
            Debug = 1,

            /// <summary>
            /// 信息级别。
            /// </summary>
            Info = 2,

            /// <summary>
            /// 警告级别。
            /// </summary>
            Warning = 3,

            /// <summary>
            /// 错误级别。
            /// </summary>
            Error = 4,

            /// <summary>
            /// 致命级别。
            /// </summary>
            Fatal = 5,

            /// <summary>
            /// 关闭日志。
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
            this.SaveLog(this.logPath, pendingLogs, ref this.logFileAvailable, "game.log");
            if (level >= LogLevelEnum.Error)
            {
                this.SaveErrorLog(logMessage);
            }
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
                this.PrepareLogFile(this.logPath, false, ref this.logFileAvailable, "game.log");
                this.PrepareLogFile(this.errorLogPath, false, ref this.errorFileAvailable, "error.log");
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

            this.SaveLog(this.logPath, pendingLogs, ref this.logFileAvailable, "game.log");
        }

        private void SaveLog(string path, List<string> logs, ref bool fileAvailable, string fileName)
        {
            if (!this.isSave || !fileAvailable || logs == null || logs.Count == 0)
            {
                return;
            }

            try
            {
                lock (this.syncRoot)
                {
                    File.AppendAllText(path, string.Join(Environment.NewLine, logs) + Environment.NewLine);
                }
            }
            catch (Exception exception)
            {
                fileAvailable = false;
                Debug.LogWarning($"[LogManager] 写入{fileName}失败，已暂停该文件日志: {exception.Message}");
            }
        }

        private void SaveErrorLog(string logMessage)
        {
            if (!this.isSave || !this.errorFileAvailable || string.IsNullOrEmpty(logMessage))
            {
                return;
            }

            try
            {
                lock (this.syncRoot)
                {
                    File.AppendAllText(this.errorLogPath, logMessage + Environment.NewLine);
                }
            }
            catch (Exception exception)
            {
                this.errorFileAvailable = false;
                Debug.LogWarning($"[LogManager] 写入error.log失败，已暂停错误日志: {exception.Message}");
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

        private void PrepareLogFile(string path, bool clear, ref bool fileAvailable, string fileName)
        {
            if (!this.isSave)
            {
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (clear)
                {
                    File.WriteAllText(path, string.Empty);
                }
                else if (!File.Exists(path))
                {
                    File.WriteAllText(path, string.Empty);
                }

                fileAvailable = true;
            }
            catch (Exception exception)
            {
                fileAvailable = false;
                Debug.LogWarning($"[LogManager] 初始化{fileName}失败，已暂停该文件日志: {exception.Message}");
            }
        }

        private void HandleUnityLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (this.suppressUnityErrorCapture > 0
                || (type != LogType.Error && type != LogType.Assert && type != LogType.Exception))
            {
                return;
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logMessage;
            lock (this.syncRoot)
            {
                logMessage = $"{timestamp} [Unity{type}] {this.index++} {condition ?? string.Empty}";
            }

            if (!string.IsNullOrEmpty(stackTrace))
            {
                logMessage += Environment.NewLine + stackTrace;
            }

            this.SaveErrorLog(logMessage);
        }

        private void WriteToUnityConsole(string logMessage, LogLevelEnum level)
        {
            switch (level)
            {
                // case LogLevelEnum.Trace:
                // case LogLevelEnum.Debug:  // Debug/Trace 仅写文件，不输出到 Unity 控制台
                case LogLevelEnum.Info:
                    Debug.Log(logMessage);
                    break;
                case LogLevelEnum.Warning:
                    Debug.LogWarning(logMessage);
                    break;
                case LogLevelEnum.Error:
                case LogLevelEnum.Fatal:
                    try
                    {
                        this.suppressUnityErrorCapture++;
                        Debug.LogError(logMessage);
                    }
                    finally
                    {
                        this.suppressUnityErrorCapture--;
                    }

                    break;
            }
        }
    }
}
