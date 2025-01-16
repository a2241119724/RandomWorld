using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class DebugUI : MonoBehaviour
    {
        public static DebugUI Instance { private set; get; }

        private Text text;

        private void Awake()
        {
            text = Tool.GetComponentInChildren<Text>(gameObject,"Info");
            Instance = this;
        }

        public void updateTaskInfo(string _text)
        {
            text.text = _text;
        }
    }
}
