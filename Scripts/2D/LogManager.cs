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
        private readonly LogLevel minLogLevel = LogLevel.Info; // 最小的日志级别.
        private readonly string logPath = Application.persistentDataPath + "/game.log";
        private readonly bool isSave = true;
        private readonly List<string> logs;
        private readonly int maxLogCount = 10;

        public LogManager()
        {
            this.logs = new List<string>();
            if (this.isSave)
            {
                File.WriteAllText(this.logPath, string.Empty);
            }
        }

        /// <summary>
        /// 日志级别.
        /// </summary>
        public enum LogLevel
        {
            /// <summary>
            /// Debug.
            /// </summary>
            Debug,

            /// <summary>
            /// Info.
            /// </summary>
            Info,

            /// <summary>
            /// Warning.
            /// </summary>
            Warning,

            /// <summary>
            /// Error.
            /// </summary>
            Error,

            /// <summary>
            /// Fatal.
            /// </summary>
            Fatal,
        }

        /// <summary>
        /// 记录日志.
        /// </summary>
        /// <param name="message">日志内容.</param>
        /// <param name="level">日志级别.</param>
        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            if ((int)level < (int)this.minLogLevel)
            {
                return;
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logMessage = $"{timestamp} [{level}] {message}";

            if (level == LogLevel.Error)
            {
                Debug.Log(logMessage);
            }

            lock (this.logs)
            {
                this.logs.Add(logMessage);
                if (this.logs.Count > this.maxLogCount)
                {
                    if (this.logs.Count > this.maxLogCount)
                    {
                        this.SaveLog(this.logPath, this.logs);
                    }
                }
            }
        }

        private void SaveLog(string path, List<string> logs)
        {
            // 如果启用了文件记录，则写入文件
            if (this.isSave)
            {
                File.AppendAllText(path, string.Join("\n", logs));
            }

            logs.Clear();
        }
    }
}
