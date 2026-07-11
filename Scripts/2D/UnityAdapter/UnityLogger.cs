namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// Unity implementation of IGameLogger wrapping UnityEngine.Debug.
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
