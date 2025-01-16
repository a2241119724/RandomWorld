using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class SelectUI : MonoBehaviour
    {
        public static SelectUI Instance { private set; get; }
        public Character Character { get; set; }
        // Map
        public Vector3 Target { get; set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (Character != null)
            {
                transform.position = Character.transform.position;
            }
            else
            {
                transform.position = new Vector3(Mathf.RoundToInt(Target.x + 0.5f) - 0.5f, Mathf.RoundToInt(Target.y + 0.5f) - 0.5f);
            }
        }
    }
}
