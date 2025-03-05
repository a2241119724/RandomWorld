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
        private Transform btns;

        private void Awake()
        {
            btns = Tool.GetComponentInChildren<Transform>(gameObject, "Panel");
        }

        void Update()
        {
            if (!Input.anyKeyDown) return;
            for (int i=0;i < btns.childCount; i++)
            {
                if (Input.GetKeyDown(keyCodes[i]))
                {
                    if (PanelController.Instance.Panels.Peek() == ForegroundPanel.Instance)
                    {
                        PanelController.Instance.show(ForegroundPanel.Instance.ToolMenus[i]);
                    }
                    else if(PanelController.Instance.Panels.Peek() == ForegroundPanel.Instance.ToolMenus[i])
                    {
                        PanelController.Instance.close();
                    }
                    break;
                }
            }
        }
    }
}
