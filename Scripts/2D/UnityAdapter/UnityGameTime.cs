namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// Unity implementation of IGameTime wrapping UnityEngine.Time.
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
