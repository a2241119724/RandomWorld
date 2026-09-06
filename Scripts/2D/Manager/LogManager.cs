namespace LAB2D.Manager
{
    using LAB2D;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// 日志管理.
    /// </summary>
    public class LogManager : Singleton<LogManager>
    {
        private readonly object syncRoot = new object();
        // 文件写专用锁：与 syncRoot 分离——AppendAllText（Open/Write/Close）耗时且波动大
        // （磁盘抖动时数十 ms），若在 syncRoot 内执行会阻塞所有线程的日志入队（主线程
        // Update 卡在日志锁上）。写之间互斥即可，入队不被 I/O 拖住（bug-fixes.md 2026-09-06）。
        private readonly object ioRoot = new object();
        private readonly string logPath = Path.Combine(Application.persistentDataPath, "game.log");
        private readonly string errorLogPath = Path.Combine(Application.persistentDataPath, "error.log");
        private readonly List<string> logs = new List<string>();
        private readonly int maxLogCount = 300; // 每 300 条批量落盘一次。10 时每秒 ~30 次 File.AppendAllText（Open/Write/Close），
                                                // 100 在 100 Worker 常态 ~1000 行/秒下仍 ~10 次写/秒；300 再降 3 倍（~3 次/秒）。
                                                // 影响：game.log 最多滞后 ~1-2 秒（Flush 在退出/关停时兜底），崩溃丢日志窗口同量级。
        private LogLevelEnum minLogLevel = LogLevelEnum.Debug; // 最小的日志级别.
        private bool isSave = true;
        private bool logFileAvailable = true;
        private bool errorFileAvailable = true;
        private int suppressUnityErrorCapture = 0;
        private long index = 0;

        // ---- Log() 的热路径降本字段（均在 syncRoot 锁内使用，见 Log 内注释）----
        /// <summary>时间戳缓存：上次格式化对应的毫秒数（Ticks/TimeSpan.TicksPerMillisecond）。</summary>
        private long lastTimestampMs = -1;

        /// <summary>时间戳缓存：lastTimestampMs 对应的格式化串（"yyyy-MM-dd HH:mm:ss.fff"）。</summary>
        private string lastTimestampString;

        /// <summary>日志行拼接复用缓冲（仅锁内使用，每次 Length=0 重写）。</summary>
        private StringBuilder logBuilder;

        /// <summary>级别名缓存：索引 = (int)LogLevelEnum，与枚举值域 0-6 一一对应。</summary>
        private static readonly string[] LevelNames =
        {
            "Trace", "Debug", "Info", "Warning", "Error", "Fatal", "Off",
        };

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

            // 时间戳同毫秒复用（2026-09-06 AWorker.Update Invoke 峰值耗时治理）：
            // 自定义格式 DateTime.ToString 是单条日志最大成本（格式化 ~几μs + 串分配），
            // 而格式精度本就是 1ms（fff）——同毫秒多条共享同一字符串零语义损失。日志高峰期
            //（建造/战斗/决策风暴，正是 Invoke 峰值期）~4 条/ms 命中率高，平期 ~30%。
            DateTime now = DateTime.Now;

            lock (this.syncRoot)
            {
                long nowMs = now.Ticks / TimeSpan.TicksPerMillisecond;
                if (nowMs != this.lastTimestampMs)
                {
                    this.lastTimestampMs = nowMs;
                    this.lastTimestampString = now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                }

                // 拼接改 StringBuilder 复用 + 级别名静态数组：替代插值（string.Format 对
                // enum/int 装箱 + 中间串），单条内部分配只剩最终 ToString 一次。
                // builder 为实例字段，仅在 syncRoot 锁内使用，线程安全。
                StringBuilder builder = this.logBuilder ?? (this.logBuilder = new StringBuilder(160));
                builder.Length = 0;
                builder.Append(this.lastTimestampString).Append(" [").Append(LevelNames[(int)level])
                    .Append("] ").Append(this.index++).Append(' ').Append(message ?? string.Empty);
                logMessage = builder.ToString();

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

            // join 与写盘均在 ioRoot（写互斥）内、syncRoot 之外：入队锁只保护 logs 列表，
            // I/O 不阻塞其他线程的 Log() 调用。logs 参数是锁内拷贝出的独立列表，锁外只读安全。
            try
            {
                lock (this.ioRoot)
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

            // 同 SaveLog：写盘走 ioRoot，不与入队锁互斥（Unity 日志回调线程也可能进入）。
            try
            {
                lock (this.ioRoot)
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
