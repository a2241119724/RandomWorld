using System;
using System.Collections;
using UnityEngine;

namespace LAB2D
{
    /// <summary>
    /// √ª”√
    /// </summary>
    public class CoroutineManager : MonoBehaviour
    {
        public static CoroutineManager Instance { get; set; }
        //private int availableCount;
        //private Func<IEnumerator>[] coroutineDelegates;
        //private List<Coroutine> coroutines;

        private void Awake()
        {
            Instance = this;
        }

        public CoroutineManager(int maxCount) {
            //availableCount = maxCount;
            //coroutineDelegates = new Func<IEnumerator>[maxCount];
        }

        public void startCoroutine(Func<IEnumerator> coroutineDelegate) {
            //if (availableCount > 0)
            //{
            //    coroutines.Add(StartCoroutine(coroutineDelegate()));
            //}
            StartCoroutine(coroutineDelegate());
        }
    }
}

