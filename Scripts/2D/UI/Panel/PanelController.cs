namespace LAB2D.UI.Panel
{
    using LAB2D;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 面板控制器
    /// </summary>
    public class PanelController : Singleton<PanelController>
    {
        private Transform parent;
        private Stack<IBasePanel> panels;

        /// <summary>
        /// 所有面板父物体
        /// </summary>
        public Transform Parent
        {
            get
            {
                if (this.parent == null)
                {
                    this.parent = GameObject.FindGameObjectWithTag(TagConstant.UI_TAG).transform;
                }

                return this.parent;
            }

            set
            {
                this.parent = value;
            }
        }

        /// <summary>
        /// 面板栈
        /// </summary>
        public Stack<IBasePanel> Panels
        {
            get
            {
                if (this.panels == null)
                {
                    this.panels = new Stack<IBasePanel>();
                }

                return this.panels;
            }

            set
            {
                this.panels = value;
            }
        }

        /// <summary>
        /// 展示下一个界面
        /// </summary>
        /// <param name="basePanel">下一个界面信息</param>
        public void Show(IBasePanel basePanel)
        {
            if (this.Panels.Count > 0 && !(basePanel is ItemInfoPanel
                || basePanel is AIChatPanel
                || basePanel is DialoguePanel))
            {
                this.Panels.Peek().OnPause();
            }

            this.Panels.Push(basePanel);
            basePanel.OnEnter();
        }

        /// <summary>
        /// 关闭面板
        /// </summary>
        public void Close()
        {
            if (this.Panels.Count > 0)
            {
                // 先pop再执行退出
                IBasePanel panel = this.Panels.Pop();
                panel.OnExit();
            }

            if (this.Panels.Count > 0)
            {
                this.Panels.Peek().OnRun();
            }
        }

        /// <summary>
        /// 最上面的面板是否是前景面板
        /// </summary>
        /// <returns>是否</returns>
        public bool IsForeground()
        {
            if (this.Panels.Count > 0)
            {
                return this.Panels.Peek() == ForegroundPanel.Instance;
            }

            return false;
        }
    }
}