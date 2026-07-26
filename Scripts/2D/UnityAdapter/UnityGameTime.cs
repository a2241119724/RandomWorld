namespace LAB2D.UnityAdapter
{
    using LAB2D.Domain.Common;
    using UnityEngine;

    /// <summary>
    /// IGameTime 的 Unity 实现，封装 UnityEngine.Time。
    /// </summary>
    public sealed class UnityGameTime : IGameTime
    {
        public float DeltaTime
        {
            get { return UnityEngine.Time.deltaTime; }
        }

        public float Time
        {
            get { return UnityEngine.Time.time; }
        }

        public float RealtimeSinceStartup
        {
            get { return UnityEngine.Time.realtimeSinceStartup; }
        }
    }
}
