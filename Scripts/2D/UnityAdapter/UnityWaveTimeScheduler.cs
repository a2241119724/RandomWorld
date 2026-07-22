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
        public object Start(IEnumerator routine)
        {
            return Core.ServiceLocator.Get<TileMap>().StartCoroutine(routine);
        }

        public void Stop(object coroutine)
        {
            if (coroutine == null)
            {
                return;
            }

            Coroutine unityCoroutine = coroutine as Coroutine;
            if (unityCoroutine != null)
            {
                Core.ServiceLocator.Get<TileMap>().StopCoroutine(unityCoroutine);
            }
        }

        public object WaitForSeconds(float seconds)
        {
            return new WaitForSeconds(seconds < 0.0f ? 0.0f : seconds);
        }

        public object WaitUntilMapReady()
        {
            return new WaitUntil(() => Core.ServiceLocator.Get<Core.MapInitCoordinator>().IsComplete);
        }
    }
}
