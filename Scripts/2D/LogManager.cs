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
    /// ��¼��־
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
            // ���������̨
            Debug.Log(logMessage);
        }

        // �洢����־�б�
        //logs.Add(logMessage);

        // ����������ļ���¼����д���ļ�
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
