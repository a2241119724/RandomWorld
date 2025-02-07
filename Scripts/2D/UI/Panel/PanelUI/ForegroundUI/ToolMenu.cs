using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace LAB2D
{
    public class ToolMenu : MonoBehaviour
    {
        private static readonly KeyCode[] keyCodes = { KeyCode.Alpha1, KeyCode.Alpha2,
            KeyCode.Alpha3,KeyCode.Alpha4,KeyCode.Alpha5,KeyCode.Alpha6,KeyCode.Alpha7,
            KeyCode.Alpha8,KeyCode.Alpha9};

        void Update()
        {
            for(int i=0;i < transform.childCount; i++)
            {
                if (!Input.anyKeyDown) break;
                if (Input.GetKeyDown(keyCodes[i]))
                {
                    ForegroundPanel.Instance.ToolMenus[i].Invoke();
                    break;
                }
            }
        }
    }
}
