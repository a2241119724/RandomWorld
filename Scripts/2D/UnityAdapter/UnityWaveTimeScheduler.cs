namespace LAB2D.UnityAdapter
{
    using System.Collections;
    using UnityEngine;

    /// <summary>
    /// Default Unity coroutine scheduler used by WaveManager.
    /// It keeps the existing TileMap coroutine host to preserve runtime behavior.
    /// </summary>
    public sealed class UnityWaveTimeScheduler : IWaveTimeScheduler
    {
        public Coroutine Start(IEnumerator routine)
        {
            return TileMap.Instance == null ? null : TileMap.Instance.StartCoroutine(routine);
        }

        public void Stop(Coroutine coroutine)
        {
            if (coroutine == null || TileMap.Instance == null)
            {
                return;
            }

            TileMap.Instance.StopCoroutine(coroutine);
        }

        public YieldInstruction WaitForSeconds(float seconds)
        {
            return new WaitForSeconds(seconds < 0.0f ? 0.0f : seconds);
        }
    }
}
