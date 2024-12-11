using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class DebugUI : MonoBehaviour
    {
        private Text text;

        private void Awake()
        {
            text = Tool.GetComponentInChildren<Text>(gameObject,"Info");
        }

        void Update()
        {
            string _text = WorkerTaskManager.Instance.getTaskInfo();
            text.text = _text;
        }
    }
}
