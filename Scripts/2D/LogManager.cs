using LAB2D;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LogManager : Singleton<LogManager>
{
    public readonly LogLevel minLogLevel = LogLevel.Info;

    private List<string> logs;
    private readonly string logPath = Application.persistentDataPath + "/game.log";
    private readonly bool isSave = true;

    public LogManager()
    {
        logs = new List<string>();
        if (isSave)
        {
            File.WriteAllText(logPath, string.Empty);
        }
    }

    /// <summary>
    /// 记录日志
    /// </summary>
    /// <param name="message"></param>
    /// <param name="level"></param>
    public void log(string message, LogLevel level = LogLevel.Info)
    {
        if ((int)level < (int)minLogLevel)
            return;

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string logMessage = $"{timestamp} [{level}] {message}";

        if(level == LogLevel.Error)
        {
            // 输出到控制台
            Debug.Log(logMessage);
        }

        // 存储到日志列表
        //logs.Add(logMessage);

        // 如果启用了文件记录，则写入文件
        if (isSave)
        {
            File.AppendAllText(logPath, logMessage + Environment.NewLine);
        }
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Fatal
    }
}
