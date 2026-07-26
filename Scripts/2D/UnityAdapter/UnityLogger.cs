namespace LAB2D.UnityAdapter
{
    using LAB2D.Domain.Common;
    using UnityEngine;

    /// <summary>
    /// IGameLogger 的 Unity 实现，封装 UnityEngine.Debug。
    /// </summary>
    public sealed class UnityLogger : IGameLogger
    {
        public void Log(string message)
        {
            Debug.Log(message);
        }

        public void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        public void LogError(string message)
        {
            Debug.LogError(message);
        }
    }
}
