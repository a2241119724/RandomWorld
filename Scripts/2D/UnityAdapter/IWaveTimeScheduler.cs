namespace LAB2D.UnityAdapter
{
    using System.Collections;
    using UnityEngine;

    /// <summary>
    /// Bridges wave coroutine scheduling so WaveManager does not directly depend on
    /// a specific scene Singleton for StartCoroutine, StopCoroutine, or waits.
    /// </summary>
    public interface IWaveTimeScheduler
    {
        Coroutine Start(IEnumerator routine);

        void Stop(Coroutine coroutine);

        YieldInstruction WaitForSeconds(float seconds);
    }
}
