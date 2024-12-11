using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public abstract class Singleton<T> where T : new()
    {
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new T();
                }
                return instance;
            }
        }
        private static T instance;

        public void init() {
            // ≥ı ºªØInstance
        }
    }
}
