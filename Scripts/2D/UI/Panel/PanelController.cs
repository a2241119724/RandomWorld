using System;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class PanelController : Singleton<PanelController>
    {
        public Transform Parent; // 所有面板父物体
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
        /// 展示下一个界面
        /// </summary>
        /// <param name="basePanel">下一个界面信息</param>
        /// <param name="parent">父物体</param>
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
                // 先pop再执行退出
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