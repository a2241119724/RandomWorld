namespace LAB2D.UnityAdapter
{
    using System.Collections;

    /// <summary>
    /// Bridges wave coroutine scheduling so WaveManager does not directly depend on
    /// a specific scene Singleton for StartCoroutine, StopCoroutine, or waits.
    /// </summary>
    public interface IWaveTimeScheduler
    {
        object Start(IEnumerator routine);

        void Stop(object coroutine);

        object WaitForSeconds(float seconds);

        object WaitUntilMapReady();
    }
}
