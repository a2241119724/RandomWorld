using System;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class PanelController : Singleton<PanelController>
    {
        public Transform Parent; // ������常����
        public Stack<IBasePanel> Panels { get; set; }

        public PanelController()
        {
            Parent = GameObject.FindGameObjectWithTag(ResourceConstant.UI_TAG_ROOT).transform;
            Panels = new Stack<IBasePanel>();
            if (Panels == null) {
                Debug.LogError("panels assign resource Error!!!");
                return;
            }
        }

        /// <summary>
        /// չʾ��һ������
        /// </summary>
        /// <param name="basePanel">��һ��������Ϣ</param>
        /// <param name="parent">������</param>
        public void show(IBasePanel basePanel)
        {
            if (Panels.Count > 0 && !(basePanel is ItemInfoPanel 
                || basePanel is AIChatPanel))
            {
                Panels.Peek().OnPause();
            }
            Panels.Push(basePanel);
            basePanel.OnEnter();
        }

        public void close()
        {
            if (Panels.Count > 0)
            {
                // ��pop��ִ���˳�
                IBasePanel panel =  Panels.Pop();
                panel.OnExit();
            }
            if (Panels.Count > 0)
            {
                Panels.Peek().OnRun();
            }
        }
    }
}